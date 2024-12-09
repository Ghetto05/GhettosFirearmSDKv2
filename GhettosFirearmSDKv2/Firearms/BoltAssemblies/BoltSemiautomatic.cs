using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

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
        private Transform _chamberPositionRoundMount;

        private ConfigurableJoint _joint;
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
        private int _shotsSinceTriggerReset;

        private bool _isReciprocating;
        private bool _isClosing;
        public float startTimeOfMovement;
        private bool _letGoBeforeClosed;
        private bool _closingAfterRelease;
        private bool _closedAfterLoad = true;
        private bool _pressedTriggerWhileMoving;

        private bool _behindLoadPoint;
        private bool _beforeLoadPoint = true;
        private bool _beforeHammerPoint = true;

        private bool _lastFrameHeld;
        public Hammer hammer;
        public bool cockHammerOnTriggerPull;

        public bool overrideHeldState;
        public bool heldState;

        private bool _failureToExtract;

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
            if (firearm.roundsPerMinute == 0 && !rigidBody.gameObject.TryGetComponent(out ConstantForce _)) InitializeJoint(false, false, true);
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

            // ReSharper disable once UseObjectOrCollectionInitializer
            var chamber = new GameObject("ChamberPos");
            chamber.transform.parent = roundMount.parent;
            chamber.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _chamberPositionRoundMount = chamber.transform;
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
            boltHandles.AddRange(rigidBody.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item));
            boltHandles.AddRange(bolt.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item));
            if (chargingHandle)
                boltHandles.AddRange(chargingHandle.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item));
            foreach (var h in boltHandles)
            {
                h.customRigidBody = rigidBody;
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
                InitializeJoint(false);
            }
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            if (!loadedCartridge)
                return;
            loadedCartridge.item.physicBody.isKinematic = true;
            loadedCartridge.transform.parent = _failureToExtract ? _chamberPositionRoundMount : _failureToEject && state == BoltState.Locked ? stovepipeRoundPosition : roundMount;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
        }

        private void BoltSemiautomatic_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (boltHandles.Contains(handle) && action == Interactable.Action.Ungrab && state != BoltState.Locked)
            {
                _letGoBeforeClosed = true;
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
                _closingAfterRelease = true;
                _isClosing = true;
                _behindLoadPoint = true;
                if (!loadedCartridge) TryLoadRound();
            }
            else
            {
                _isClosing = false;
                _closingAfterRelease = false;
                _letGoBeforeClosed = false;
                _pressedTriggerWhileMoving = false;
                //bolt.localPosition = catchPoint.localPosition;
            }
            InitializeJoint(locked && (!chargingHandle || chargingHandleLocksBack));
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!isPulled) _shotsSinceTriggerReset = 0;
        }

        public override void TryFire()
        {
            if (hammer)
            {
                if (cockHammerOnTriggerPull) hammer.Cock();
                hammer.Fire();
            }
            if (!isOpenBolt || loadedCartridge)
                _shotsSinceTriggerReset++;
            var failureToFire = Util.DoMalfunction(Settings.malfunctionFailureToFire, Settings.failureToFireChance,
                firearm.malfunctionChanceMultiplier);
            if (!loadedCartridge || loadedCartridge.Fired || failureToFire)
            {
                if (failureToFire)
                    loadedCartridge?.DisableCartridge();
                InvokeFireLogicFinishedEvent();
                return;
            }

            foreach (var hand in firearm.item.handlers)
            {
                if (hand.playerHand && hand.playerHand.controlHand != null) hand.playerHand.controlHand.HapticShort(50f);
            }
            firearm.PlayFireSound(loadedCartridge);
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                firearm.PlayMuzzleFlash(loadedCartridge);
            IncrementBreachSmokeTime();
            FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, !(firearm.roundsPerMinute > 0 && HeldByAI()));
            if (firearm.roundsPerMinute > 0)
                _isReciprocating = true;
            startTimeOfMovement = Time.time;
            InvokeFireEvent();
            InvokeFireLogicFinishedEvent();
            SaveChamber(loadedCartridge?.item.itemId, loadedCartridge?.Fired ?? false);
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

        public bool MoveBoltWithRb()
        {
            if (!hasBoltcatch && !isOpenBolt) return true;
            var behindCatchpoint = Util.AbsDist(startPoint.localPosition, rigidBody.transform.localPosition) > Util.AbsDist(catchPoint.localPosition, startPoint.localPosition);
            var hasChargingHandle = chargingHandle != null;
            //Debug.Log($"Handle behind lock point: {hasChargingHandle && behindCatchpoint} no charging handle: {!hasChargingHandle} not caught: {!caught} state: {state.ToString()}");
            return (hasChargingHandle && behindCatchpoint) || !hasChargingHandle || !caught;
        }

        private void FixedUpdate()
        {
            if (!_joint) return;

            //UpdateChamberedRound();
            if (caught && _letGoBeforeClosed && chargingHandle) chargingHandle.localPosition = startPoint.localPosition;
            foreach (var releaseButton in releaseButtons)
            {
                releaseButton.caught = caught;
            }

            isHeld = BoltHeld();
            if (isHeld)
            {
                _isReciprocating = false;
            }

            #region non-held lock for bolt actions
            if (isHeld && !rigidBody.gameObject.TryGetComponent(out ConstantForce _) && firearm.roundsPerMinute == 0 && !_lastFrameHeld)
            {
                InitializeJoint(false);
            }
            else if (!isHeld && !rigidBody.gameObject.TryGetComponent(out ConstantForce _) && firearm.roundsPerMinute == 0 && _lastFrameHeld)
            {
                InitializeJoint(false, false, true);
            }
            #endregion

            #region held movement
            //state check
            if (isHeld || _letGoBeforeClosed || _closingAfterRelease)
            {
                if (MoveBoltWithRb()) bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, rigidBody.transform.localPosition.z);
                else bolt.localPosition = catchPoint.localPosition;
                if (chargingHandle && (!_closingAfterRelease || chargingHandleLocksBack))
                {
                    chargingHandle.localPosition = new Vector3(chargingHandle.localPosition.x, chargingHandle.localPosition.y, rigidBody.transform.localPosition.z);
                }
                
                if (!caught &&
                    hasBoltcatch &&
                    state == BoltState.Moving &&
                    boltHandles.Any(x => x.handlers.Any(hand => hand.playerHand?.controlHand?.usePressed == true)) &&
                    Util.AbsDist(catchPoint.localPosition, bolt.localPosition) < Settings.boltPointTreshold)
                {
                    _pressedTriggerWhileMoving = true;
                    CatchBolt(true);
                }
                
                //racked
                if (Util.AbsDist(bolt.position, startPoint.position) < Settings.boltPointTreshold && state == BoltState.Moving)
                {
                    bolt.localPosition = startPoint.localPosition;
                    _closedAfterLoad = true;
                    _letGoBeforeClosed = false;
                    _closingAfterRelease = false;
                    _failureToExtract = false;
                    laststate = BoltState.Moving;
                    state = BoltState.Locked;
                    Util.PlayRandomAudioSource(rackSounds);
                    Util.PlayRandomAudioSource(rackSoundsHeld);
                }
                //pulled
                else if (Util.AbsDist(bolt.position, endPoint.position) < Settings.boltPointTreshold && state == BoltState.Moving)
                {
                    laststate = BoltState.Moving;
                    state = BoltState.Back;
                    previousChargingHandleState = chargingHandleState;
                    chargingHandleState = BoltState.Back;
                    Util.PlayRandomAudioSource(pullSounds);
                    Util.PlayRandomAudioSource(chargingHandlePullSounds);
                    Util.PlayRandomAudioSource(pullSoundsHeld);
                    _closingAfterRelease = false;

                    if (_closedAfterLoad && firearm.roundsPerMinute != 0)
                        EjectRound();

                    if (CatchOpenBolt() || (((firearm.magazineWell && firearm.magazineWell.IsEmptyAndHasMagazine()) || (lockIfNoMagazineFound && firearm.magazineWell?.currentMagazine == null)) && loadedCartridge == null && !caught && hasBoltcatch))
                    {
                        CatchBolt(true);
                    }
                    else if ((firearm.magazineWell?.currentMagazine == null || !firearm.magazineWell.IsEmpty() || loadedCartridge) && caught && hasBoltcatch && !_pressedTriggerWhileMoving)
                    {
                        CatchBolt(false);
                    }

                    if (lockIfNoMagazineFound && firearm.magazineWell?.currentMagazine == null && loadedCartridge == null && !caught && hasBoltcatch)
                    {
                        CatchBolt(true);
                    }

                    if (CatchOpenBolt())
                    {
                        CatchBolt(true);
                    }
                    
                    if (loadRoundOnPull && !loadedCartridge)
                        TryLoadRound();
                }
                //caught
                else if (state == BoltState.Moving && caught && Util.AbsDist(catchPoint.localPosition, bolt.localPosition) < Settings.boltPointTreshold)
                {
                    if (!chargingHandle)
                        _letGoBeforeClosed = false;

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
                    if (roundLoadPoint && _behindLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                    {
                        if (!loadedCartridge && !loadRoundOnPull)
                            TryLoadRound();
                        _behindLoadPoint = false;
                    }
                    else if (roundLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                        _behindLoadPoint = true;
                }
                //ejecting
                if (firearm.roundsPerMinute == 0 && state == BoltState.Moving && (laststate == BoltState.Front || laststate == BoltState.Locked))
                {
                    if (roundLoadPoint && _beforeLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                    {
                        EjectRound();
                        _beforeLoadPoint = false;
                    }
                    else if (roundLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                        _beforeLoadPoint = true;
                }
                //hammer
                if (state == BoltState.Moving && laststate == BoltState.Locked)
                {
                    if (hammer && !hammer.cocked && _beforeHammerPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                    {
                        hammer.Cock();
                    }
                    else if (hammer && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                        _beforeHammerPoint = true;
                }

                //Charging handle racked
                if (chargingHandle && Util.AbsDist(chargingHandle.position, startPoint.position) < Settings.boltPointTreshold && chargingHandleState == BoltState.Moving)
                {
                    Util.PlayRandomAudioSource(chargingHandleRackSounds);
                    previousChargingHandleState = chargingHandleState;
                    _letGoBeforeClosed = false;
                    chargingHandleState = BoltState.Front;
                }
                //Charging handle moving
                else if (chargingHandle && chargingHandleState != BoltState.Moving && Util.AbsDist(chargingHandle.position, endPoint.position) > Settings.boltPointTreshold && Util.AbsDist(chargingHandle.position, startPoint.position) > Settings.boltPointTreshold)
                {
                    previousChargingHandleState = chargingHandleState;
                    chargingHandleState = BoltState.Moving;
                }
            }
            #endregion held movement

            #region firing movement
            else if (firearm.roundsPerMinute != 0)
            {
                if (_isClosing)
                {
                    bolt.localPosition = Vector3.Lerp(endPoint.localPosition, startPoint.localPosition, BoltLerp(startTimeOfMovement, firearm.roundsPerMinute));
                }

                if (_isReciprocating)
                {
                    if (state == BoltState.Locked && !_failureToEject && !_failureToExtract && Util.DoMalfunction(Settings.malfunctionFailureToExtract, Settings.failureToExtractChance, firearm.malfunctionChanceMultiplier))
                    {
                        _failureToExtract = true;
                        UpdateChamberedRounds();
                    }
                    
                    state = BoltState.Moving;
                    bolt.localPosition = Vector3.Lerp(startPoint.localPosition, endPoint.localPosition, BoltLerp(startTimeOfMovement, firearm.roundsPerMinute));
                    //hammer
                    if (hammer && !hammer.cocked && _beforeHammerPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                    {
                        hammer.Cock();
                    }
                    else if (hammer && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition)) _beforeHammerPoint = true;
                }

                if (Util.AbsDist(bolt.localPosition, endPoint.localPosition) < 0.0001f && _isReciprocating)
                {
                    _isReciprocating = false;
                    if ((firearm.magazineWell.IsEmptyAndHasMagazine() && !caught && hasBoltcatch && !onlyCatchIfManuallyPulled) || (reciprocatingBarrel != null && !reciprocatingBarrel.AllowBoltReturn()) || CatchOpenBolt())
                    {
                        _isClosing = false;
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
                        _isClosing = true;
                        if (isOpenBolt)
                        {
                            EjectRound();
                            TryLoadRound();
                        }
                    }
                    //bolt test below
                    if ((reciprocatingBarrel == null || !reciprocatingBarrel.lockBoltBack) && !isOpenBolt)
                    {
                        EjectRound();
                        TryLoadRound();
                    }
                    Util.PlayRandomAudioSource(pullSounds);
                    Util.PlayRandomAudioSource(pullSoundsNotHeld);
                }
                else if (Util.AbsDist(bolt.localPosition, startPoint.localPosition) < 0.0001f && _isClosing && state != BoltState.Locked)
                {
                    _closedAfterLoad = true;
                    _isClosing = false;
                    _isReciprocating = false;
                    state = BoltState.Locked;
                    Util.PlayRandomAudioSource(rackSounds);
                    Util.PlayRandomAudioSource(rackSoundsNotHeld);
                    bolt.localPosition = startPoint.localPosition;
                }

                // var loadPercentage = 0.75f;
                // if ((reciprocatingBarrel == null || !reciprocatingBarrel.lockBoltBack) && !isOpenBolt)
                // {
                //     if (_isReciprocating && _lastCyclePercentage < loadPercentage && cyclePercentage >= loadPercentage)
                //         EjectRound();
                //     if (_isClosing && _lastCyclePercentage > loadPercentage && cyclePercentage <= loadPercentage)
                //         TryLoadRound();
                // }
            }
            #endregion firing movement

            #region firing

            if (caught && isOpenBolt && !CatchOpenBolt())
            {
                CatchBolt(false);
            }

            if ((hammer == null || hammer.cocked || cockHammerOnTriggerPull) && ((fireOnTriggerPress && firearm.triggerState) || externalTriggerState || isOpenBolt) && state == BoltState.Locked && (firearm.fireMode != FirearmBase.FireModes.Safe || isOpenBolt))
            {
                if (firearm.fireMode == FirearmBase.FireModes.Semi && _shotsSinceTriggerReset == 0) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Burst && _shotsSinceTriggerReset < firearm.burstSize) TryFire();
                else if (firearm.fireMode == FirearmBase.FireModes.Auto) TryFire();
            }
            
            #endregion

            _lastFrameHeld = isHeld;
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
            if (firearm.fireMode == FirearmBase.FireModes.Semi && _shotsSinceTriggerReset == 0 && firearm.triggerState)
                return false;

            if (firearm.fireMode == FirearmBase.FireModes.Burst && _shotsSinceTriggerReset < firearm.burstSize && firearm.triggerState)
                return false;
            if (firearm.fireMode == FirearmBase.FireModes.Auto && firearm.triggerState)
                return false;

            return true;
        }

        public override void EjectRound()
        {
            if (firearm.magazineWell && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired)
                firearm.magazineWell.Eject(true);
            if (!loadedCartridge || _failureToExtract)
                return;
            SaveChamber(null, false);
            var c = loadedCartridge;
            loadedCartridge = null;
            if (roundEjectPoint)
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
            if (roundEjectDir) 
            {
                AddForceToCartridge(c, roundEjectDir, roundEjectForce);
                AddTorqueToCartridge(c);
            }
            c.ToggleHandles(true);
            InvokeEjectRound(c);
        }

        public override void TryLoadRound()
        {
            if (_failureToExtract)
                return;
            
            var originallyInfinite = false;
            if (HeldByAI() && firearm.magazineWell?.currentMagazine != null)
            {
                originallyInfinite = firearm.magazineWell.currentMagazine.infinite;
                firearm.magazineWell.currentMagazine.infinite = true;
            }

            if (!loadedCartridge && firearm.magazineWell && firearm.magazineWell.ConsumeRound() is { } c)
            {
                _closedAfterLoad = false;
                loadedCartridge = c;
                UpdateChamberedRounds();
                SaveChamber(c.item.itemId, c.Fired);
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
            if (!_joint)
            {
                _joint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
                _joint.connectedBody = rigidBody;
                _joint.massScale = 0.00001f;
            }
            var limit = new SoftJointLimit();
            if (boltActionLocked)
            {
                _joint.anchor = GrandparentLocalPosition(caught? catchPoint : rigidBody.transform, firearm.item.transform);
                limit.limit = 0;
            }
            //default, start to back movement
            else if (!lockedBack && !safetyLocked)
            {
                _joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((startPoint.localPosition.z - endPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(endPoint.position, startPoint.position) / 2;
            }
            //locked back, between end point and lock point movement
            else if (lockedBack && !safetyLocked)
            {
                _joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + ((catchPoint.localPosition.z - endPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(endPoint.position, catchPoint.position) / 2;
            }
            else if (!lockedBack)
            //locked front by safety, between start and ak lock point movement
            {
                _joint.anchor = new Vector3(GrandparentLocalPosition(startPoint, firearm.item.transform).x, GrandparentLocalPosition(startPoint, firearm.item.transform).y, GrandparentLocalPosition(startPoint, firearm.item.transform).z + ((akBoltLockPoint.localPosition.z - startPoint.localPosition.z) / 2));
                limit.limit = Vector3.Distance(startPoint.position, akBoltLockPoint.position) / 2;
            }
            _joint.linearLimit = limit;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = Vector3.zero;
            _joint.xMotion = ConfigurableJointMotion.Locked;
            _joint.yMotion = ConfigurableJointMotion.Locked;
            if (boltActionLocked) _joint.zMotion = ConfigurableJointMotion.Locked;
            //else if (!lockedBack) joint.zMotion = ConfigurableJointMotion.Limited;
            //else joint.zMotion = ConfigurableJointMotion.Free;
            else _joint.zMotion = ConfigurableJointMotion.Limited;
            _joint.angularXMotion = ConfigurableJointMotion.Locked;
            _joint.angularYMotion = ConfigurableJointMotion.Locked;
            _joint.angularZMotion = ConfigurableJointMotion.Locked;
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
            if (!loadedCartridge && (state != BoltState.Locked || forced) && !c.loaded)
            {
                loadedCartridge = c;
                c.item.DisallowDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                UpdateChamberedRounds();
                SaveChamber(c.item.itemId, c.Fired);
                return true;
            }
            return false;
        }

        public void CalculatePercentage()
        {
            var distanceStartBolt = Util.AbsDist(bolt, startPoint);
            var totalDistance = Util.AbsDist(startPoint, endPoint);
            LastCyclePercentage = cyclePercentage;
            cyclePercentage = Mathf.Clamp01(distanceStartBolt / totalDistance);
        }
    }
}