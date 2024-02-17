using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class FlintLock : BoltBase
    {
        [Header("Firing")]
        public float fireDelay;
        public ParticleSystem panEffect;
        public PowderReceiver mainReceiver;
        public float baseRecoil = 20;
        
        [Header("Hammer")]
        public Transform hammer;
        public Transform hammerIdlePosition;
        public Transform hammerCockedPosition;
        private bool _hammerState;

        [Header("Pan")]
        public Transform pan;
        public Transform panOpenedPosition;
        public Transform panClosedPosition;
        public PowderReceiver panReceiver;
        private bool _panClosed;

        [Header("Round loading")]
        public string caliber;
        public Collider roundInsertCollider;
        public Transform roundMountPoint;
        public Cartridge loadedCartridge;
        private float lastRoundPosition;

        [Header("Ram rod")]
        public Transform rodFrontEnd;
        public Transform rodRearEnd;
        public string ramRodItem;
        private Item currentRamRod;
        public Collider ramRodInsertCollider;
        private ConfigurableJoint joint;
        private bool rodAwayFromBreach;

        [Header("Audio")]
        public AudioSource[] sizzleSound; 
        [Space]
        public AudioSource[] hammerCockSounds;
        public AudioSource[] hammerFireSounds;
        [Space]
        public AudioSource[] panOpenSounds;
        public AudioSource[] panCloseSounds;
        [Space]
        public AudioSource[] ramRodInsertSound;
        public AudioSource[] ramRodExtractSound;
        [Space]
        public AudioSource[] roundInsertSounds;

        private ProjectileData emptyFireData;

        private void Start()
        {
            GenerateFireData();
            OpenPan(true);
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        private void GenerateFireData()
        {
            emptyFireData = gameObject.AddComponent<ProjectileData>();
            emptyFireData.recoil = 20;
            emptyFireData.forceDestabilize = false;
            emptyFireData.forceIncapitate = false;
            emptyFireData.isHitscan = true;
            emptyFireData.lethalHeadshot = false;
            emptyFireData.penetrationPower = ProjectileData.PenetrationLevels.None;
            emptyFireData.projectileCount = 30;
            emptyFireData.projectileRange = 1;
            emptyFireData.projectileSpread = 25;
            emptyFireData.damagePerProjectile = 0.3f;
            emptyFireData.hasBodyImpactEffect = false;
            emptyFireData.hasImpactEffect = false;
        }

        private void InvokedStart()
        {
            firearm.OnCollisionEvent += FirearmOnOnCollisionEvent;
            firearm.OnCockActionEvent += CockHammer;
            firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;
            firearm.OnAltActionEvent += FirearmOnOnAltActionEvent;
        }

        private void FirearmOnOnAltActionEvent(bool longpress)
        {
            if (!longpress)
            {
                if (_panClosed)
                    OpenPan();
                else
                    ClosePan();
            }
        }

        private void FirearmOnOnTriggerChangeEvent(bool ispulled)
        {
            if (ispulled)
            {
                TryFire();
            }
        }

        private void FirearmOnOnCollisionEvent(Collision collision)
        {
            if (collision.rigidbody.TryGetComponent(out Item hitItem))
            {
                if (currentRamRod == null && hitItem.itemId.Equals(ramRodItem) && Util.CheckForCollisionWithThisCollider(collision, ramRodInsertCollider))
                {
                    //InitializeRamRodJoint(hitItem.physicBody.rigidBody);
                    InitializeRamRodJoint(hitItem);
                    currentRamRod = hitItem;
                    currentRamRod.disallowDespawn = true;
                    rodAwayFromBreach = false;
                    Util.PlayRandomAudioSource(ramRodInsertSound);
                }
                else if (hitItem.TryGetComponent(out Cartridge c) && Util.CheckForCollisionWithThisCollider(collision, roundInsertCollider))
                {
                    LoadChamber(c);
                }
            }
        }

        [EasyButtons.Button]
        public override void TryFire()
        {
            if (!_hammerState)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            
            Util.PlayRandomAudioSource(hammerFireSounds);
            hammer.SetPositionAndRotation(hammerIdlePosition.position, hammerIdlePosition.rotation);
            _hammerState = false;

            if (!_panClosed)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            
            OpenPan();

            if (!panReceiver.Sufficient())
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            if (!FirearmsSettings.infiniteAmmo)
                panReceiver.currentAmount = 0;
            Util.PlayRandomAudioSource(sizzleSound);
            if (panEffect != null)
                panEffect.Play();
            
            Invoke(nameof(DelayedFire), fireDelay);

            base.TryFire();
        }

        public void DelayedFire()
        {
            if (!mainReceiver.Sufficient())
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            if (!FirearmsSettings.infiniteAmmo)
                mainReceiver.currentAmount = 0;
            if (loadedCartridge != null && Vector3.Distance(loadedCartridge.transform.position, rodRearEnd.position) < 1f)
            {
                firearm.PlayFireSound(loadedCartridge);
                if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                    firearm.PlayMuzzleFlash(loadedCartridge);
                FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hitPoints, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
                FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
                loadedCartridge.Fire(hitPoints, trajectories, firearm.actualHitscanMuzzle, hitCreatures, !HeldByAI() && !FirearmsSettings.infiniteAmmo);
            }
            else if (loadedCartridge != null)
            {
                mainReceiver.currentAmount = mainReceiver.minimum;
            }
            else
            {
                FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, emptyFireData, out List<Vector3> hitPoints, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
                FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, baseRecoil, 1, firearm.recoilModifier, firearm.recoilModifiers);
                firearm.defaultMuzzleFlash?.Play();   
            }
            Util.PlayRandomAudioSource(firearm.fireSounds);
        }

        [EasyButtons.Button]
        public void CockHammer()
        {
            if (_hammerState)
                return;
            Util.PlayRandomAudioSource(hammerCockSounds);
            hammer.SetPositionAndRotation(hammerCockedPosition.position, hammerCockedPosition.rotation);
            _hammerState = true;
        }

        [EasyButtons.Button]
        public void OpenPan(bool forced = false)
        {
            if (!_panClosed && !forced)
                return;
            if (!forced)
                Util.PlayRandomAudioSource(panOpenSounds);
            pan.SetPositionAndRotation(panOpenedPosition.position, panOpenedPosition.rotation);
            _panClosed = false;
            panReceiver.blocked = false;
        }

        [EasyButtons.Button]
        public void ClosePan()
        {
            if (_panClosed || !_hammerState)
                return;
            Util.PlayRandomAudioSource(panCloseSounds);
            pan.SetPositionAndRotation(panClosedPosition.position, panClosedPosition.rotation);
            _panClosed = true;
            panReceiver.blocked = true;
        }

        private void FixedUpdate()
        {
            mainReceiver.blocked = loadedCartridge != null || currentRamRod != null;
            
            if (currentRamRod != null && !rodAwayFromBreach &&
                Vector3.Distance(currentRamRod.transform.position, rodFrontEnd.position) > 0.05f)
                rodAwayFromBreach = true;
            
            if (currentRamRod != null && rodAwayFromBreach &&
                Vector3.Distance(currentRamRod.transform.position, rodFrontEnd.position) < 0.02f)
            {
                InitializeRamRodJoint(null);
                currentRamRod.disallowDespawn = false;
                currentRamRod = null;
                Util.PlayRandomAudioSource(ramRodExtractSound);
            }

            if (currentRamRod != null && loadedCartridge != null)
            {
                float currentPos = Vector3.Distance(rodFrontEnd.position, currentRamRod.transform.position);
                float targetPos = Vector3.Distance(rodFrontEnd.position, rodFrontEnd.position);
                float posTime = currentPos / targetPos;
                if (posTime > lastRoundPosition)
                    lastRoundPosition = posTime;
                loadedCartridge.transform.position = Vector3.Lerp(rodFrontEnd.position, rodRearEnd.position, lastRoundPosition);
            }
        }

        private void InitializeRamRodJoint(Item item)
        {
            if (joint != null)
                Destroy(joint);
            if (item == null)
            {
                Debug.Log("No RB");
                return;
            }

            joint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
            joint.massScale = 0.00001f;
            joint.linearLimit = new SoftJointLimit
            {
                limit = Vector3.Distance(rodFrontEnd.position, rodRearEnd.position) / 2
            };
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
            joint.anchor = new Vector3(GrandparentLocalPosition(rodRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).y, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).z + ((rodFrontEnd.localPosition.z - rodRearEnd.localPosition.z) / 2));
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Free;
            item.transform.position = rodFrontEnd.position;
            item.transform.rotation = rodFrontEnd.rotation;
            joint.connectedBody = item.physicBody.rigidBody;
        }

        public override Cartridge GetChamber()
        {
            return loadedCartridge;
        }

        public override bool LoadChamber(Cartridge c, bool forced = false)
        {
            if (loadedCartridge == null && (Util.AllowLoadCatridge(c, caliber) || forced))
            {
                loadedCartridge = c;
                c.item.disallowDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.item.physicBody.isKinematic = true;
                c.transform.parent = rodFrontEnd;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                SaveChamber(c.item.itemId);
                return true;
            }
            return false;
        }
    }
}
