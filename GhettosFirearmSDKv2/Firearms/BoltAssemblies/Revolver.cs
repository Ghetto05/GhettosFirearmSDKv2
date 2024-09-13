using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GhettosFirearmSDKv2
{
    public class Revolver : BoltBase
    {
        [Header("FOLDING")]
        public List<Handle> foregripHandles;

        public List<AttachmentPoint> foldAttachmentPoints;
        private HingeJoint _foldJoint;
        public Rigidbody foldBody;
        public Transform foldAxis;
        public Vector3 foldingAxis;
        public float minFoldAngle;
        public float maxFoldAngle;
        public float foldingDamper;

        [Space]
        public Transform foldClosedPosition;

        public Transform foldOpenedPosition;

        [Space]
        public Transform latchAxis;

        public Transform latchClosedPosition;
        public Transform latchOpenedPosition;

        [Space]
        [Header("ROTATE")]
        private HingeJoint _rotateJoint;

        public Rigidbody rotateBody;
        public Transform rotateAxis;
        public Vector3 rotatingAxis;
        public Transform rotateRoot;
        public float rotatingDamper;

        [Space]
        public Transform chamberPicker;

        public List<Transform> chamberLocators;
        public List<float> chamberRotations;

        public bool rotateWhenReleasingTrigger;
        public float triggerPullForTrigger;
        public float triggerPullMax;
        public Transform triggerAxis;
        public Transform triggerIdlePosition;
        public Transform triggerPulledPosition;
        public float triggerPull;
        public List<AudioSource> triggerPullSound;
        public List<AudioSource> triggerResetSound;
        public float onTriggerWeight = 0.8f;

        [Space]
        [Header("AUTO ROTATE")]
        public bool autoRotateCylinder;
        public bool limitCylinderRotation;
        private bool _autoTurnOnNextTriggerRelease;
        private bool _autoTurning;
        private float _autoRotateStartTime;

        [HideInInspector]
        public bool cocked;

        [HideInInspector]
        [Space]
        [Header("HAMMER")]
        public bool singleActionOnly;

        public bool pullHammerWhenOpened;
        public bool returnedTriggerSinceHammer = true;
        public Transform hammerAxis;
        public Transform hammerIdlePosition;
        public Transform hammerCockedPosition;
        public List<AudioSource> hammerHitSounds;
        public List<AudioSource> hammerCockSounds;
        public Collider cockCollider;

        [Space]
        [Header("LOADING")]
        public bool autoEject;

        public Transform ejectDir;
        public float ejectForce;
        public List<string> calibers;
        public List<Transform> mountPoints;
        public List<Collider> loadColliders;
        private Cartridge[] _loadedCartridges;
        public Transform ejectorRoot;
        public Transform ejectorStart;
        public Transform ejectorEnd;

        [Space]
        [Header("AUDIO")]
        public List<AudioSource> lockSounds;

        public List<AudioSource> unlockSounds;
        public List<AudioSource> ejectSounds;
        public List<AudioSource> loadSounds;
        public List<AudioSource> chamberClickSounds;

        private bool _closed;
        private float _lastOpenTime;
        private bool _allowInsert;
        private MagazineSaveData _data;
        private int _shotsSinceTriggerReset;
        private int _currentChamber;
        private bool _ejectedSinceLastOpen;
        private bool _afterCockAction;
        private float _lastTriggerPull;

        public bool useGravityEject = true;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime + 0.2f);
        }

        public void InvokedStart()
        {
            Lock(true);
            rotateBody.gameObject.AddComponent<CollisionRelay>().OnCollisionEnterEvent += OnCollisionEvent;
            _loadedCartridges = new Cartridge[mountPoints.Count];
            firearm.OnCockActionEvent += Firearm_OnCockActionEvent;
            firearm.OnCollisionEventTR += Firearm_OnCollisionEventTR;
            firearm.OnActionEvent += FirearmOnOnActionEvent;
            firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;

            if (firearm.item.TryGetCustomData(out _data))
            {
                for (var i = 0; i < _data.Contents.Length; i++)
                {
                    if (_data.Contents[i] != null)
                    {
                        var index = i;
                        Util.SpawnItem(_data.Contents[index], "Bolt Chamber", ci =>
                        {
                            var c = ci.GetComponent<Cartridge>();
                            LoadChamber(index, c, false);
                        }, transform.position + Vector3.up * 3);
                    }
                }

                UpdateChamberedRounds();
            }
            else
            {
                firearm.item.AddCustomData(new MagazineSaveData());
                firearm.item.TryGetCustomData(out _data);
                _data.Contents = new string[_loadedCartridges.Length];
            }

            _allowInsert = true;
            UpdateChamberedRounds();
        }

        private void FirearmOnOnTriggerChangeEvent(bool ispulled)
        {
            if (!ispulled && _autoTurnOnNextTriggerRelease)
            { 
                _autoTurning = true;
                _autoRotateStartTime = Time.time;
                _autoTurnOnNextTriggerRelease = false;
            }
        }

        private void FirearmOnOnActionEvent(Interactable.Action action)
        {
            if (action == Interactable.Action.UseStop)
            {
                _afterCockAction = false;
            }
        }

        private void Firearm_OnCollisionEventTR(CollisionInstance collisionInstance)
        {
            if (cockCollider != null && collisionInstance.sourceCollider == cockCollider && collisionInstance.targetCollider.GetComponentInParent<Player>() != null)
            {
                ApplyNextChamber(false);
                Cock();
            }
        }

        private void Firearm_OnCockActionEvent()
        {
            if (hammerAxis != null)
            {
                if (cocked)
                    Uncock();
                else
                {
                    _afterCockAction = true;
                    Cock();
                    ApplyNextChamber(false);
                }
            }
        }

        public void OnCollisionEvent(Collision collision)
        {
            if (_allowInsert && collision.collider.GetComponentInParent<Cartridge>() is { } car && !car.loaded)
            {
                foreach (var insertCollider in loadColliders)
                {
                    if (Util.CheckForCollisionWithThisCollider(collision, insertCollider))
                    {
                        var index = loadColliders.IndexOf(insertCollider);
                        LoadChamber(index, car);
                    }
                }
            }
        }

        public void LoadChamber(int index, Cartridge cartridge, bool overrideSave = true)
        {
            if (_loadedCartridges[index] == null && Util.AllowLoadCartridge(cartridge, calibers[index]))
            {
                if (overrideSave)
                    Util.PlayRandomAudioSource(loadSounds);
                _loadedCartridges[index] = cartridge;
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

        private void FixedUpdate()
        {
            if (_closed)
            {
                //Trigger pull
                if (firearm.item.mainHandleRight.handlers.Count > 0)
                {
                    var hand = firearm.item.mainHandleRight.handlers[0].playerHand.controlHand;
                    if (hand.alternateUsePressed)
                        triggerPull = 0f;
                    else
                        triggerPull = Mathf.Clamp01(hand.useAxis / (triggerPullMax - Settings.revolverTriggerDeadzone));
                }
                else triggerPull = 0;

                if (Mathf.Approximately(triggerPull, 0f))
                {
                    if (!autoRotateCylinder && rotateWhenReleasingTrigger && !returnedTriggerSinceHammer)
                        ApplyNextChamber(true);
                    returnedTriggerSinceHammer = true;
                    if (triggerAxis != null && _shotsSinceTriggerReset > 0)
                        Util.PlayRandomAudioSource(triggerResetSound);
                    _shotsSinceTriggerReset = 0;
                }

                if (firearm.setUpForHandPose)
                {
                    foreach (var h in firearm.AllTriggerHandles().Where(h => h != null))
                    {
                        if (h.handlers.Count > 0)
                        {
                            float weight;
                            if (PlayerControl.GetHand(h.handlers[0].side).usePressed)
                            {
                                weight = onTriggerWeight + (1 - onTriggerWeight) * triggerPull;
                                _lastTriggerPull = Time.time;
                            }
                            else if (Time.time - _lastTriggerPull <= Settings.triggerDisciplineTime)
                                weight = onTriggerWeight;
                            else
                                weight = 0f;

                            h.handlers[0].poser.SetTargetWeight(weight);
                        }
                    }
                }

                //Hammer
                if (hammerAxis != null && !singleActionOnly)
                {
                    if (!cocked && triggerPull >= 1f && returnedTriggerSinceHammer && !singleActionOnly)
                    {
                        Cock();
                        ApplyNextChamber(true);
                        TryFire();
                    }

                    if (!cocked && returnedTriggerSinceHammer)
                    {
                        hammerAxis.localEulerAngles = new Vector3(Mathf.Lerp(hammerIdlePosition.localEulerAngles.x, hammerCockedPosition.localEulerAngles.x, triggerPull), 0, 0);
                    }
                }

                //Cylinder
                if (!autoRotateCylinder && (!cocked || hammerAxis == null) && !rotateWhenReleasingTrigger && !singleActionOnly && returnedTriggerSinceHammer && _closed && firearm.fireMode != FirearmBase.FireModes.Safe)
                {
                    if (_shotsSinceTriggerReset == 0 && !cocked)
                        rotateAxis.localEulerAngles = Vector3.Lerp(new Vector3(0, 0, chamberRotations[_currentChamber]), GetNextTargetRotation(), triggerPull);
                }

                if (!autoRotateCylinder && (!cocked || hammerAxis == null) && rotateWhenReleasingTrigger && !singleActionOnly && !returnedTriggerSinceHammer && _closed && firearm.fireMode != FirearmBase.FireModes.Safe)
                {
                    if (_shotsSinceTriggerReset == 1 && !cocked)
                        rotateAxis.localEulerAngles = Vector3.Lerp(new Vector3(0, 0, chamberRotations[_currentChamber]), GetNextTargetRotation(), 1 - triggerPull);
                }

                if (autoRotateCylinder && _autoTurning)
                {
                    var time = Mathf.Clamp01((Time.time - _autoRotateStartTime) / (60f / firearm.roundsPerMinute));
                    if (time.IsApproximately(1))
                    {
                        _autoTurning = false;
                        ApplyNextChamber(true);
                    }
                    else
                    {
                        rotateAxis.localEulerAngles = Vector3.Lerp(new Vector3(0, 0, chamberRotations[_currentChamber]), GetNextTargetRotation(), time);
                    }
                }

                //Trigger
                triggerAxis.localEulerAngles = new Vector3(Mathf.Lerp(triggerIdlePosition.localEulerAngles.x, triggerPulledPosition.localEulerAngles.x, triggerPull), 0, 0);
                if ((cocked || hammerAxis == null) && triggerPull >= triggerPullForTrigger && !_afterCockAction)
                {
                    TryFire();
                    if (hammerAxis != null && returnedTriggerSinceHammer)
                        Util.PlayRandomAudioSource(triggerPullSound);
                }
            }

            if (useGravityEject && !_closed && CheckEjectionGravity(ejectDir))
            {
                EjectCasings();
            }

            //Mathf.Abs(Mathf.Abs(foldBody.transform.localEulerAngles.z) - Mathf.Abs(foldClosedPosition.localEulerAngles.z))
            if (!_closed && Time.time - _lastOpenTime > 0.3f && Quaternion.Angle(foldBody.transform.rotation, foldClosedPosition.rotation) < 1f)
            {
                Lock();
            }

            //Mathf.Abs(Mathf.Abs(foldBody.transform.localEulerAngles.z) - Mathf.Abs(foldOpenedPosition.localEulerAngles.z))
            if (autoEject && !_ejectedSinceLastOpen && Quaternion.Angle(foldBody.transform.rotation, foldOpenedPosition.rotation) < 1f)
            {
                _ejectedSinceLastOpen = true;
                EjectCasings();
            }

            if (ejectorRoot != null)
            {
                if (_closed)
                {
                    ejectorRoot.localPosition = ejectorStart.localPosition;
                }
                else
                {
                    var time = Mathf.Clamp01(Quaternion.Angle(foldClosedPosition.rotation, foldBody.transform.rotation) / Quaternion.Angle(foldClosedPosition.rotation, foldOpenedPosition.rotation));
                    ejectorRoot.localPosition = Vector3.Lerp(ejectorStart.localPosition, ejectorEnd.localPosition, time);
                }
            }
        }

        private void Update()
        {
            BaseUpdate();
        }

        private bool CheckEjectionGravity(Transform t)
        {
            var angle = Vector3.Angle(t.forward, Vector3.down);
            return angle < 50f;
        }

        public void EjectCasings()
        {
            if (_closed || _loadedCartridges == null)
                return;

            for (var i = 0; i < _loadedCartridges.Length; i++)
            {
                TryEjectSingle(i);
            }
        }

        public void TryEjectSingle(int i)
        {
            if (_loadedCartridges[i] != null)
            {
                Util.PlayRandomAudioSource(ejectSounds);
                var c = _loadedCartridges[i];
                _loadedCartridges[i] = null;
                //if (ejectPoints.Count > i && ejectPoints[i] != null)
                //{
                //    c.transform.position = ejectPoints[i].position;
                //    c.transform.rotation = ejectPoints[i].rotation;
                //}
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.ToggleCollision(true);
                Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
                var rb = c.item.physicBody.rigidBody;
                c.item.DisallowDespawn = false;
                c.transform.parent = null;
                c.loaded = false;
                rb.isKinematic = false;
                rb.WakeUp();
                if (ejectDir != null) AddForceToCartridge(c, ejectDir, ejectForce);
                c.ToggleHandles(true);
                InvokeEjectRound(c);
                SaveCartridges();
            }
        }

        public int GetNextChamberIndex() => _currentChamber + 1 == chamberRotations.Count ? 0 : _currentChamber + 1;

        public void Cock()
        {
            if (hammerAxis == null || cocked)
                return;

            hammerAxis.localEulerAngles = hammerCockedPosition.localEulerAngles;
            cocked = true;
            Util.PlayRandomAudioSource(hammerCockSounds);
        }

        public void Uncock()
        {
            if (hammerAxis == null || !cocked)
                return;

            _autoTurning = false;
            hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
            cocked = false;
        }

        public void ApplyNextChamber(bool playSound, bool first = false)
        {
            _autoTurning = false;
            _currentChamber = first ? 0 : GetNextChamberIndex();
            rotateAxis.localEulerAngles = new Vector3(0, 0, chamberRotations[_currentChamber]);
            if (playSound)
                Util.PlayRandomAudioSource(chamberClickSounds);
        }

        public override void TryFire()
        {
            if ((autoRotateCylinder && _autoTurning) || state != BoltState.Locked || (!singleActionOnly && _shotsSinceTriggerReset > 0) || firearm.fireMode == FirearmBase.FireModes.Safe)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            returnedTriggerSinceHammer = false;
            if (hammerAxis != null)
            {
                hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
                cocked = false;
                Util.PlayRandomAudioSource(hammerHitSounds);
            }
            else if (!rotateWhenReleasingTrigger && !autoRotateCylinder)
                ApplyNextChamber(true);

            if (_shotsSinceTriggerReset == 0 && (!limitCylinderRotation || _currentChamber != chamberRotations.Count - 1))
            {
                _autoTurnOnNextTriggerRelease = true;
            }

            _shotsSinceTriggerReset++;

            var ca = GetCurrentChamber();
            if (ca == -1)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            var loadedCartridge = _loadedCartridges[ca];
            if (loadedCartridge == null || loadedCartridge.fired)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            foreach (var hand in firearm.item.handlers)
            {
                if (hand.playerHand != null || hand.playerHand.controlHand != null)
                    hand.playerHand.controlHand.HapticShort(50f);
            }

            IncrementBreachSmokeTime();
            firearm.PlayFireSound(loadedCartridge);
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
            {
                firearm.PlayMuzzleFlash(loadedCartridge);
            }

            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
            loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, !Settings.infiniteAmmo);
            InvokeFireEvent();
            InvokeFireLogicFinishedEvent();
        }

        private int GetCurrentChamber()
        {
            var car = -1;
            for (var i = _loadedCartridges.Length - 1; i >= 0; i--)
            {
                if (Vector3.Distance(chamberPicker.position, chamberLocators[i].position) <= 0.01f)
                    car = i;
            }

            return car;
        }

        private Vector3 GetNextTargetRotation()
        {
            var diff = chamberRotations[1] - chamberRotations[0];
            return new Vector3(0, 0, chamberRotations[_currentChamber] + diff);
        }

        public void Lock(bool initial = false)
        {
            if (_closed) return;

            _ejectedSinceLastOpen = false;
            _closed = true;
            var smallestDistance = 1000f;
            for (var i = 0; i < chamberRotations.Count; i++)
            {
                if (Vector3.Distance(chamberLocators[i].position, chamberPicker.position) < smallestDistance)
                {
                    smallestDistance = Vector3.Distance(chamberLocators[i].position, chamberPicker.position);
                    _currentChamber = i;
                }
            }
            
            if (limitCylinderRotation)
                ApplyNextChamber(false, true);

            if (!initial)
            {
                foreach (var p in foldAttachmentPoints)
                {
                    foreach (var h in p.GetComponentsInChildren<Handle>())
                    {
                        h.SetTouch(true);
                    }
                }

                foreach (var h in foregripHandles)
                {
                    h.SetTouch(true);
                }
            }

            state = BoltState.Locked;
            if (!initial)
                Util.PlayRandomAudioSource(lockSounds);
            InitializeFoldJoint(true);
            InitializeRotateJoint(true);

            if (latchAxis != null)
                latchAxis.SetLocalPositionAndRotation(latchClosedPosition.localPosition, latchClosedPosition.localRotation);

            if (hammerAxis != null && pullHammerWhenOpened && !cocked)
                hammerAxis.SetLocalPositionAndRotation(hammerIdlePosition.localPosition, hammerIdlePosition.localRotation);

            OnClose?.Invoke();
        }

        public Quaternion ToQuaternion(Vector3 vec)
        {
            return Quaternion.Euler(vec.x, vec.y, vec.z);
        }

        public void Unlock()
        {
            if (!_closed) return;

            _closed = false;
            state = BoltState.Moving;
            _lastOpenTime = Time.time;
            Util.PlayRandomAudioSource(unlockSounds);

            foreach (var h in foregripHandles)
            {
                foreach (var hand in h.handlers.ToArray())
                {
                    hand.UnGrab(false);
                }

                h.SetTouch(false);
            }

            foreach (var p in foldAttachmentPoints)
            {
                foreach (var h in p.GetComponentsInChildren<Handle>())
                {
                    h.SetTouch(false);
                }
            }

            InitializeFoldJoint(false);
            InitializeRotateJoint(false);

            if (latchAxis != null)
                latchAxis.SetLocalPositionAndRotation(latchOpenedPosition.localPosition, latchOpenedPosition.localRotation);


            if (hammerAxis != null && pullHammerWhenOpened)
                hammerAxis.SetLocalPositionAndRotation(hammerCockedPosition.localPosition, hammerCockedPosition.localRotation);

            OnOpen?.Invoke();
        }

        public void InitializeFoldJoint(bool close)
        {
            if (close)
            {
                foldBody.transform.localPosition = foldClosedPosition.localPosition;
                foldBody.transform.eulerAngles = foldClosedPosition.eulerAngles;

                foldAxis.SetParent(foldClosedPosition.transform);
                foldAxis.localPosition = Vector3.zero;
                foldAxis.localEulerAngles = Vector3.zero;
            }
            else
            {
                foldBody.transform.localPosition = foldClosedPosition.localPosition;
                foldBody.transform.eulerAngles = foldClosedPosition.eulerAngles;

                foldAxis.SetParent(foldBody.transform);
                foldAxis.localPosition = Vector3.zero;
                foldAxis.localEulerAngles = Vector3.zero;
            }

            if (_foldJoint == null)
            {
                _foldJoint = firearm.item.gameObject.AddComponent<HingeJoint>();
                _foldJoint.connectedBody = foldBody;
                _foldJoint.massScale = 0.00001f;
                _foldJoint.enableCollision = false;
            }

            _foldJoint.autoConfigureConnectedAnchor = false;
            _foldJoint.anchor = GrandparentLocalPosition(foldClosedPosition.transform, firearm.item.transform);
            _foldJoint.connectedAnchor = Vector3.zero;
            _foldJoint.axis = foldingAxis;
            _foldJoint.useLimits = true;
            _foldJoint.enableCollision = false;
            _foldJoint.limits = close ? new JointLimits { min = 0f, max = 0f } : new JointLimits { min = minFoldAngle, max = maxFoldAngle };
        }

        public void InitializeRotateJoint(bool close)
        {
            if (close)
            {
                rotateBody.transform.localPosition = rotateRoot.localPosition;
                rotateBody.transform.localEulerAngles = rotateRoot.localEulerAngles;

                rotateAxis.SetParent(rotateRoot);
                rotateAxis.localPosition = Vector3.zero;
                rotateAxis.localEulerAngles = new Vector3(0, 0, chamberRotations[_currentChamber]);
            }
            else
            {
                rotateBody.transform.localPosition = rotateRoot.localPosition;
                rotateBody.transform.localEulerAngles = new Vector3(0, 0, chamberRotations[_currentChamber]);

                rotateAxis.SetParent(rotateBody.transform);
                rotateAxis.localPosition = Vector3.zero;
                rotateAxis.localEulerAngles = Vector3.zero;
            }

            if (_rotateJoint == null)
            {
                _rotateJoint = foldBody.gameObject.AddComponent<HingeJoint>();
                _rotateJoint.connectedBody = rotateBody;
                _rotateJoint.massScale = 0.00001f;
            }

            _rotateJoint.enableCollision = false;
            _rotateJoint.autoConfigureConnectedAnchor = false;
            _rotateJoint.anchor = GrandparentLocalPosition(rotateRoot.transform, foldBody.transform);
            _rotateJoint.connectedAnchor = Vector3.zero;
            _rotateJoint.axis = rotatingAxis;
            _rotateJoint.useLimits = close;
            _rotateJoint.enableCollision = false;
            _rotateJoint.limits = new JointLimits { min = 0f, max = 0f };
        }

        public override void TryRelease(bool forced = false)
        {
            if (state == BoltState.Locked)
                Unlock();
        }

        public void SaveCartridges()
        {
            _data.Contents = new string[_loadedCartridges.Length];
            for (var i = 0; i < _loadedCartridges.Length; i++)
            {
                _data.Contents[i] = _loadedCartridges[i]?.item.itemId;
            }
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            for (var i = 0; i < mountPoints.Count; i++)
            {
                if (_loadedCartridges[i] != null)
                {
                    _loadedCartridges[i].GetComponent<Rigidbody>().isKinematic = true;
                    _loadedCartridges[i].transform.parent = mountPoints[i];
                    _loadedCartridges[i].transform.localPosition = Vector3.zero;
                    _loadedCartridges[i].transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
            }
        }

        public delegate void OnCloseDelegate();
        public event OnCloseDelegate OnClose;
        public delegate void OnOpenDelegate();
        public event OnOpenDelegate OnOpen;
    }
}