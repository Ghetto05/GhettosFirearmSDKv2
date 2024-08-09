using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class BreakAction : BoltBase, IAmmunitionLoadable
    {
        public bool ejector = true;

        public Axes foldAxis;
        public float minFoldAngle;
        public float maxFoldAngle;

        public List<AudioSource> lockSounds;
        public List<AudioSource> unlockSounds;

        public List<AudioSource> ejectSounds;
        public List<AudioSource> insertSounds;

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

        public Transform ejectorAxis;
        public Transform ejectorClosedPosition;
        public Transform ejectorOpenedPosition;
        public float ejectorMoveStartPercentage = 0.8f;

        public FiremodeSelector chamberSetSelector;
        public List<string> editorChamberSets;
        public Dictionary<int, List<int>> chamberSets = new Dictionary<int, List<int>>();
        public List<Trigger> triggers;
        public List<string> targetHandPoses;
        private List<HandPoseData> targetHandPoseData = new List<HandPoseData>();
        public List<string> defaultAmmoItems;
        

        private MagazineSaveData data;

        private int currentChamber;
        private int currentChamberSet;

        public enum Axes
        {
            X,
            Y,
            Z,
        }

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
            firearm.actualHitscanMuzzle = muzzles.First();

            if (editorChamberSets.Any())
            {
                int i = 0;
                foreach (string s in editorChamberSets)
                {
                    chamberSets.Add(i, new List<int>());
                    chamberSets[i].AddRange(s.Split(',').Select(cs => int.Parse(cs)));
                    i++;
                }
            }
            else
            {
                chamberSets.Add(0, new List<int>());
                for (int i = 0; i < mountPoints.Count; i++)
                {
                    chamberSets[0].Add(i);
                }
            }
        }

        public void InvokedStart()
        {
            loadedCartridges = new Cartridge[mountPoints.Count];
            actualMuzzles = muzzles;
            state = BoltState.Locked;
            rb.gameObject.AddComponent<CollisionRelay>().onCollisionEnterEvent += OnCollisionEvent;
            firearm.OnMuzzleCalculatedEvent += Firearm_OnMuzzleCalculatedEvent;
            
            if (chamberSets.Any())
            {
                if (chamberSetSelector != null)
                    chamberSetSelector.onFiremodeChanged += ChamberSetSelectorOnFireModeChanged;
                SetChamberSet(0);

                if (triggers.Any() && triggers.Count > currentChamberSet)
                {
                    foreach (Trigger t in triggers)
                    {
                        t.triggerEnabled = false;
                    }
                    triggers[currentChamberSet].triggerEnabled = true;
                }
            }

            Initialize();
            if (firearm.item.TryGetCustomData(out data))
            {
                for (int i = 0; i < data.contents.Length; i++)
                {
                    if (data.contents[i] != null)
                    {
                        int index = i;
                        Util.SpawnItem(data.contents[index], "Bolt Chamber", ci => { Cartridge c = ci.GetComponent<Cartridge>(); LoadChamber(index, c, false); }, transform.position + Vector3.up * 3);
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

        private void ChamberSetSelectorOnFireModeChanged(FirearmBase.FireModes newMode)
        {
            if (newMode != FirearmBase.FireModes.Safe)
            {
                currentChamberSet = chamberSetSelector.currentIndex;
                SetChamberSet(currentChamberSet);
            }
        }

        private void SetChamberSet(int set)
        {
            currentChamber = chamberSets[set].First();

            foreach (Trigger t in triggers)
            {
                t.triggerEnabled = false;
            }

            if (triggers.Count > set)
                triggers[set].triggerEnabled = true;

            if (targetHandPoseData.Any())
            {
                foreach (HandlePose h in firearm.AllTriggerHandles().Where(h => h != null).SelectMany(h => h.orientations))
                {
                    h.targetHandPoseData = targetHandPoseData[set];
                }
            }

            if (defaultAmmoItems.Count > set)
                firearm.defaultAmmoItem = defaultAmmoItems[set];
        }

        private void Firearm_OnMuzzleCalculatedEvent()
        {
            if (firearm.actualHitscanMuzzle.TryGetComponent(out BreakActionMuzzleOverride over))
            {
                actualMuzzles = over.newMuzzles;
                actualMuzzleFlashes = over.newMuzzleFlashes;
                firearm.actualHitscanMuzzle = over.newMuzzles.First();
            }
            else
            {
                actualMuzzles = muzzles;
                actualMuzzleFlashes = muzzleFlashes;
                firearm.actualHitscanMuzzle = muzzles.First();
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
            if (loadedCartridges[index] == null && Util.AllowLoadCartridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(insertSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.DisallowDespawn = true;
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

        public void TryEjectSingle(int i, bool ignoreFiredState = false, bool silent = false)
        {
            if (loadedCartridges[i] != null)
            {
                Cartridge c = loadedCartridges[i];
                if (Settings.breakActionsEjectOnlyFired && !c.fired && !ignoreFiredState)
                    return;
                if (chamberSets.Any() && !chamberSets[currentChamberSet].Contains(i))
                    return;
                if (!silent)
                    Util.PlayRandomAudioSource(ejectSounds);
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
                c.item.DisallowDespawn = false;
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
                
            CalculateCyclePercentage();
            UpdateEjector();

            if (firearm.triggerState)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) for (int i = 0; i < mountPoints.Count; i++) { TryFire(); };
            }
            else shotsSinceTriggerReset = 0;
        }

        private void Update()
        {
            BaseUpdate();
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
                
                shotsSinceTriggerReset++;
                if (hammers.Count > currentChamber && hammers[currentChamber] != null)
                    hammers[currentChamber].Fire();

                if (hammerCocked && cartridge)
                {
                    foreach (RagdollHand hand in firearm.item.handlers)
                    {
                        if (hand.playerHand != null || hand.playerHand.controlHand != null)
                            hand.playerHand.controlHand.HapticShort(50f);
                    }
                    Transform muzzle = muzzles.Count < 2 ? firearm.actualHitscanMuzzle : actualMuzzles[currentChamber];
                    Cartridge loadedCartridge = loadedCartridges[currentChamber];
                    IncrementBreachSmokeTime();
                    firearm.PlayFireSound(loadedCartridge);
                    if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                    {
                        if (actualMuzzleFlashes != null && actualMuzzleFlashes.Count > currentChamber && actualMuzzleFlashes[currentChamber] != null && muzzles.Count > 1) actualMuzzleFlashes[currentChamber].Play();
                        else firearm.PlayMuzzleFlash(loadedCartridge);
                    }
                    FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
                    FireMethods.Fire(firearm.item, muzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
                    loadedCartridge.Fire(hits, trajectories, muzzle, hitCreatures, !Settings.infiniteAmmo);
                    InvokeFireEvent();
                }
            }

            if (!chamberSets.Any())
            {
                currentChamber++;
                if (currentChamber >= mountPoints.Count)
                    currentChamber = 0;
            }
            else
            {
                currentChamber++;
                if (currentChamber >= chamberSets[currentChamberSet].Count)
                    currentChamber = chamberSets[currentChamberSet].First();
            }

            InvokeFireLogicFinishedEvent();
        }

        public void UpdateEjector()
        {
            if (state == BoltState.Locked || ejectorAxis == null || cyclePercentage < ejectorMoveStartPercentage)
            {
                if (ejectorAxis != null)
                    ejectorAxis.SetPositionAndRotation(ejectorClosedPosition.position, ejectorClosedPosition.rotation);
                return;
            }

            float actualPercentage = (cyclePercentage - ejectorMoveStartPercentage) / (1 - ejectorMoveStartPercentage);
            ejectorAxis.position = Vector3.Lerp(ejectorClosedPosition.position, ejectorOpenedPosition.position, actualPercentage);
            ejectorAxis.rotation = Quaternion.Lerp(ejectorClosedPosition.rotation, ejectorOpenedPosition.rotation, actualPercentage);
        }

        public override void TryRelease(bool forced = false)
        {
            if (state == BoltState.Locked)
                Unlock();
            else if (Settings.breakActionsEjectOnlyFired)
                for (int i = 0; i < loadedCartridges.Length; i++)
                {
                    TryEjectSingle(i, true);
                }
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
            foreach (Handle h in barrel.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
            {
                h.SetTouch(true);
            }
            InitializeJoint(true);
        }

        public void Unlock()
        {
            if (!chamberSets.Any())
                currentChamber = 0;
            else
                currentChamber = chamberSets[currentChamberSet].First();
            state = BoltState.Moving;
            if (lockAxis != null)
            {
                lockAxis.localPosition = lockUnlockedPosition.localPosition;
                lockAxis.localEulerAngles = lockUnlockedPosition.localEulerAngles;
            }
            ejectedSinceOpen = false;
            Util.PlayRandomAudioSource(unlockSounds);
            foreach (Handle h in barrel.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
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
            base.UpdateChamberedRounds();
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

        public void CalculateCyclePercentage()
        {
            float angle = Quaternion.Angle(rb.transform.rotation, closedPosition.rotation);
            float targetAngle = Quaternion.Angle(openedPosition.rotation, closedPosition.rotation);

            cyclePercentage = Mathf.Clamp01(angle / targetAngle);
        }

        private int GetFirstFreeChamber()
        {
            List<int> availableChambers = new List<int>();
            for (int i = loadedCartridges.Length - 1; i >= 0; i--)
            {
                if (loadedCartridges[i] == null)
                    availableChambers.Add(i);
            }

            availableChambers = availableChambers.Where(x => chamberSets[currentChamberSet].Contains(x)).ToList();

            if (availableChambers.Count == 0)
                return 0;
            
            return availableChambers.First();
        }

        public string GetCaliber()
        {
            return calibers[GetFirstFreeChamber()];
        }

        public int GetCapacity()
        {
            return calibers.Count(x => x.Equals(GetCaliber()));
        }

        public List<Cartridge> GetLoadedCartridges()
        {
            return loadedCartridges.ToList();
        }

        public void LoadRound(Cartridge cartridge)
        {
            LoadChamber(GetFirstFreeChamber(), cartridge);
        }

        public void ClearRounds()
        {
            foreach (Cartridge car in loadedCartridges)
            { 
                if (car != null)
                    car.item.Despawn(0.05f);
            }
            
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                TryEjectSingle(i, true, true);
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public bool GetForceCorrectCaliber()
        {
            return false;
        }

        public List<string> GetAlternativeCalibers()
        {
            return null;
        }
    }
}