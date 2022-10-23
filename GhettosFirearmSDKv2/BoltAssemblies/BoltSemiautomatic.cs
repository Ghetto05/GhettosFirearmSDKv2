using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class BoltSemiautomatic : BoltBase
    {
        public List<AttachmentPoint> onBoltPoints;
        public bool isHeld;
        public bool locksWhenSafetyIsOn = false;
        public bool hasBoltcatch;
        public bool hasBoltCatchReleaseControl = true;
        public BoltReleaseButton[] releaseButtons;
        List<Handle> boltHandles;
        public Rigidbody rigidBody;
        public Transform bolt;
        public Transform chargingHandle;
        public Transform startPoint;
        public Transform endPoint;
        public Transform catchPoint;
        public Transform akBoltLockPoint;
        public Transform roundLoadPoint;
        public Transform roundEjectPoint;
        public Transform roundMount;
        public Cartridge loadedCartridge;

        private ConfigurableJoint joint;
        public ConstantForce force;
        public float pointTreshold;

        public AudioSource[] rackSounds;
        public AudioSource[] pullSounds;
        public AudioSource[] chargingHandleRackSounds;
        public AudioSource[] chargingHandlePullSounds;

        public float roundEjectForce;
        public Transform roundEjectDir;
        int shotsSinceTriggerReset = 0;

        bool isReciprocating = false;
        bool isClosing = false;
        float startTimeOfMovement = 0f;
        bool letGoBeforeClosed = false;
        bool closingAfterRelease = false;

        bool behindLoadPoint = false;

        public void Awake()
        {
            foreach (BoltReleaseButton releaseButton in releaseButtons)
            {
                releaseButton.OnReleaseEvent += TryRelease;
            }
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;

            if (locksWhenSafetyIsOn && firearm.fireMode == FirearmBase.FireModes.Safe) InitializeJoint(false, true);
            else InitializeJoint(false);
            UpdateBoltHandles();
            firearm.item.OnHeldActionEvent += BoltSemiautomatic_OnHeldActionEvent;
            StartCoroutine(delayedGetChamber());
        }

        public void UpdateBoltHandles()
        {
            boltHandles = rigidBody.GetComponentsInChildren<Handle>().ToList();
            foreach (AttachmentPoint point in onBoltPoints)
            {
                if (point.currentAttachment != null) boltHandles.AddRange(point.currentAttachment.handles);
            }
        }

        private void Firearm_OnFiremodeChangedEvent()
        {
            if (!locksWhenSafetyIsOn) return;
            if (firearm.fireMode == FirearmBase.FireModes.Safe)
            {
                InitializeJoint(false, true);
            }
            else
            {
                InitializeJoint(false, false);
            }
        }

        private void UpdateChamberedRound()
        {
            if (loadedCartridge == null) return;
            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
            loadedCartridge.transform.parent = roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Vector3.zero;
        }

        private void BoltSemiautomatic_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (boltHandles.Contains(handle) && action == Interactable.Action.Ungrab && state != BoltState.Locked)
            {
                letGoBeforeClosed = true;
            }
        }

        public void LockBoltOnLockPoint(bool locked)
        {
            caught = locked;
            state = BoltState.LockedBack;
            if (!locked)
            {
                rigidBody.transform.localPosition = new Vector3(rigidBody.transform.localPosition.x, rigidBody.transform.localPosition.y, bolt.localPosition.z);
                closingAfterRelease = true;
                isClosing = true;
            }
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!isPulled) shotsSinceTriggerReset = 0;
        }

        public override void TryFire()
        {
            if (loadedCartridge == null || loadedCartridge.fired) return;
            shotsSinceTriggerReset++;
            foreach (RagdollHand hand in firearm.gameObject.GetComponent<Item>().handlers)
            {
                if (hand.playerHand == null || hand.playerHand.controlHand == null) return;
                hand.playerHand.controlHand.HapticShort(50f);
            }
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
            isReciprocating = true;
            startTimeOfMovement = Time.time;
        }

        public override void TryRelease()
        {
            if (!hasBoltCatchReleaseControl) return;
            LockBoltOnLockPoint(false);
        }

        private bool BoltHeld()
        {
            foreach (Handle handl in boltHandles)
            {
                if (handl.IsHanded()) return true;
            }
            return false;
        }

        private void FixedUpdate()
        {
            UpdateChamberedRound();
            if (caught && letGoBeforeClosed && chargingHandle != null) chargingHandle.localPosition = startPoint.localPosition;
            foreach (BoltReleaseButton releaseButton in releaseButtons)
            {
                releaseButton.caught = caught;
            }

            isHeld = BoltHeld();
            #region held movement
            //state check
            if (isHeld || letGoBeforeClosed || closingAfterRelease)
            {
                bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, rigidBody.transform.localPosition.z);
                if (chargingHandle != null && !closingAfterRelease)
                {
                    chargingHandle.localPosition = new Vector3(chargingHandle.localPosition.x, chargingHandle.localPosition.y, rigidBody.transform.localPosition.z);
                }
                
                //Racked
                if (Util.AbsDist(bolt.position, startPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    letGoBeforeClosed = false;
                    closingAfterRelease = false;
                    laststate = BoltState.Moving;
                    state = BoltState.Locked;
                    Util.PlayRandomAudioSource(rackSounds);
                    Util.PlayRandomAudioSource(chargingHandleRackSounds);
                }
                //Pulled
                else if (Util.AbsDist(bolt.position, endPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
                    Util.PlayRandomAudioSource(pullSounds);
                    Util.PlayRandomAudioSource(chargingHandlePullSounds);

                    if (firearm.magazineWell.IsEmptyAndHasMagazine() && !caught && hasBoltcatch)
                    {
                        LockBoltOnLockPoint(true);
                    }
                    else if (!firearm.magazineWell.IsEmptyAndHasMagazine() && caught && hasBoltcatch)
                    {
                        LockBoltOnLockPoint(false);
                    }
                    closingAfterRelease = false;

                    EjectRound();
                }
                //Pulled safety lock
                else if (locksWhenSafetyIsOn && firearm.fireMode == FirearmBase.FireModes.Safe && Util.AbsDist(startPoint.position, akBoltLockPoint.position) > 0.01 && Util.AbsDist(bolt.position, akBoltLockPoint.position) < pointTreshold && state == BoltState.Moving)
                {
                    Util.PlayRandomAudioSource(pullSounds);
                    Util.PlayRandomAudioSource(chargingHandlePullSounds);
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
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
            #endregion held movement
            #region firing movement
            else
            {
                if (isClosing)
                {
                    bolt.localPosition = Vector3.Lerp(endPoint.localPosition, startPoint.localPosition, BoltLerp(startTimeOfMovement, firearm.roundsPerMinute));
                }
                if (isReciprocating)
                {
                    bolt.localPosition = Vector3.Lerp(startPoint.localPosition, endPoint.localPosition, BoltLerp(startTimeOfMovement, firearm.roundsPerMinute));
                }

                if (Util.AbsDist(bolt.localPosition, endPoint.localPosition) < 0.0001f && isReciprocating)
                {
                    isReciprocating = false;
                    if (firearm.magazineWell.IsEmptyAndHasMagazine() && !caught && hasBoltcatch)
                    {
                        isClosing = false;
                        LockBoltOnLockPoint(true);
                        bolt.localPosition = catchPoint.localPosition;
                    }
                    else
                    {
                        isClosing = true;
                    }
                    state = BoltState.Moving;
                    startTimeOfMovement = Time.time;
                    EjectRound();
                    TryLoadRound();
                    Util.PlayRandomAudioSource(pullSounds);
                }
                else if (Util.AbsDist(bolt.localPosition, startPoint.localPosition) < 0.0001f && isClosing && state != BoltState.Locked)
                {
                    isClosing = false;
                    isReciprocating = false;
                    state = BoltState.Locked;
                    Util.PlayRandomAudioSource(rackSounds);
                    bolt.localPosition = startPoint.localPosition;
                }
            }
            #endregion firing movement

            //firing
            if (state == BoltState.Locked && firearm.triggerState && firearm.fireMode != FirearmBase.FireModes.Safe)
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) TryFire();
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
            if (loadedCartridge == null && firearm.magazineWell.ConsumeRound() is Cartridge c)
            {
                loadedCartridge = c;
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;
                SaveChamber(c.item.itemId);
            }
        }

        private void InitializeJoint(bool lockedBack, bool safetyLocked = false)
        {
            ConfigurableJoint pJoint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
            pJoint.connectedBody = rigidBody;
            pJoint.massScale = 0.00001f;
            SoftJointLimit limit = new SoftJointLimit();
            //default, start to back movement
            if (!lockedBack && !safetyLocked)
            {
                pJoint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((startPoint.localPosition.z - endPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(endPoint.position, startPoint.position) / 2;
            }
            //locked back, between end point and lock point movement
            else if (lockedBack && !safetyLocked)
            {
                pJoint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((catchPoint.localPosition.z - endPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(endPoint.position, catchPoint.position) / 2;
            }
            else if (safetyLocked && !lockedBack)
            //locked front by safety, between start and ak lock point movement
            {
                pJoint.anchor = new Vector3(GrandparentLocalPosition(startPoint, firearm.item.transform).x, GrandparentLocalPosition(startPoint, firearm.item.transform).y, GrandparentLocalPosition(startPoint, firearm.item.transform).z + ((akBoltLockPoint.localPosition.z - startPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(startPoint.position, akBoltLockPoint.position) / 2;
            }
            pJoint.linearLimit = limit;
            pJoint.autoConfigureConnectedAnchor = false;
            pJoint.connectedAnchor = Vector3.zero;
            pJoint.xMotion = ConfigurableJointMotion.Locked;
            pJoint.yMotion = ConfigurableJointMotion.Locked;
            if (!lockedBack) pJoint.zMotion = ConfigurableJointMotion.Limited;
            else pJoint.zMotion = ConfigurableJointMotion.Free;
            pJoint.angularXMotion = ConfigurableJointMotion.Locked;
            pJoint.angularYMotion = ConfigurableJointMotion.Locked;
            pJoint.angularZMotion = ConfigurableJointMotion.Locked;
            ConfigurableJoint prevJoint = joint;
            joint = pJoint;
            Destroy(prevJoint);
            if (lockedBack)
            {
                rigidBody.transform.localPosition = catchPoint.localPosition;
                rigidBody.transform.localRotation = catchPoint.localRotation;
            }
            else
            {
                rigidBody.transform.localPosition = startPoint.localPosition;
                rigidBody.transform.localRotation = startPoint.localRotation;
            }
        }

        private float BoltLerp(float startTime, int rpm)
        {
            float timeThatPassed = Time.time - startTime;
            float timeForOneRound = 60f / rpm;
            return timeThatPassed / (timeForOneRound / 2f);
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
