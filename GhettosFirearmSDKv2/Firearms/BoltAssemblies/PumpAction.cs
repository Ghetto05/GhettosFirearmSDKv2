using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class PumpAction : BoltBase
    {
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
        public float pointTreshold = 0.004f;
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
        ConfigurableJoint joint;
        bool closedSinceLastEject = false;
        bool wentToFrontSinceLastLock = false;
        bool currentRoundRemounted;

        private List<List<RagdollHand>> handlers;
        private List<List<Vector3>> handlerAnchors;

        public void Awake()
        {
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            firearm.item.OnGrabEvent += Item_OnGrabEvent;
            firearm.OnAttachmentAddedEvent += Firearm_OnAttachmentAddedEvent;
            firearm.OnAttachmentRemovedEvent += Firearm_OnAttachmentRemovedEvent;
            handlers = new List<List<RagdollHand>>();
            handlerAnchors = new List<List<Vector3>>();
            RefreshBoltHandles();
            StartCoroutine(delayedGetChamber());
            Lock(true);
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
            boltHandles = new List<Handle>();
            handlers = new List<List<RagdollHand>>();
            handlerAnchors = new List<List<Vector3>>();

            foreach (Handle h in rb.gameObject.GetComponentsInChildren<Handle>())
            {
                boltHandles.Add(h);
                h.customRigidBody = rb;
            }
            foreach (AttachmentPoint point in onBoltPoints)
            {
                foreach (Attachment attachment in point.GetAllChildAttachments())
                {
                    foreach (Handle handle in attachment.handles)
                    {
                        handle.customRigidBody = rb;
                        boltHandles.Add(handle);
                    }
                }
            }

            foreach (Handle h in boltHandles)
            {
                handlers.Add(new List<RagdollHand>());
                handlerAnchors.Add(new List<Vector3>());
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            if (boltHandles.Contains(handle))
            {
                RefreshBoltHandles();
            }
            SetStateOnAllHandlers(lockJoint != null);
        }

        private void UpdateChamberedRound()
        {
            if (loadedCartridge == null) return;
            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
            loadedCartridge.transform.parent = currentRoundRemounted && roundReparent != null ? roundReparent : roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Vector3.zero;
        }

        public void SetStateOnAllHandlers(bool locked)
        {
            if (locked)
            {
                foreach (Handle handle in boltHandles)
                {
                    int handleIndex = boltHandles.IndexOf(handle);
                    foreach (RagdollHand hand in handle.handlers)
                    {
                        if (hand.gripInfo.joint is ConfigurableJoint j)
                        {
                            j.connectedBody = firearm.item.rb;
                            j.anchor = Vector3.zero;
                            j.connectedAnchor = handlers[handleIndex].Contains(hand) ? handlerAnchors[handleIndex][handlers[handleIndex].IndexOf(hand)] : firearm.item.rb.transform.InverseTransformPoint(j.transform.position);
                        }
                    }
                }
            }
            else
            {
                foreach (Handle handle in boltHandles)
                {
                    int handleIndex = boltHandles.IndexOf(handle);
                    foreach (RagdollHand hand in handle.handlers)
                    {
                        if (hand.gripInfo.joint is ConfigurableJoint j)
                        {
                            if (!handlers[handleIndex].Contains(hand))
                            {
                                handlers[handleIndex].Add(hand);
                                handlerAnchors[handleIndex].Insert(handlers[handleIndex].IndexOf(hand), j.connectedAnchor);
                            }
                            j.connectedBody = rb;
                            j.anchor = Vector3.zero;
                            j.connectedAnchor = rb.transform.InverseTransformPoint(j.transform.position);
                        }
                    }
                }
            }
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!isPulled) shotsSinceTriggerReset = 0;
            else Lock(false);
        }

        public override void TryFire()
        {
            if (loadedCartridge == null || loadedCartridge.fired) return;
            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand == null || hand.playerHand.controlHand == null) return;
                hand.playerHand.controlHand.HapticShort(50f);
            }
            shotsSinceTriggerReset++;
            if (loadedCartridge.additionalMuzzleFlash != null)
            {
                loadedCartridge.additionalMuzzleFlash.transform.position = firearm.hitscanMuzzle.position;
                loadedCartridge.additionalMuzzleFlash.transform.rotation = firearm.hitscanMuzzle.rotation;
                loadedCartridge.additionalMuzzleFlash.transform.SetParent(firearm.hitscanMuzzle);
                StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
            }
            firearm.PlayFireSound();
            firearm.PlayMuzzleFlash();
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.rb, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories);
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle);
            Lock(false);
        }

        private void Lock(bool locked)
        {
            if (locked)
            {
                bolt.localPosition = startPoint.localPosition;
                rb.transform.localPosition = startPoint.localPosition;
                if (lockJoint == null) lockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
                lockJoint.connectedBody = rb;
                lockJoint.connectedMassScale = 100f;
                closedSinceLastEject = true;
                wentToFrontSinceLastLock = false;
                Destroy(joint);
                SetStateOnAllHandlers(true);
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
            UpdateChamberedRound();
            isHeld = BoltHandleHeld();

            //state check
            if (isHeld)
            {
                if (lockJoint == null) bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, rb.transform.localPosition.z);
                if (Util.AbsDist(bolt.position, startPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Locked;
                    if (loadedCartridge != null) currentRoundRemounted = true;
                    if (wentToFrontSinceLastLock) Lock(true);
                    Util.PlayRandomAudioSource(rackSounds);
                }
                else if (Util.AbsDist(bolt.position, endPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
                    Util.PlayRandomAudioSource(pullSounds);
                    wentToFrontSinceLastLock = true;
                    if (closedSinceLastEject) EjectRound();
                    closedSinceLastEject = false;
                }
                else if (state != BoltState.Moving && Util.AbsDist(bolt.position, endPoint.position) > pointTreshold && Util.AbsDist(bolt.position, startPoint.position) > pointTreshold)
                {
                    laststate = state;
                    state = BoltState.Moving;
                }
                //loading
                if (state == BoltState.Moving && (laststate == BoltState.Back || laststate == BoltState.LockedBack))
                {
                    if (roundLoadPoint != null && behindLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                    {
                        if (loadedCartridge == null) TryLoadRound();
                        behindLoadPoint = false;
                    }
                    else if (roundLoadPoint != null && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition)) behindLoadPoint = true;
                }
            }
            //firing
            if (state == BoltState.Locked && firearm.triggerState && firearm.fireMode != Firearm.FireModes.Safe)
            {
                if (firearm.fireMode == Firearm.FireModes.Semi && (slamFire || shotsSinceTriggerReset == 0)) TryFire();
            }
        }

        private void EjectRound()
        {
            if (loadedCartridge == null) return;
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
            Rigidbody rb = c.GetComponent<Rigidbody>();
            c.item.disallowDespawn = false;
            c.item.disallowRoomDespawn = false;
            c.transform.parent = null;
            rb.isKinematic = false;
            rb.WakeUp();
            if (roundEjectDir != null) rb.AddForce(roundEjectDir.forward * roundEjectForce, ForceMode.Impulse);
            c.ToggleHandles(true);
        }

        private void TryLoadRound()
        {
            if (loadedCartridge == null && firearm.magazineWell != null && firearm.magazineWell.ConsumeRound() is Cartridge c)
            {
                loadedCartridge = c;
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;
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

        public override bool ForceLoadChamber(Cartridge c)
        {
            if (loadedCartridge == null)
            {
                loadedCartridge = c;
                c.item.disallowDespawn = true;
                c.item.disallowRoomDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;
                SaveChamber(c.item.itemId);
                return true;
            }
            return false;
        }

        public override void TryRelease()
        {
            if (lockJoint != null) Lock(false);
        }
    }
}
