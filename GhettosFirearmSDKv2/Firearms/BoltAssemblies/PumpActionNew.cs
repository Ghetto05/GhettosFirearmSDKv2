//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using ThunderRoad;

//namespace GhettosFirearmSDKv2
//{
//    public class PumpAction : BoltBase
//    {
//        public Rigidbody rb;
//        public Transform bolt;
//        public Transform startPoint;
//        public Transform endPoint;
//        public Transform hammerCockPoint;
//        public Transform roundEjectPoint;
//        public Transform roundLoadPoint;
//        public List<AttachmentPoint> onBoltPoints;
//        [HideInInspector]
//        public List<Handle> boltHandles;
//        public AudioSource[] rackSounds;
//        public AudioSource[] pullSounds;
//        public bool slamFire;
//        public float roundEjectForce = 0.6f;
//        public Transform roundEjectDir;
//        public Transform roundMount;
//        public Transform roundReparent;
//        [HideInInspector]
//        public Cartridge loadedCartridge;
//        public Hammer hammer;

//        bool behindLoadPoint = false;
//        int shotsSinceTriggerReset;
//        FixedJoint lockJoint;
//        FixedJoint nonHeldLockJoint;
//        ConfigurableJoint joint;
//        bool closedSinceLastEject = false;
//        bool wentToFrontSinceLastLock = false;
//        bool currentRoundRemounted;
//        bool ready = false;

//        public void Start()
//        {
//            Invoke("InvokedStart", FirearmsSettings.invokeTime + 0.01f);
//        }

//        CapsuleCollider stabilizer;
//        public void InvokedStart()
//        {
//            //InitializeJoint();
//            RefreshBoltHandles();
//            Lock(true);
//            stabilizer = rb.gameObject.AddComponent<CapsuleCollider>();
//            stabilizer.radius = 0.03f;
//            stabilizer.gameObject.layer = LayerMask.NameToLayer("UI");

//            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
//            firearm.item.OnGrabEvent += Item_OnGrabEvent;
//            firearm.OnAttachmentAddedEvent += Firearm_OnAttachmentAddedEvent;
//            firearm.OnAttachmentRemovedEvent += Firearm_OnAttachmentRemovedEvent;
//            ChamberSaved();
//            if (loadedCartridge) Invoke("DelayedReparent", 0.03f);
//            ready = true;
//        }

//        private void Firearm_OnAttachmentRemovedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
//        {
//            RefreshBoltHandles();
//        }

//        private void Firearm_OnAttachmentAddedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
//        {
//            RefreshBoltHandles();
//        }

//        public void RefreshBoltHandles()
//        {
//            boltHandles = new List<Handle>();

//            foreach (Handle h in rb.gameObject.GetComponentsInChildren<Handle>())
//            {
//                boltHandles.Add(h);
//            }
//            foreach (AttachmentPoint point in onBoltPoints)
//            {
//                foreach (Attachment attachment in point.GetAllChildAttachments())
//                {
//                    foreach (Handle handle in attachment.handles)
//                    {
//                        boltHandles.Add(handle);
//                    }
//                }
//            }

//            foreach (Handle h in boltHandles)
//            {
//                h.customRigidBody = !lockJoint ? rb : null;
//            }
//        }

//        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
//        {
//            if (boltHandles.Contains(handle))
//            {
//                RefreshBoltHandles();
//            }
//            //SetStateOnAllHandlers(lockJoint);
//            if (loadedCartridge && roundReparent && currentRoundRemounted)
//            {
//                loadedCartridge.transform.SetParent(roundReparent);
//            }
//        }

//        public override void UpdateChamberedRounds()
//        {
//            if (!loadedCartridge) return;
//            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
//            loadedCartridge.transform.parent = currentRoundRemounted && roundReparent ? roundReparent : roundMount;
//            loadedCartridge.transform.localPosition = Vector3.zero;
//            loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
//        }

//        public void SetStateOnAllHandlers(bool locked)
//        {
//            foreach (Handle handle in boltHandles)
//            {
//                RagdollHand[] hands = handle.handlers.ToArray();
//                handle.Release();
//                foreach (RagdollHand hand in hands)
//                {
//                    hand.Grab(handle, true);
//                }
//            }
//        }

//        private void Firearm_OnTriggerChangeEvent(bool isPulled)
//        {
//            if (!isPulled) shotsSinceTriggerReset = 0;
//        }

//        public override void TryFire()
//        {
//            if (!loadedCartridge || loadedCartridge.fired)
//            {
//                Lock(false);
//                return;
//            }
//            foreach (RagdollHand hand in firearm.item.handlers)
//            {
//                if (!hand.playerHand || hand.playerHand.controlHand is null) return;
//                hand.playerHand.controlHand.HapticShort(50f);
//            }
//            shotsSinceTriggerReset++;
//            if (loadedCartridge.additionalMuzzleFlash)
//            {
//                loadedCartridge.additionalMuzzleFlash.transform.position = firearm.hitscanMuzzle.position;
//                loadedCartridge.additionalMuzzleFlash.transform.rotation = firearm.hitscanMuzzle.rotation;
//                loadedCartridge.additionalMuzzleFlash.transform.SetParent(firearm.hitscanMuzzle);
//                StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
//            }
//            firearm.PlayFireSound(loadedCartridge);
//            firearm.PlayMuzzleFlash(loadedCartridge);
//            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
//            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, firearm.CalculateDamageMultiplier());
//            if (!FirearmsSettings.infiniteAmmo || (FirearmsSettings.infiniteAmmo && firearm.magazineWell))
//            {
//                loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle);
//                Lock(false);
//            }
//        }

//        private void Lock(bool locked)
//        {
//            RefreshBoltHandles();
//            if (locked)
//            {
//                bolt.localPosition = startPoint.localPosition;
//                //rb.transform.localPosition = startPoint.localPosition;
//                if (!lockJoint) lockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
//                lockJoint.connectedBody = rb;
//                //lockJoint.connectedMassScale = 100f;
//                closedSinceLastEject = true;
//                wentToFrontSinceLastLock = false;
//                foreach (Handle h in boltHandles)
//                {
//                    h.customRigidBody = firearm.item.physicBody.rigidBody;
//                }
//                Destroy(joint);
//                SetStateOnAllHandlers(true);
//            }
//            else if (lockJoint)
//            {
//                foreach (Handle h in boltHandles)
//                {
//                    h.customRigidBody = rb;
//                }
//                Destroy(lockJoint);
//                InitializeJoint();
//                SetStateOnAllHandlers(false);
//            }
//        }

//        private bool BoltHandleHeld()
//        {
//            foreach (Handle h in boltHandles)
//            {
//                if (h.IsHanded()) return true;
//            }

//            return false;
//        }

//        private void FixedUpdate()
//        {
//            if (!ready) return;
//            stabilizer.gameObject.layer = LayerMask.NameToLayer("UI");
//            stabilizer.gameObject.SetActive(isHeld);

//            //UpdateChamberedRound();
//            isHeld = BoltHandleHeld();

//            //state check
//            if (isHeld)
//            {
//                if (nonHeldLockJoint) Destroy(nonHeldLockJoint);

//                if (!lockJoint) bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, rb.transform.localPosition.z);
//                if (Util.AbsDist(bolt.position, startPoint.position) < FirearmsSettings.boltPointTreshold && state == BoltState.Moving)
//                {
//                    laststate = BoltState.Moving;
//                    state = BoltState.Locked;
//                    if (loadedCartridge && roundReparent)
//                    {
//                        currentRoundRemounted = true;
//                        loadedCartridge.transform.SetParent(roundReparent);
//                        loadedCartridge.transform.localPosition = Vector3.zero;
//                        loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
//                    }
//                    if (wentToFrontSinceLastLock) Lock(true);
//                    Util.PlayRandomAudioSource(rackSounds);
//                }
//                else if (Util.AbsDist(bolt.position, endPoint.position) < FirearmsSettings.boltPointTreshold && state == BoltState.Moving)
//                {
//                    laststate = BoltState.Moving;
//                    state = BoltState.Back;
//                    Util.PlayRandomAudioSource(pullSounds);
//                    wentToFrontSinceLastLock = true;
//                    if (closedSinceLastEject) EjectRound();
//                    closedSinceLastEject = false;
//                }
//                else if (state != BoltState.Moving && Util.AbsDist(bolt.position, endPoint.position) > FirearmsSettings.boltPointTreshold && Util.AbsDist(bolt.position, startPoint.position) > FirearmsSettings.boltPointTreshold)
//                {
//                    laststate = state;
//                    state = BoltState.Moving;
//                }
//                //loading
//                if (state == BoltState.Moving && (laststate == BoltState.Back || laststate == BoltState.LockedBack))
//                {
//                    if (roundLoadPoint && behindLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
//                    {
//                        if (!loadedCartridge) TryLoadRound();
//                        behindLoadPoint = false;
//                    }
//                    else if (roundLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition)) behindLoadPoint = true;
//                }
//            }
//            else
//            {
//                if (!nonHeldLockJoint)
//                {
//                    nonHeldLockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
//                    nonHeldLockJoint.connectedBody = rb;
//                    //nonHeldLockJoint.connectedMassScale = 100f;
//                }
//            }

//            //firing
//            if (state == BoltState.Locked && firearm.triggerState && firearm.fireMode != FirearmBase.FireModes.Safe)
//            {
//                if (firearm.fireMode == FirearmBase.FireModes.Semi && (slamFire || shotsSinceTriggerReset == 0)) TryFire();
//            }
//        }

//        public override void EjectRound()
//        {
//            if (!loadedCartridge) return;
//            currentRoundRemounted = false;
//            Cartridge c = loadedCartridge;
//            loadedCartridge = null;
//            if (roundEjectPoint)
//            {
//                c.transform.position = roundEjectPoint.position;
//                c.transform.rotation = roundEjectPoint.rotation;
//            }
//            Util.IgnoreCollision(c.gameObject, firearm.item.gameObject, true);
//            c.ToggleCollision(true);
//            Util.DelayIgnoreCollision(c.gameObject, firearm.item.gameObject, false, 3f, firearm.item);
//            Rigidbody rb = c.GetComponent<Rigidbody>();
//            c.item.DisallowDespawn = false;
//            c.transform.parent = null;
//            c.loaded = false;
//            rb.isKinematic = false;
//            rb.WakeUp();
//            if (roundEjectDir)
//            {
//                AddTorqueToCartridge(c);
//                AddForceToCartridge(c, roundEjectDir, roundEjectForce);
//            }
//            c.ToggleHandles(true);
//            if (firearm.magazineWell && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired) firearm.magazineWell.Eject();
//            InvokeEjectRound(c);
//        }

//        public override void TryLoadRound()
//        {
//            if (!loadedCartridge && firearm.magazineWell && firearm.magazineWell.ConsumeRound() is Cartridge c)
//            {
//                loadedCartridge = c;
//                c.GetComponent<Rigidbody>().isKinematic = true;
//                c.transform.SetParent(roundMount);
//                c.transform.localPosition = Vector3.zero;
//                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
//                SaveChamber(c.item.itemId);
//            }
//        }

//        private void InitializeJoint()
//        {
//            joint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
//            joint.connectedBody = rb;
//            SoftJointLimit limit = new SoftJointLimit();
//            joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((startPoint.localPosition.z - endPoint.localPosition.z) / 2));
//            limit.limit = Vector3.Distance(endPoint.position, startPoint.position) / 2;
//            joint.linearLimit = limit;
//            joint.autoConfigureConnectedAnchor = false;
//            joint.connectedAnchor = Vector3.zero;
//            joint.xMotion = ConfigurableJointMotion.Locked;
//            joint.yMotion = ConfigurableJointMotion.Locked;
//            joint.zMotion = ConfigurableJointMotion.Limited;
//            joint.angularXMotion = ConfigurableJointMotion.Locked;
//            joint.angularYMotion = ConfigurableJointMotion.Locked;
//            joint.angularZMotion = ConfigurableJointMotion.Locked;
//            rb.transform.localPosition = startPoint.localPosition;
//            rb.transform.localRotation = startPoint.localRotation;
//        }

//        public override bool LoadChamber(Cartridge c, bool forced)
//        {
//            if (!loadedCartridge && (state != BoltState.Locked || forced) && !c.loaded)
//            {
//                loadedCartridge = c;
//                c.item.DisallowDespawn = true;
//                c.loaded = true;
//                c.ToggleHandles(false);
//                c.ToggleCollision(false);
//                c.UngrabAll();
//                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
//                c.GetComponent<Rigidbody>().isKinematic = true;
//                c.transform.SetParent(roundMount);
//                c.transform.localPosition = Vector3.zero;
//                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
//                Util.DelayedExecute(0.005f, DelayedReparent, this);
//                SaveChamber(c.item.itemId);
//                return true;
//            }
//            return false;
//        }

//        public void DelayedReparent()
//        {
//            loadedCartridge.transform.SetParent(roundMount);
//            currentRoundRemounted = true;
//        }

//        public override void TryRelease(bool forced = false)
//        {
//            if (lockJoint) Lock(false);
//        }
//    }
//}