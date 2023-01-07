using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class PumpAction : BoltBase
    {
        public Rigidbody handle;
        Handle bhandle;
        public Transform bolt;
        public Transform startPoint;
        public Transform endPoint;
        public Transform roundEjectPoint;
        public Transform roundLoadPoint;
        public Transform roundMountAfterLock;

        public float pointTreshold;

        public AudioSource[] rackSounds;
        public AudioSource[] pullSounds;

        public bool slamFire;

        public float roundEjectForce;
        public Transform roundEjectDir;

        public Transform roundMount;
        public Cartridge loadedCartridge;

        bool behindLoadPoint = false;
        int shotsSinceTriggerReset;
        FixedJoint lockJoint;
        ConfigurableJoint joint;
        bool closedSinceLastEject = false;
        bool wentToFrontSinceLastLock = false;

        public void Awake()
        {
            bhandle = handle.gameObject.GetComponentInChildren<Handle>();
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            firearm.item.OnGrabEvent += Item_OnGrabEvent;
            firearm.item.OnUngrabEvent += Item_OnUngrabEvent;
            Initialize();
            StartCoroutine(delayedGetChamber());
            Lock(true);
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            //SetStateOnAllHandlers(lockJoint != null);
        }

        private void UpdateChamberedRound()
        {
            if (loadedCartridge == null) return;
            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
            loadedCartridge.transform.parent = roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Vector3.zero;
        }

        public void SetStateOnAllHandlers(bool locked)
        {
            if (locked)
            {
                foreach (RagdollHand hand in bhandle.handlers.ToArray())
                {
                    //if (hand.gripInfo is Handle.GripInfo g)
                    //{
                    //    //g.joint.connectedBody = firearm.item.rb;
                    //    //g.joint.connectedAnchor = firearm.item.rb.transform.InverseTransformPoint(hand.gripInfo.transform.position);
                    //    //g.playerJoint.connectedBody = firearm.item.rb;
                    //    //g.playerJoint.connectedAnchor = firearm.item.rb.transform.InverseTransformPoint(g.transform.position);
                    //}

                    hand.UnGrab(false);

                    bhandle.customRigidBody = null;
                    bhandle.rb = firearm.item.rb;

                    hand.Grab(bhandle, true);
                }
            }
            else
            {
                foreach (RagdollHand hand in bhandle.handlers.ToArray())
                {
                    //if (hand.gripInfo is Handle.GripInfo g)
                    //{
                    //    //g.joint.connectedBody = handle;
                    //    //g.joint.connectedAnchor = handle.transform.InverseTransformPoint(hand.gripInfo.transform.position);
                    //    //g.playerJoint.connectedBody = handle;
                    //    //g.playerJoint.connectedAnchor = handle.transform.InverseTransformPoint(g.transform.position);
                    //}

                    hand.UnGrab(false);

                    bhandle.customRigidBody = handle;
                    bhandle.rb = handle;

                    hand.Grab(bhandle, true);
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
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash) firearm.PlayMuzzleFlash();
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.rb, firearm.recoilModifier, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories);
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle);
            Lock(false);
            InvokeFireEvent();
        }

        public override Cartridge GetChamber()
        {
            return loadedCartridge;
        }

        private void Lock(bool locked)
        {
            if (locked)
            {
                bolt.localPosition = startPoint.localPosition;
                handle.transform.localPosition = startPoint.localPosition;
                //if (lockJoint == null) lockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
                //lockJoint.connectedBody = handle;
                //lockJoint.connectedMassScale = 100f;
                closedSinceLastEject = true;
                wentToFrontSinceLastLock = false;
                //Destroy(joint);
                SetStateOnAllHandlers(true);
            }
            else if (lockJoint != null)
            {
                //InitializeJoint();
                //Destroy(lockJoint);
                SetStateOnAllHandlers(false);
            }
        }

        private void FixedUpdate()
        {
            UpdateChamberedRound();
            isHeld = bhandle.IsHanded();

            //state check
            if (isHeld)
            {
                if (lockJoint == null) bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, handle.transform.localPosition.z);
                if (Util.AbsDist(bolt.position, startPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Locked;
                    if (roundMountAfterLock != null && loadedCartridge != null)
                    {
                        loadedCartridge.transform.parent = roundMount;
                        loadedCartridge.transform.localPosition = Vector3.zero;
                        loadedCartridge.transform.localEulerAngles = Vector3.zero;
                    }
                    if (wentToFrontSinceLastLock) Lock(true);
                    Util.PlayRandomAudioSource(rackSounds);
                }
                else if (Util.AbsDist(bolt.position, endPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
                    Util.PlayRandomAudioSource(pullSounds);
                    wentToFrontSinceLastLock = true;
                    if (closedSinceLastEject) EjectRound(true);
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
            if (fireOnTriggerPress && state == BoltState.Locked && firearm.triggerState && firearm.fireMode != FirearmBase.FireModes.Safe)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && (slamFire || shotsSinceTriggerReset == 0)) TryFire();
            }
        }

        private void EjectRound(bool manual)
        {
            if (loadedCartridge == null) return;
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
            c.item.disallowRoomDespawn = false;
            c.transform.parent = null;
            rb.isKinematic = false;
            rb.WakeUp();
            if (roundEjectDir != null) rb.AddForce(roundEjectDir.forward * roundEjectForce, ForceMode.Impulse);
            c.ToggleHandles(true);
            base.InvokeEjectRound(manual, c);
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

        public override void Initialize()
        {
            if (joint != null) Destroy(joint);
            joint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = handle;
            joint.massScale = 0.00001f;
            //joint.connectedMassScale = 9999f;
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
            handle.transform.localPosition = startPoint.localPosition;
            handle.transform.localRotation = startPoint.localRotation;
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
