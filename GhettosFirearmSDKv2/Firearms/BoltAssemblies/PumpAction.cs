using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class PumpAction : BoltBase
    {
        public bool actsAsRelay;
        public Rigidbody rb;
        public Transform bolt;
        public Transform startPoint;
        public Transform endPoint;
        public Transform hammerCockPoint;
        public Transform roundEjectPoint;
        public Transform roundLoadPoint;
        public List<AttachmentPoint> onBoltPoints;
        [HideInInspector]
        public List<Handle> boltHandles;
        public AudioSource[] rackSounds;
        public AudioSource[] pullSounds;
        public bool slamFire;
        public float roundEjectForce = 0.6f;
        public Transform roundEjectDir;
        public Transform roundMount;
        public Transform roundReparent;
        [HideInInspector]
        public Cartridge loadedCartridge;
        public Hammer hammer;

        bool behindLoadPoint = false;
        int shotsSinceTriggerReset;
        FixedJoint lockJoint;
        FixedJoint nonHeldLockJoint;
        ConfigurableJoint joint;
        bool closedSinceLastEject = false;
        bool wentToFrontSinceLastLock = false;
        bool currentRoundRemounted;
        bool ready = false;
        bool beforeHammerPoint = true;

        public void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            firearm.item.OnGrabEvent += Item_OnGrabEvent;
            firearm.OnAttachmentAddedEvent += Firearm_OnAttachmentAddedEvent;
            firearm.OnAttachmentRemovedEvent += Firearm_OnAttachmentRemovedEvent;
            RefreshBoltHandles();
            ChamberSaved();
            if (loadedCartridge != null) Invoke(nameof(DelayedReparent), 0.03f);
            Lock(true);
            ready = true;
            Invoke(nameof(UpdateChamberedRounds), 1f);
        }

        private void Firearm_OnAttachmentRemovedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            RefreshBoltHandles();
        }

        private void Firearm_OnAttachmentAddedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            RefreshBoltHandles();
        }

        public void RefreshBoltHandles()
        {
            try
            {
                boltHandles = new List<Handle>();

                foreach (Handle h in rb.gameObject.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
                {
                    boltHandles.Add(h);
                }
                foreach (Handle h in bolt.gameObject.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
                {
                    boltHandles.Add(h);
                }
                foreach (AttachmentPoint point in onBoltPoints)
                {
                    foreach (Attachment attachment in point.GetAllChildAttachments())
                    {
                        foreach (Handle handle in attachment.handles)
                        {
                            if (handle.GetType() == typeof(GhettoHandle))
                            {
                                GhettoHandle hh = (GhettoHandle)handle;
                                hh.type = GhettoHandle.HandleType.PumpAction;
                            }
                            boltHandles.Add(handle);
                        }
                    }
                }

                foreach (Handle h in boltHandles)
                {
                    h.customRigidBody = lockJoint == null ? rb : firearm.item.physicBody.rigidBody;
                }
            }
            catch (System.Exception)
            {
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            RefreshBoltHandles();
            if (boltHandles.Contains(handle))
            {
            }
            if (loadedCartridge != null && roundReparent != null && currentRoundRemounted)
            {
                loadedCartridge.transform.SetParent(roundReparent);
            }
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            if (loadedCartridge == null) return;
            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
            loadedCartridge.transform.parent = currentRoundRemounted && roundReparent != null ? roundReparent : roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
        }

        public void SetStateOnAllHandlers(bool locked)
        {
            foreach (Handle handle in boltHandles)
            {
                List<RagdollHand> hands = handle.handlers.ToList();
                //handle.Release();
                handle.customRigidBody = locked ? firearm.item.physicBody.rigidBody : rb;

                foreach (RagdollHand hand in hands)
                {
                    //hand.Grab(handle, false);
                    Rigidbody targetRB = locked ? firearm.item.physicBody.rigidBody : rb;
                    hand.gripInfo.joint.connectedAnchor = targetRB.transform.InverseTransformPoint(hand.gripInfo.transform.position);
                    hand.gripInfo.joint.connectedBody = targetRB;
                }
            }
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!isPulled) shotsSinceTriggerReset = 0;
        }

        public override void TryFire()
        {
            if (actsAsRelay || loadedCartridge == null || loadedCartridge.fired || (hammer != null && !hammer.cocked))
            {
                Lock(false);
                InvokeFireLogicFinishedEvent();
                return;
            }
            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand != null && hand.playerHand.controlHand != null)
                    hand.playerHand.controlHand.HapticShort(50f);
            }
            shotsSinceTriggerReset++;
            if (hammer)
                hammer.Fire();
            if (loadedCartridge.additionalMuzzleFlash != null)
            {
                loadedCartridge.additionalMuzzleFlash.transform.position = firearm.actualHitscanMuzzle.position;
                loadedCartridge.additionalMuzzleFlash.transform.rotation = firearm.actualHitscanMuzzle.rotation;
                loadedCartridge.additionalMuzzleFlash.transform.SetParent(firearm.actualHitscanMuzzle);
                loadedCartridge.additionalMuzzleFlash.Play();
                StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
            }
            firearm.PlayFireSound(loadedCartridge);
            firearm.PlayMuzzleFlash(loadedCartridge);
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            bool fire = false;
            if (!FirearmsSettings.infiniteAmmo || (FirearmsSettings.infiniteAmmo && firearm.magazineWell != null))
            {
                fire = true;
                Lock(false);
            }
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, fire);
            InvokeFireLogicFinishedEvent();
            InvokeFireEvent();
        }

        public void Lock(bool locked)
        {
            RefreshBoltHandles();
            if (locked)
            {
                Util.DelayedExecute(0.005f, DelayedReparent, this);
                bolt.localPosition = startPoint.localPosition;
                rb.transform.localPosition = startPoint.localPosition;
                if (lockJoint == null) lockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
                lockJoint.connectedBody = rb;
                lockJoint.connectedMassScale = 100f;
                closedSinceLastEject = true;
                wentToFrontSinceLastLock = false;
                SetStateOnAllHandlers(true);
                Destroy(joint);
            }
            else if (lockJoint != null)
            {
                InitializeJoint();
                Destroy(lockJoint);
                SetStateOnAllHandlers(false);
            }
        }

        private bool BoltHandleHeld()
        {
            foreach (Handle h in boltHandles)
            {
                if (h.IsHanded()) return true;
            }

            return false;
        }

        private void FixedUpdate()
        {
            if (!ready) return;

            //UpdateChamberedRound();
            isHeld = BoltHandleHeld();

            //state check
            if (isHeld)
            {
                if (nonHeldLockJoint != null) Destroy(nonHeldLockJoint);

                if (lockJoint == null) bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, rb.transform.localPosition.z);
                if (Util.AbsDist(bolt.position, startPoint.position) < FirearmsSettings.boltPointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Locked;
                    if (loadedCartridge != null && roundReparent != null)
                    {
                        currentRoundRemounted = true;
                        loadedCartridge.transform.SetParent(roundReparent);
                        loadedCartridge.transform.localPosition = Vector3.zero;
                        loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
                    }
                    if (wentToFrontSinceLastLock) Lock(true);
                    Util.PlayRandomAudioSource(rackSounds);
                }
                else if (Util.AbsDist(bolt.position, endPoint.position) < FirearmsSettings.boltPointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
                    Util.PlayRandomAudioSource(pullSounds);
                    wentToFrontSinceLastLock = true;
                    if (closedSinceLastEject) EjectRound();
                    closedSinceLastEject = false;
                }
                else if (state != BoltState.Moving && Util.AbsDist(bolt.position, endPoint.position) > FirearmsSettings.boltPointTreshold && Util.AbsDist(bolt.position, startPoint.position) > FirearmsSettings.boltPointTreshold)
                {
                    laststate = state;
                    state = BoltState.Moving;
                }
                //loading
                if (state == BoltState.Moving && (laststate == BoltState.Back || laststate == BoltState.LockedBack))
                {
                    if (roundLoadPoint != null && behindLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                    {
                        if (loadedCartridge == null && !actsAsRelay) TryLoadRound();
                        behindLoadPoint = false;
                    }
                    else if (roundLoadPoint != null && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition)) behindLoadPoint = true;
                }
                //hammer
                if (state == BoltState.Moving && laststate == BoltState.Locked && !actsAsRelay)
                {
                    if (hammer != null && !hammer.cocked && beforeHammerPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                    {
                        hammer.Cock();
                    }
                    else if (hammer != null && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition)) beforeHammerPoint = true;
                }
            }
            else
            {
                if (nonHeldLockJoint == null)
                {
                    nonHeldLockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
                    nonHeldLockJoint.connectedBody = rb;
                    nonHeldLockJoint.connectedMassScale = 100f;
                }
            }

            //firing
            if (state == BoltState.Locked && firearm.triggerState && fireOnTriggerPress && firearm.fireMode != FirearmBase.FireModes.Safe)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && (slamFire || shotsSinceTriggerReset == 0 || actsAsRelay)) TryFire();
            }

            CalculatePercentage();
        }

        public override void EjectRound()
        {
            if (actsAsRelay || loadedCartridge == null)
                return;
            SaveChamber("");
            currentRoundRemounted = false;
            Cartridge c = loadedCartridge;
            loadedCartridge = null;
            if (roundEjectPoint != null)
            {
                c.transform.position = roundEjectPoint.position;
                c.transform.rotation = roundEjectPoint.rotation;
            }
            Util.IgnoreCollision(c.gameObject, firearm.item.gameObject, true);
            c.ToggleCollision(true);
            Util.DelayIgnoreCollision(c.gameObject, firearm.item.gameObject, false, 3f, firearm.item);
            Rigidbody crb = c.item.physicBody.rigidBody;
            c.item.disallowDespawn = false;
            c.transform.parent = null;
            c.loaded = false;
            crb.isKinematic = false;
            crb.WakeUp();
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
            if (!actsAsRelay && loadedCartridge == null && firearm.magazineWell != null && firearm.magazineWell.ConsumeRound() is Cartridge c)
            {
                loadedCartridge = c;
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.SetParent(roundMount);
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                SaveChamber(c.item.itemId);
            }
        }

        private void InitializeJoint()
        {
            joint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = rb;
            //pJoint.massScale = 0.00001f;
            joint.connectedMassScale = 100f;
            SoftJointLimit limit = new SoftJointLimit();
            joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((startPoint.localPosition.z - endPoint.localPosition.z) / 2));
            limit.limit = Vector3.Distance(endPoint.position, startPoint.position) / 2;
            joint.linearLimit = limit;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            rb.transform.localPosition = startPoint.localPosition;
            rb.transform.localRotation = startPoint.localRotation;
        }

        public override bool LoadChamber(Cartridge c, bool forced)
        {
            if (loadedCartridge == null && (state != BoltState.Locked || forced) && !c.loaded)
            {
                loadedCartridge = c;
                c.item.disallowDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.SetParent(roundMount);
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                SaveChamber(c.item.itemId);
                return true;
            }
            return false;
        }

        public void DelayedReparent()
        {
            if (loadedCartridge == null) return;
            loadedCartridge.transform.SetParent(roundReparent != null ? roundReparent : roundMount);
            currentRoundRemounted = true;
        }

        public override void TryRelease(bool forced = false)
        {
            if (lockJoint != null) Lock(false);
        }

        public void CalculatePercentage()
        {
            float distanceStartBolt = Util.AbsDist(bolt, startPoint);
            float totalDistance = Util.AbsDist(startPoint, endPoint);
            cyclePercentage = Mathf.Clamp01(distanceStartBolt / totalDistance);
        }
    }
}
