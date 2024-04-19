using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Bolt assemblies/Minigun")]
    public class Minigun : BoltBase
    {
        public bool revOnTrigger;
        public bool loopingMuzzleFlash;
        
        public float[] barrelAngles;
        public Transform roundMount;
        public Cartridge loadedCartridge;
        public Transform roundEjectPoint;
        public Transform roundEjectDir;
        public float roundEjectForce;
        public Transform barrel;

        public AudioSource RevUpSound;
        public AudioSource RevDownSound;
        public AudioSource RotatingLoop;
        public AudioSource RotatingLoopPlusFiring;
        private bool revving;
        private float degreesPerSecond;

        private float lastShotTime;
        private float currentSpeed;
        private float revUpBeginTime;
        private float beginTime = -100f;
        private bool revvingUp;
        private bool revvingDown;


        private void Start()
        {
            if (!revOnTrigger)
                firearm.item.OnHeldActionEvent += Item_OnHeldActionEvent;
            else
                firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;
        }

        private void FirearmOnOnTriggerChangeEvent(bool isPulled)
        {
            if (isPulled)
                StartRevving();
            else
                StopRevving();
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == firearm.item.mainHandleRight)
            {
                if (action == Interactable.Action.AlternateUseStart) StartRevving();
                else if (action == Interactable.Action.AlternateUseStop) StopRevving();
            }
        }

        private void StartRevving()
        {
            if (revving)
                return;
            revvingUp = true;
            revvingDown = false;
            beginTime = Time.time;
            revUpBeginTime = Time.time;
            RevUpSound.Play();
            float timeForOneRound = 60f / firearm.roundsPerMinute;
            float timeForOneRotation = timeForOneRound * barrelAngles.Length;
            float rotationsPerSecond = 1 / timeForOneRotation;
            degreesPerSecond = rotationsPerSecond * 360;
        }

        private void StopRevving()
        {
            if (!revving)
                return;
            revving = false;
            revvingUp = false;
            revvingDown = true;
            beginTime = Time.time;
            RotatingLoop.Stop();
            RotatingLoopPlusFiring.Stop();
            RevUpSound.Stop();
            RevDownSound.Play();
            
            if (loopingMuzzleFlash && firearm.defaultMuzzleFlash != null && firearm.defaultMuzzleFlash.isPlaying)
                firearm.defaultMuzzleFlash.Stop();
        }

        private void FixedUpdate()
        {
            revving = revvingUp && (Time.time - revUpBeginTime >= RevUpSound.clip.length);
            if (revving && !RotatingLoop.isPlaying)
                RotatingLoop.Play();
            
            if (revvingUp || revvingDown)
            {
                float timeSinceStart = Time.time - beginTime;
                float speed = timeSinceStart / RevUpSound.clip.length;
                if (speed > 1)
                    speed = 1;
                if (revvingDown)
                    speed = 1f - speed;
                currentSpeed = speed;
            }

            if (barrel != null)
                barrel.Rotate(new Vector3(0, 0, degreesPerSecond * Time.deltaTime * currentSpeed));

            if (fireOnTriggerPress && firearm.triggerState && revving && Time.time - lastShotTime >= 60f / firearm.roundsPerMinute)
            {
                TryFire();
            }

            if (!RotatingLoopPlusFiring.isPlaying && revving && firearm.triggerState && !firearm.magazineWell.IsEmpty())
            {
                RotatingLoopPlusFiring.Play();
                RotatingLoop.Stop();
            }
            if (!RotatingLoop.isPlaying && revving && (!firearm.triggerState || firearm.magazineWell.IsEmpty()))
            {
                RotatingLoop.Play();
                RotatingLoopPlusFiring.Stop();
            }
        }

        private void Update()
        {
            BaseUpdate();
        }

        public override void TryFire()
        {
            TryLoadRound();
            if (loadedCartridge == null || loadedCartridge.fired)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            lastShotTime = Time.time;
            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand != null && hand.playerHand.controlHand != null) 
                    hand.playerHand.controlHand.HapticShort(50f);
            }
            if (!loopingMuzzleFlash && loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                firearm.PlayMuzzleFlash(loadedCartridge);
            else if (loopingMuzzleFlash && firearm.defaultMuzzleFlash != null && !firearm.defaultMuzzleFlash.isPlaying)
                firearm.defaultMuzzleFlash.Play();
            IncrementBreachSmokeTime();
            firearm.PlayFireSound(loadedCartridge);
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            Util.PlayRandomAudioSource(firearm.fireSounds);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, true);
            EjectRound();
            InvokeFireEvent();
            InvokeFireLogicFinishedEvent();
        }

        public override void EjectRound()
        {
            if (loadedCartridge == null)
                return;
            Cartridge c = loadedCartridge;
            loadedCartridge = null;
            if (roundEjectPoint != null)
            {
                c.transform.position = roundEjectPoint.position;
                c.transform.rotation = roundEjectPoint.rotation;
            }
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.ToggleCollision(true);
            Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
            Rigidbody rb = c.GetComponent<Rigidbody>();
            c.item.disallowDespawn = false;
            c.transform.parent = null;
            rb.isKinematic = false;
            c.loaded = false;
            rb.WakeUp();
            if (roundEjectDir != null)
            {
                AddTorqueToCartridge(c);
                AddForceToCartridge(c, roundEjectDir, roundEjectForce);
            }
            c.ToggleHandles(true);
            if (firearm.magazineWell != null && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired)
                firearm.magazineWell.Eject();
            InvokeEjectRound(c);
        }

        public override void TryLoadRound()
        {
            if (loadedCartridge == null && firearm.magazineWell.ConsumeRound() is Cartridge c)
            {
                loadedCartridge = c;
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
            }
        }
    }
}
