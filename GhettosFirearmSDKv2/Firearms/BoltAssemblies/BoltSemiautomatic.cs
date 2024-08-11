using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class BoltSemiautomatic : BoltBase
    {
        public BoltState chargingHandleState;
        public BoltState previousChargingHandleState;

        public bool isOpenBolt;
        public List<AttachmentPoint> onBoltPoints;
        public bool locksWhenSafetyIsOn;
        public bool hasBoltcatch;
        public bool hasBoltCatchReleaseControl = true;
        public bool chargingHandleLocksBack;
        public bool onlyCatchIfManuallyPulled;
        public bool lockIfNoMagazineFound;
        public bool loadRoundOnPull;
        public BoltReleaseButton[] releaseButtons;
        public List<Handle> boltHandles;
        public Rigidbody rigidBody;
        public Transform bolt;
        public Transform chargingHandle;
        public Transform startPoint;
        public Transform endPoint;
        public Transform catchPoint;
        public Transform akBoltLockPoint;
        public Transform roundLoadPoint;
        public Transform hammerCockPoint;
        public Transform roundMount;
        public Cartridge loadedCartridge;

        private ConfigurableJoint joint;
        public ConstantForce force;

        public AudioSource[] rackSounds;
        public AudioSource[] pullSounds;
        public AudioSource[] chargingHandleRackSounds;
        public AudioSource[] chargingHandlePullSounds;
        public AudioSource[] rackSoundsHeld;
        public AudioSource[] pullSoundsHeld;
        public AudioSource[] rackSoundsNotHeld;
        public AudioSource[] pullSoundsNotHeld;
        public AudioSource[] catchOnSearSounds;

        public float roundEjectForce;
        public Transform roundEjectDir;
        public Transform roundEjectPoint;
        private int shotsSinceTriggerReset;

        private bool isReciprocating;
        private bool isClosing;
        public float startTimeOfMovement;
        private bool letGoBeforeClosed;
        private bool closingAfterRelease;
        private bool closedAfterLoad = true;

        private bool behindLoadPoint;
        private bool beforeLoadPoint = true;
        private bool beforeHammerPoint = true;

        private bool lastFrameHeld;
        public Hammer hammer;
        public bool cockHammerOnTriggerPull;

        public bool overrideHeldState;
        public bool heldState;

        public void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            foreach (var releaseButton in releaseButtons)
            {
                releaseButton.OnReleaseEvent += TryRelease;
            }
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;
            firearm.item.OnHeldActionEvent += BoltSemiautomatic_OnHeldActionEvent;
            firearm.OnAttachmentAddedEvent += Firearm_OnAttachmentAddedEvent;
            firearm.OnAttachmentRemovedEvent += Firearm_OnAttachmentRemovedEvent;

            rigidBody.transform.position = startPoint.position;
            if (firearm.roundsPerMinute == 0 && !rigidBody.gameObject.TryGetComponent(out ConstantForce c)) InitializeJoint(false, false, true);
            else if (locksWhenSafetyIsOn && firearm.fireMode == FirearmBase.FireModes.Safe) InitializeJoint(false, true);
            else InitializeJoint(false);
            if (isOpenBolt)
            {
                CatchBolt(true);
                bolt.localPosition = catchPoint.localPosition;
            }
            UpdateBoltHandles();
            ChamberSaved();
            Invoke(nameof(UpdateChamberedRounds), 1f);

            if (isOpenBolt || !hasBoltCatchReleaseControl)
                disallowRelease = true;
        }

        public override List<Handle> GetNoInfluenceHandles()
        {
            return boltHandles;
        }

        private void Firearm_OnAttachmentRemovedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            UpdateBoltHandles();
        }

        private void Firearm_OnAttachmentAddedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            UpdateBoltHandles();
        }

        public void UpdateBoltHandles()
        {
            boltHandles = new List<Handle>();
            foreach (var h in rigidBody.gameObject.GetComponentsInChildren<Handle>())
            {
                boltHandles.Add(h);
                h.customRigidBody = rigidBody;
            }
            foreach (var point in onBoltPoints)
            {
                foreach (var attachment in point.GetAllChildAttachments())
                {
                    foreach (var handle in attachment.handles)
                    {
                        handle.customRigidBody = rigidBody;
                        boltHandles.Add(handle);
                    }
                }
            }
        }

        private void Firearm_OnFiremodeChangedEvent()
        {
            if (!locksWhenSafetyIsOn) return;
            StartCoroutine(DelayedReSetupBolt());
        }

        private IEnumerator DelayedReSetupBolt()
        {
            yield return new WaitForSeconds(0.03f);
            if (firearm.fireMode == FirearmBase.FireModes.Safe)
            {
                InitializeJoint(false, true);
            }
            else
            {
                InitializeJoint(false, false);
            }
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            if (loadedCartridge == null) return;
            loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
            loadedCartridge.transform.parent = roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
        }

        private void BoltSemiautomatic_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (boltHandles.Contains(handle) && action == Interactable.Action.Ungrab && state != BoltState.Locked)
            {
                letGoBeforeClosed = true;
            }
        }

        public void CatchBolt(bool locked)
        {
            caught = locked;
            laststate = locked? state : BoltState.LockedBack;
            state = locked? BoltState.LockedBack : BoltState.Moving;
            if (!locked)
            {
                //rigidBody.transform.localPosition = new Vector3(rigidBody.transform.localPosition.x, rigidBody.transform.localPosition.y, bolt.localPosition.z);
                closingAfterRelease = true;
                isClosing = true;
                behindLoadPoint = true;
                if (loadedCartridge == null) TryLoadRound();
            }
            else
            {
                isClosing = false;
                closingAfterRelease = false;
                letGoBeforeClosed = false;
                //bolt.localPosition = catchPoint.localPosition;
            }
            InitializeJoint(locked && (chargingHandle == null || chargingHandleLocksBack));
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!isPulled) shotsSinceTriggerReset = 0;
        }

        public override void TryFire()
        {
            if (hammer != null)
            {
                if (cockHammerOnTriggerPull) hammer.Cock();
                hammer.Fire();
            }
            if (!isOpenBolt || loadedCartridge != null)
                shotsSinceTriggerReset++;
            if (loadedCartridge == null || loadedCartridge.fired)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            foreach (var hand in firearm.item.handlers)
            {
                if (hand.playerHand != null && hand.playerHand.controlHand != null) hand.playerHand.controlHand.HapticShort(50f);
            }
            firearm.PlayFireSound(loadedCartridge);
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                firearm.PlayMuzzleFlash(loadedCartridge);
            IncrementBreachSmokeTime();
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, !(firearm.roundsPerMinute > 0 && HeldByAI()));
            if (firearm.roundsPerMinute > 0)
                isReciprocating = true;
            startTimeOfMovement = Time.time;
            InvokeFireEvent();
            InvokeFireLogicFinishedEvent();
        }

        public override Cartridge GetChamber()
        {
            return loadedCartridge;
        }

        public override void TryRelease(bool forced = false)
        {
            if (!hasBoltCatchReleaseControl && !forced)
                return;
            if (caught)
                CatchBolt(false);
        }

        private bool BoltHeld()
        {
            if (overrideHeldState)
                return heldState;
            
            foreach (var handle in boltHandles)
            {
                if (handle.IsHanded()) return true;
            }
            return false;
        }

        public bool MoveBoltWithRB()
        {
            if (!hasBoltcatch && !isOpenBolt) return true;
            var behindCatchpoint = Util.AbsDist(startPoint.localPosition, rigidBody.transform.localPosition) > Util.AbsDist(catchPoint.localPosition, startPoint.localPosition);
            var hasChargingHandle = chargingHandle != null;
            //Debug.Log($"Handle behind lock point: {hasChargingHandle && behindCatchpoint} no charging handle: {!hasChargingHandle} not caught: {!caught} state: {state.ToString()}");
            return (hasChargingHandle && behindCatchpoint) || !hasChargingHandle || !caught;
        }

        private void FixedUpdate()
        {
            if (joint == null) return;

            //UpdateChamberedRound();
            if (caught && letGoBeforeClosed && chargingHandle != null) chargingHandle.localPosition = startPoint.localPosition;
            foreach (var releaseButton in releaseButtons)
            {
                releaseButton.caught = caught;
            }

            isHeld = BoltHeld();
            if (isHeld)
            {
                isReciprocating = false;
            }

            #region non-held lock
            if (isHeld && !rigidBody.gameObject.TryGetComponent(out ConstantForce c) && firearm.roundsPerMinute == 0 && !lastFrameHeld)
            {
                InitializeJoint(false, false, false);
            }
            else if (!isHeld && !rigidBody.gameObject.TryGetComponent(out ConstantForce cf) && firearm.roundsPerMinute == 0 && lastFrameHeld)
            {
                InitializeJoint(false, false, true);
            }
            #endregion non-held lock

            #region held movement
            //state check
            if (isHeld || letGoBeforeClosed || closingAfterRelease)
            {
                if (MoveBoltWithRB()) bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, rigidBody.transform.localPosition.z);
                else bolt.localPosition = catchPoint.localPosition;
                if (chargingHandle != null && (!closingAfterRelease || chargingHandleLocksBack))
                {
                    chargingHandle.localPosition = new Vector3(chargingHandle.localPosition.x, chargingHandle.localPosition.y, rigidBody.transform.localPosition.z);
                }
                
                //Racked
                if (Util.AbsDist(bolt.position, startPoint.position) < Settings.boltPointTreshold && state == BoltState.Moving)
                {
                    bolt.localPosition = startPoint.localPosition;
                    closedAfterLoad = true;
                    letGoBeforeClosed = false;
                    closingAfterRelease = false;
                    laststate = BoltState.Moving;
                    state = BoltState.Locked;
                    Util.PlayRandomAudioSource(rackSounds);
                    Util.PlayRandomAudioSource(rackSoundsHeld);
                }
                //Pulled
                else if (Util.AbsDist(bolt.position, endPoint.position) < Settings.boltPointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
                    previousChargingHandleState = chargingHandleState;
                    chargingHandleState = BoltState.Back;
                    Util.PlayRandomAudioSource(pullSounds);
                    Util.PlayRandomAudioSource(chargingHandlePullSounds);
                    Util.PlayRandomAudioSource(pullSoundsHeld);
                    closingAfterRelease = false;

                    if (closedAfterLoad && firearm.roundsPerMinute != 0)
                        EjectRound();

                    if (CatchOpenBolt() || (((firearm.magazineWell != null && firearm.magazineWell.IsEmptyAndHasMagazine()) || (lockIfNoMagazineFound && firearm.magazineWell.currentMagazine == null)) && loadedCartridge == null && !caught && hasBoltcatch))
                    {
                        CatchBolt(true);
                    }
                    else if ((firearm.magazineWell.currentMagazine == null || !firearm.magazineWell.IsEmpty() || loadedCartridge != null) && caught && hasBoltcatch)
                    {
                        CatchBolt(false);
                    }

                    if (lockIfNoMagazineFound && firearm.magazineWell.currentMagazine == null && loadedCartridge == null && !caught && hasBoltcatch)
                    {
                        CatchBolt(true);
                    }

                    if (CatchOpenBolt())
                    {
                        CatchBolt(true);
                    }
                    
                    if (loadRoundOnPull && loadedCartridge == null)
                        TryLoadRound();
                }
                //caught
                else if (state == BoltState.Moving && caught && Util.AbsDist(catchPoint.localPosition, bolt.localPosition) < Settings.boltPointTreshold)
                {
                    if (chargingHandle == null)
                        letGoBeforeClosed = false;
                    
                    Util.PlayRandomAudioSource(catchOnSearSounds);
                    laststate = state;
                    state = BoltState.LockedBack;
                }
                //moving
                else if (state != BoltState.Moving && !(caught && state == BoltState.LockedBack && Util.AbsDist(bolt.position, catchPoint.position) < Settings.boltPointTreshold) && Util.AbsDist(bolt.position, endPoint.position) > Settings.boltPointTreshold && Util.AbsDist(bolt.position, startPoint.position) > Settings.boltPointTreshold)
                {
                    laststate = state;
                    state = BoltState.Moving;
                }
                //loading
                if (state == BoltState.Moving && (laststate == BoltState.Back || laststate == BoltState.LockedBack))
                {
                    if (roundLoadPoint != null && behindLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                    {
                        if (loadedCartridge == null && !loadRoundOnPull)
                            TryLoadRound();
                        behindLoadPoint = false;
                    }
                    else if (roundLoadPoint != null && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                        behindLoadPoint = true;
                }
                //ejecting
                if (firearm.roundsPerMinute == 0 && state == BoltState.Moving && (laststate == BoltState.Front || laststate == BoltState.Locked))
                {
                    if (roundLoadPoint != null && beforeLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                    {
                        EjectRound();
                        beforeLoadPoint = false;
                    }
                    else if (roundLoadPoint != null && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                        beforeLoadPoint = true;
                }
                //hammer
                if (state == BoltState.Moving && laststate == BoltState.Locked)
                {
                    if (hammer != null && !hammer.cocked && beforeHammerPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                    {
                        hammer.Cock();
                    }
                    else if (hammer != null && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                        beforeHammerPoint = true;
                }

                //Charging handle racked
                if (chargingHandle != null && Util.AbsDist(chargingHandle.position, startPoint.position) < Settings.boltPointTreshold && chargingHandleState == BoltState.Moving)
                {
                    Util.PlayRandomAudioSource(chargingHandleRackSounds);
                    previousChargingHandleState = chargingHandleState;
                    letGoBeforeClosed = false;
                    chargingHandleState = BoltState.Front;
                }
                //Charging handle moving
                else if (chargingHandle != null && chargingHandleState != BoltState.Moving && Util.AbsDist(chargingHandle.position, endPoint.position) > Settings.boltPointTreshold && Util.AbsDist(chargingHandle.position, startPoint.position) > Settings.boltPointTreshold)
                {
                    previousChargingHandleState = chargingHandleState;
                    chargingHandleState = BoltState.Moving;
                }
            }
            #endregion held movement

            #region firing movement
            else if (firearm.roundsPerMinute != 0)
            {
                if (isClosing)
                {
                    bolt.localPosition = Vector3.Lerp(endPoint.localPosition, startPoint.localPosition, BoltLerp(startTimeOfMovement, firearm.roundsPerMinute));
                }

                if (isReciprocating)
                {
                    state = BoltState.Moving;
                    bolt.localPosition = Vector3.Lerp(startPoint.localPosition, endPoint.localPosition, BoltLerp(startTimeOfMovement, firearm.roundsPerMinute));
                    //hammer
                    if (hammer != null && !hammer.cocked && beforeHammerPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                    {
                        hammer.Cock();
                    }
                    else if (hammer != null && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition)) beforeHammerPoint = true;
                }

                if (Util.AbsDist(bolt.localPosition, endPoint.localPosition) < 0.0001f && isReciprocating)
                {
                    isReciprocating = false;
                    if ((firearm.magazineWell.IsEmptyAndHasMagazine() && !caught && hasBoltcatch && !onlyCatchIfManuallyPulled) || (reciprocatingBarrel != null && !reciprocatingBarrel.AllowBoltReturn()) || CatchOpenBolt())
                    {
                        isClosing = false;
                        CatchBolt(true);
                        bolt.localPosition = catchPoint.localPosition;
                        if (isOpenBolt)
                        {
                            EjectRound();
                        }
                    }
                    else
                    {
                        state = BoltState.Moving;
                        startTimeOfMovement = Time.time;
                        isClosing = true;
                        if (isOpenBolt)
                        {
                            EjectRound();
                            TryLoadRound();
                        }
                    }
                    if ((reciprocatingBarrel == null || !reciprocatingBarrel.lockBoltBack) && !isOpenBolt)
                    {
                        EjectRound();
                        TryLoadRound();
                    }
                    Util.PlayRandomAudioSource(pullSounds);
                    Util.PlayRandomAudioSource(pullSoundsNotHeld);
                }
                else if (Util.AbsDist(bolt.localPosition, startPoint.localPosition) < 0.0001f && isClosing && state != BoltState.Locked)
                {
                    closedAfterLoad = true;
                    isClosing = false;
                    isReciprocating = false;
                    state = BoltState.Locked;
                    Util.PlayRandomAudioSource(rackSounds);
                    Util.PlayRandomAudioSource(rackSoundsNotHeld);
                    bolt.localPosition = startPoint.localPosition;
                }
            }
            #endregion firing movement

            #region firing

            if (caught && isOpenBolt && !CatchOpenBolt())
            {
                CatchBolt(false);
            }

            if ((hammer == null || hammer.cocked || cockHammerOnTriggerPull) && ((fireOnTriggerPress && firearm.triggerState) || externalTriggerState || isOpenBolt) && state == BoltState.Locked && (firearm.fireMode != FirearmBase.FireModes.Safe || isOpenBolt))
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) TryFire();
            }
            
            #endregion

            lastFrameHeld = isHeld;
            CalculatePercentage();
        }

        private void Update()
        {
            BaseUpdate();
        }

        private bool CatchOpenBolt()
        {
            if (!isOpenBolt)
                return false;
            if (firearm.fireMode == FirearmBase.FireModes.Semi && shotsSinceTriggerReset == 0 && firearm.triggerState)
                return false;
            else if (firearm.fireMode == FirearmBase.FireModes.Burst && shotsSinceTriggerReset < firearm.burstSize && firearm.triggerState)
                return false;
            else if (firearm.fireMode == FirearmBase.FireModes.Auto && firearm.triggerState)
                return false;
            else
                return true;
        }

        public override void EjectRound()
        {
            if (firearm.magazineWell != null && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired)
                firearm.magazineWell.Eject(true);
            if (loadedCartridge == null)
                return;
            SaveChamber("");
            var c = loadedCartridge;
            loadedCartridge = null;
            if (roundEjectPoint != null)
            {
                c.transform.position = roundEjectPoint.position;
                c.transform.rotation = roundEjectPoint.rotation;
            }
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.ToggleCollision(true);
            Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
            var rb = c.item.physicBody.rigidBody;
            c.item.DisallowDespawn = false;
            c.transform.parent = null;
            c.loaded = false;
            rb.isKinematic = false;
            rb.WakeUp();
            if (roundEjectDir != null) 
            {
                AddForceToCartridge(c, roundEjectDir, roundEjectForce);
                AddTorqueToCartridge(c);
            }
            c.ToggleHandles(true);
            InvokeEjectRound(c);
        }

        public override void TryLoadRound()
        {
            var originallyInfinite = false;
            if (HeldByAI() && firearm.magazineWell?.currentMagazine != null)
            {
                originallyInfinite = firearm.magazineWell.currentMagazine.infinite;
                firearm.magazineWell.currentMagazine.infinite = true;
            }

            if (loadedCartridge == null && firearm.magazineWell != null && firearm.magazineWell.ConsumeRound() is { } c)
            {
                closedAfterLoad = false;
                loadedCartridge = c;
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                SaveChamber(c.item.itemId);
            }

            if (HeldByAI() && !originallyInfinite && firearm.magazineWell?.currentMagazine != null)
            {
                firearm.magazineWell.currentMagazine.infinite = false;
            }
        }

        public override void Initialize()
        {
            rigidBody.transform.position = startPoint.position;
            InitializeJoint(caught);
        }

        private void InitializeJoint(bool lockedBack, bool safetyLocked = false, bool boltActionLocked = false)
        {
            rigidBody.transform.rotation = startPoint.rotation;
            if (joint == null)
            {
                joint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = rigidBody;
                joint.massScale = 0.00001f;
            }
            var limit = new SoftJointLimit();
            if (boltActionLocked)
            {
                joint.anchor = GrandparentLocalPosition(caught? catchPoint : rigidBody.transform, firearm.item.transform);
                limit.limit = 0;
            }
            //default, start to back movement
            else if (!lockedBack && !safetyLocked)
            {
                joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((startPoint.localPosition.z - endPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(endPoint.position, startPoint.position) / 2;
            }
            //locked back, between end point and lock point movement
            else if (lockedBack && !safetyLocked)
            {
                joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((catchPoint.localPosition.z - endPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(endPoint.position, catchPoint.position) / 2;
            }
            else if (safetyLocked && !lockedBack)
            //locked front by safety, between start and ak lock point movement
            {
                joint.anchor = new Vector3(GrandparentLocalPosition(startPoint, firearm.item.transform).x, GrandparentLocalPosition(startPoint, firearm.item.transform).y, GrandparentLocalPosition(startPoint, firearm.item.transform).z + ((akBoltLockPoint.localPosition.z - startPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(startPoint.position, akBoltLockPoint.position) / 2;
            }
            joint.linearLimit = limit;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            if (boltActionLocked) joint.zMotion = ConfigurableJointMotion.Locked;
            //else if (!lockedBack) joint.zMotion = ConfigurableJointMotion.Limited;
            //else joint.zMotion = ConfigurableJointMotion.Free;
            else joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            //if (lockedBack)
            //{
            //    rigidBody.transform.localPosition = catchPoint.localPosition;
            //    rigidBody.transform.localRotation = catchPoint.localRotation;
            //}
            //else if (boltActionLocked) 
            //{
            //    rigidBody.position = oldPos;
            //}
            //else
            //{
            //    rigidBody.transform.localPosition = startPoint.localPosition;
            //    rigidBody.transform.localRotation = startPoint.localRotation;
            //}
        }

        private float BoltLerp(float startTime, float rpm)
        {
            var timeThatPassed = Time.time - startTime;
            var timeForOneRound = 60f / rpm;
            return timeThatPassed / (timeForOneRound / 2f);
        }

        public override bool LoadChamber(Cartridge c, bool forced)
        {
            if (loadedCartridge == null && (state != BoltState.Locked || forced) && !c.loaded)
            {
                loadedCartridge = c;
                c.item.DisallowDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = roundMount;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                SaveChamber(c.item.itemId);
                return true;
            }
            return false;
        }

        public void CalculatePercentage()
        {
            var distanceStartBolt = Util.AbsDist(bolt, startPoint);
            var totalDistance = Util.AbsDist(startPoint, endPoint);
            cyclePercentage = Mathf.Clamp01(distanceStartBolt / totalDistance);
        }
    }
}