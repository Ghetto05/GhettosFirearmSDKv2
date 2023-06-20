using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class BreakAction : BoltBase
    {
        public bool ejector = true;

        public Axes foldAxis;
        public float minFoldAngle;
        public float maxFoldAngle;

        public List<AudioSource> lockSounds;
        public List<AudioSource> unlockSounds;

        public List<AudioSource> ejectSounds;
        public List<AudioSource> insertSounds;

        public List<Handle> foregripHandles;
        public Rigidbody rb;
        public Transform barrel;
        public Transform closedPosition;
        public Transform openedPosition;

        public List<string> calibers;
        public List<Transform> muzzles;
        public List<Transform> actualMuzzles;
        public List<ParticleSystem> muzzleFlashes;
        public List<ParticleSystem> actualMuzzleFlashes;
        public List<Hammer> hammers;
        public List<Transform> mountPoints;
        public List<Collider> loadColliders;
        public List<Transform> ejectDirections;
        public List<Transform> ejectPoints;
        private Cartridge[] loadedCartridges;
        public List<float> ejectForces;

        private HingeJoint joint;
        private bool ejectedSinceOpen;
        private int shotsSinceTriggerReset = 0;
        private bool allowInsert = false;

        public Transform lockAxis;
        public Transform lockLockedPosition;
        public Transform lockUnlockedPosition;

        private MagazineSaveData data;

        int currentChamber = 0;

        public enum Axes
        {
            X,
            Y,
            Z,
        }

        private void Start()
        {
            Invoke("InvokedStart", FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            loadedCartridges = new Cartridge[mountPoints.Count];
            actualMuzzles = muzzles;
            state = BoltState.Locked;
            rb.gameObject.AddComponent<CollisionRelay>().onCollisionEnterEvent += OnCollisionEvent;
            firearm.OnMuzzleCalculatedEvent += Firearm_OnMuzzleCalculatedEvent;
            Initialize();
            if (firearm.item.TryGetCustomData(out data))
            {
                for (int i = 0; i < data.contents.Length; i++)
                {
                    if (data.contents[i] != null)
                    {
                        int index = i;
                        Catalog.GetData<ItemData>(data.contents[index]).SpawnAsync(ci => { Cartridge c = ci.GetComponent<Cartridge>(); LoadChamber(index, c, false); }, transform.position + Vector3.up * 3);
                    }
                }
                UpdateChamberedRounds();
            }
            else
            {
                firearm.item.AddCustomData(new MagazineSaveData());
                firearm.item.TryGetCustomData(out data);
                data.contents = new string[loadedCartridges.Length];
            }
            allowInsert = true;
            UpdateChamberedRounds();
        }

        private void Firearm_OnMuzzleCalculatedEvent()
        {
            if (firearm.actualHitscanMuzzle.TryGetComponent(out BreakActionMuzzleOverride overr))
            {
                actualMuzzles = overr.newMuzzles;
                actualMuzzleFlashes = overr.newMuzzleFlashes;
            }
            else
            {
                actualMuzzles = muzzles;
                actualMuzzleFlashes = muzzleFlashes;
            }
        }

        private void OnCollisionEvent(Collision collision)
        {
            if (!allowInsert) return;
            if (collision.collider.GetComponentInParent<Cartridge>() is Cartridge car && !car.loaded)
            {
                foreach (Collider insertCollider in loadColliders)
                {
                    if (Util.CheckForCollisionWithThisCollider(collision, insertCollider))
                    {
                        int index = loadColliders.IndexOf(insertCollider);
                        LoadChamber(index, car);
                    }
                }
            }
        }

        public void LoadChamber(int index, Cartridge cartridge, bool overrideSave = true)
        {
            if (loadedCartridges[index] == null && Util.AllowLoadCatridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(insertSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.disallowDespawn = true;
                cartridge.loaded = true;
                cartridge.ToggleHandles(false);
                cartridge.ToggleCollision(false);
                cartridge.UngrabAll();
                Util.IgnoreCollision(cartridge.gameObject, firearm.gameObject, true);
                cartridge.GetComponent<Rigidbody>().isKinematic = true;
                cartridge.transform.parent = mountPoints[index];
                cartridge.transform.localPosition = Vector3.zero;
                cartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
                if (overrideSave) SaveCartridges();
            }
            UpdateChamberedRounds();
        }

        public override void TryEject()
        {
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                TryEjectSingle(i);
            }
        }

        public void TryEjectSingle(int i)
        {
            if (loadedCartridges[i] != null)
            {
                Util.PlayRandomAudioSource(ejectSounds);
                Cartridge c = loadedCartridges[i];
                loadedCartridges[i] = null;
                if (ejectPoints.Count > i && ejectPoints[i] != null)
                {
                    c.transform.position = ejectPoints[i].position;
                    c.transform.rotation = ejectPoints[i].rotation;
                }
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.ToggleCollision(true);
                Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
                Rigidbody rb = c.item.physicBody.rigidBody;
                c.item.disallowDespawn = false;
                c.transform.parent = null;
                c.loaded = false;
                rb.isKinematic = false;
                rb.WakeUp();
                if (ejectDirections[i] != null)
                {
                    AddForceToCartridge(c, ejectDirections[i], ejectForces[i]);
                    //AddTorqueToCartridge(c);
                }
                c.ToggleHandles(true);
                InvokeEjectRound(c);
                SaveCartridges();
            }
        }

        public void FixedUpdate()
        {
            if (state == BoltState.Locked)
            {
                barrel.SetParent(closedPosition.parent);
                barrel.localPosition = closedPosition.localPosition;
                barrel.localEulerAngles = closedPosition.localEulerAngles;
            }
            else
            {
                barrel.SetParent(rb.transform);
                barrel.localPosition = Vector3.zero;
                barrel.localEulerAngles = Vector3.zero;
            }

            if (state != BoltState.Locked)
            {
                if (CompareEulers(rb.transform, openedPosition) && !ejectedSinceOpen)
                {
                    if (ejector) EjectRound();
                    else ejectedSinceOpen = true;
                }
                if (CompareEulers(rb.transform, closedPosition) && ejectedSinceOpen) Lock();

                if (!ejector)
                {
                    for (int i = 0; i < ejectDirections.Count; i++)
                    {
                        if (CheckEjectionGravity(ejectDirections[i]))
                        {
                            TryEjectSingle(i);
                        }
                    }
                }
            }

            if (firearm.triggerState)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) for (int i = 0; i < mountPoints.Count; i++) { TryFire(); };
            }
            else shotsSinceTriggerReset = 0;
        }

        private bool CheckEjectionGravity(Transform t)
        {
            float angle = Vector3.Angle(t.forward, Vector3.down);
            return angle < 50f;
        }

        private bool CompareEulers(Transform ta, Transform tb)
        {
            float a, b;
            if (foldAxis == Axes.X)
            {
                a = ta.localEulerAngles.x;
                b = tb.localEulerAngles.x;
            }
            else if (foldAxis == Axes.Y)
            {
                a = ta.localEulerAngles.y;
                b = tb.localEulerAngles.y;
            }
            else
            {
                a = ta.localEulerAngles.z;
                b = tb.localEulerAngles.z;
            }
            float angle = Mathf.Abs(a-b);
            return angle <= 2f;
        }

        public override void TryFire()
        {
            if (state == BoltState.Locked)
            {
                bool hammerCocked = hammers.Count - 1 < currentChamber || hammers[currentChamber] == null || hammers[currentChamber].cocked;
                bool cartridge = loadedCartridges[currentChamber] != null && !loadedCartridges[currentChamber].fired;

                if (hammerCocked && cartridge)
                {
                    shotsSinceTriggerReset++;
                    if (hammers.Count > currentChamber && hammers[currentChamber] != null) hammers[currentChamber].Fire();
                    foreach (RagdollHand hand in firearm.item.handlers)
                    {
                        if (hand.playerHand != null || hand.playerHand.controlHand != null) hand.playerHand.controlHand.HapticShort(50f);
                    }
                    Transform muzzle = muzzles.Count < 2 ? firearm.actualHitscanMuzzle : actualMuzzles[currentChamber];
                    Cartridge loadedCartridge = loadedCartridges[currentChamber];
                    if (loadedCartridge.additionalMuzzleFlash != null)
                    {
                        loadedCartridge.additionalMuzzleFlash.transform.position = muzzle.position;
                        loadedCartridge.additionalMuzzleFlash.transform.rotation = muzzle.rotation;
                        loadedCartridge.additionalMuzzleFlash.transform.SetParent(muzzle);
                        StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
                    }
                    firearm.PlayFireSound(loadedCartridge);
                    if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                    {
                        if (actualMuzzleFlashes != null && actualMuzzleFlashes.Count > currentChamber && actualMuzzleFlashes[currentChamber] != null && muzzles.Count > 1) actualMuzzleFlashes[currentChamber].Play();
                        else firearm.PlayMuzzleFlash(loadedCartridge);
                    }
                    FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
                    FireMethods.Fire(firearm.item, muzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, firearm.CalculateDamageMultiplier());
                    if (!FirearmsSettings.infiniteAmmo)
                    {
                        loadedCartridge.Fire(hits, trajectories, muzzle);
                    }
                    InvokeFireEvent();
                }
            }

            currentChamber++;
            if (currentChamber >= mountPoints.Count) currentChamber = 0;
        }

        public override void TryRelease(bool forced = false)
        {
            if (state == BoltState.Locked) Unlock();
        }

        public override void Initialize()
        {
            state = BoltState.Locked;
            InitializeJoint(true);
        }

        public void Lock()
        {
            state = BoltState.Locked;
            if (lockAxis != null)
            {
                lockAxis.localPosition = lockLockedPosition.localPosition;
                lockAxis.localEulerAngles = lockLockedPosition.localEulerAngles;
            }
            Util.PlayRandomAudioSource(lockSounds);
            foreach (Handle h in foregripHandles)
            {
                h.SetTouch(true);
            }
            InitializeJoint(true);
        }

        public void Unlock()
        {
            state = BoltState.Moving;
            if (lockAxis != null)
            {
                lockAxis.localPosition = lockUnlockedPosition.localPosition;
                lockAxis.localEulerAngles = lockUnlockedPosition.localEulerAngles;
            }
            ejectedSinceOpen = false;
            Util.PlayRandomAudioSource(unlockSounds);
            foreach (Handle h in foregripHandles)
            {
                foreach (RagdollHand hand in h.handlers.ToArray())
                {
                    hand.UnGrab(false);
                }
                h.SetTouch(false);
            }
            InitializeJoint(false);
        }

        public override void EjectRound()
        {
            ejectedSinceOpen = true;
            TryEject();
        }

        private void InitializeJoint(bool closed)
        {
            if (closed)
            {
                rb.transform.localEulerAngles = closedPosition.localEulerAngles;
                barrel.SetParent(rb.transform);
                barrel.localPosition = Vector3.zero;
                barrel.localEulerAngles = Vector3.zero;
            }
            else
            {
                barrel.SetParent(closedPosition.parent);
                barrel.localPosition = closedPosition.localPosition;
                barrel.localEulerAngles = closedPosition.localEulerAngles;
            }

            if (joint == null)
            {
                joint = firearm.item.gameObject.AddComponent<HingeJoint>();
                joint.connectedBody = rb;
                joint.massScale = 0.00001f;
            }
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = GrandparentLocalPosition(closedPosition.transform, firearm.item.transform);
            joint.connectedAnchor = Vector3.zero;
            joint.axis = foldAxis == Axes.X ? Vector3.right : foldAxis == Axes.Y ? Vector3.up : Vector3.forward;
            joint.useLimits = true;
            joint.limits = closed ? new JointLimits() { min = 0f, max = 0f } : new JointLimits() { min = minFoldAngle, max = maxFoldAngle };
        }

        public void SaveCartridges()
        {
            data.contents = new string[loadedCartridges.Length];
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                data.contents[i] = loadedCartridges[i]?.item.itemId;
            }
        }

        public override void UpdateChamberedRounds()
        {
            for (int i = 0; i < mountPoints.Count; i++)
            {
                if (loadedCartridges[i] != null)
                {
                    loadedCartridges[i].GetComponent<Rigidbody>().isKinematic = true;
                    loadedCartridges[i].transform.parent = mountPoints[i];
                    loadedCartridges[i].transform.localPosition = Vector3.zero;
                    loadedCartridges[i].transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
            }
        }
    }
}