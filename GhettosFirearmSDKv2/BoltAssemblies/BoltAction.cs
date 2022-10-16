using System.Collections;
using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;

namespace GhettosFirearmSDKv2
{
    public class BoltAction : BoltBase
    {
        public Rigidbody handle;
        Handle bhandle;
        public Transform bolt;
        public Transform rotatingChild;
        public Transform startPoint;
        public Transform endPoint;
        public Transform roundEjectPoint;
        public Transform roundLoadPoint;
        public Transform lockRot;
        public Transform unlockRot;

        public float pointTreshold;

        public AudioSource[] rackSounds;
        public AudioSource[] pullSounds;
        public AudioSource[] lockSounds;
        public AudioSource[] unlockSounds;

        public float roundEjectForce;
        public Transform roundEjectDir;

        public Transform roundMount;
        public Cartridge loadedCartridge;

        bool behindLoadPoint = false;
        bool isHeld;
        int shotsSinceTriggerReset;
        ConfigurableJoint joint;
        bool closedSinceLastEject = false;

        void Awake()
        {
            InitializeJoint();
        }

        private void FixedUpdate()
        {
            UpdateChamberedRound();
            isHeld = bhandle.IsHanded();

            //state check
            if (isHeld)
            {
                bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, handle.transform.localPosition.z);
                rotatingChild.localEulerAngles = new Vector3(rotatingChild.localEulerAngles.x, rotatingChild.localEulerAngles.y, handle.transform.localEulerAngles.z);

                //Go to front
                if (Util.AbsDist(bolt.position, startPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Front;
                    Util.PlayRandomAudioSource(rackSounds);
                }
                //go to back
                else if (Util.AbsDist(bolt.position, endPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
                    Util.PlayRandomAudioSource(pullSounds);
                    if (closedSinceLastEject) EjectRound();
                    closedSinceLastEject = false;
                }
                //unlocking
                else if (Util.AbsDist(bolt.position, startPoint.position) < pointTreshold && state == BoltState.Locked && Vector3.Angle(rotatingChild.localEulerAngles, unlockRot.localEulerAngles) < pointTreshold)
                {
                    state = BoltState.Front;
                    joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((startPoint.localPosition.z - endPoint.localPosition.z) / 2));
                    Util.PlayRandomAudioSource(unlockSounds);
                }
                //locking
                else if (Util.AbsDist(bolt.position, startPoint.position) < pointTreshold && state == BoltState.Front && Vector3.Angle(rotatingChild.localEulerAngles, lockRot.localEulerAngles) < pointTreshold)
                {
                    state = BoltState.Locked;
                    joint.anchor = GrandparentLocalPosition(startPoint, firearm.item.transform);
                    Util.PlayRandomAudioSource(lockSounds);
                }
                //moving
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
            if (state == BoltState.Locked && firearm.triggerState && firearm.fireMode != FirearmBase.FireModes.Safe)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0) TryFire();
            }
        }

        private void EjectRound()
        {
            if (loadedCartridge == null) return;
            firearm.item.RemoveCustomData<ChamberSaveData>();
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

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!isPulled) shotsSinceTriggerReset = 0;
        }

        public override void TryFire()
        {
            if (loadedCartridge == null || loadedCartridge.fired) return;
            foreach (RagdollHand hand in firearm.gameObject.GetComponent<Item>().handlers)
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
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits);
            loadedCartridge.Fire(hits, firearm.actualHitscanMuzzle);
        }

        private void UpdateChamberedRound()
        {
            if (loadedCartridge == null) return;
            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
            loadedCartridge.transform.parent = roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Vector3.zero;
        }

        private void InitializeJoint()
        {
            ConfigurableJoint pJoint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
            handle.transform.localEulerAngles = new Vector3(handle.transform.localEulerAngles.x, handle.transform.localEulerAngles.y, handle.transform.localEulerAngles.z + ((Vector3.Angle(lockRot.localEulerAngles, unlockRot.localEulerAngles) / 2)));
            pJoint.connectedBody = handle;
            joint.anchor = GrandparentLocalPosition(startPoint, firearm.item.transform);
            //pJoint.massScale = 0.00001f;
            pJoint.connectedMassScale = 100f;
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = Vector3.Distance(endPoint.position, startPoint.position) / 2;
            pJoint.linearLimit = limit;
            SoftJointLimit limit2 = new SoftJointLimit();
            limit2.limit = Vector3.Angle(lockRot.localEulerAngles, unlockRot.localEulerAngles);
            pJoint.angularZLimit = limit2;
            pJoint.autoConfigureConnectedAnchor = false;
            pJoint.connectedAnchor = Vector3.zero;
            pJoint.xMotion = ConfigurableJointMotion.Locked;
            pJoint.yMotion = ConfigurableJointMotion.Locked;
            pJoint.zMotion = ConfigurableJointMotion.Limited;
            pJoint.angularXMotion = ConfigurableJointMotion.Locked;
            pJoint.angularYMotion = ConfigurableJointMotion.Locked;
            pJoint.angularZMotion = ConfigurableJointMotion.Limited;
            joint = pJoint;
            handle.transform.localPosition = startPoint.localPosition;
            handle.transform.localRotation = startPoint.localRotation;
            bolt.transform.localEulerAngles = new Vector3(bolt.transform.localEulerAngles.x, bolt.transform.localEulerAngles.y, lockRot.localEulerAngles.z);
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
    }
}