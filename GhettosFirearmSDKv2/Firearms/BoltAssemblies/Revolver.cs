using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class Revolver : BoltBase
    {
        [Header("FOLDING")]
        public List<Handle> foregripHandles;
        public List<AttachmentPoint> foldAttachmentPoints;
        private HingeJoint foldJoint;
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
        [Header("ROTATE")]
        private HingeJoint rotateJoint;
        public Rigidbody rotateBody;
        public Transform rotateAxis;
        public Vector3 rotatingAxis;
        public Transform rotateRoot;
        public float rotatingDamper;
        [Space]
        public Transform chamberPicker;
        public List<Transform> chamberLocators;
        public List<float> chamberRotations;

        [Space]
        [Header("TRIGGER")]
        public float EDITORTriggerPullPercentage;
        public float triggerPullForTrigger;
        public float triggerPullMax;
        public Transform triggerAxis;
        public Transform triggerIdlePosition;
        public Transform triggerPulledPosition;
        public float triggerPull;
        public List<AudioSource> triggerPullSound;
        public List<AudioSource> triggerResetSound;

        [HideInInspector]
        public bool cocked;
        [HideInInspector]
        [Space]
        [Header("HAMMER")]
        public bool singleActionOnly = false;
        public bool returnedTriggerSinceHammer = true;
        public Transform hammerAxis;
        public Transform hammerIdlePosition;
        public Transform hammerCockedPosition;
        public List<AudioSource> hammerHitSounds;
        public List<AudioSource> hammerCockSounds;
        public Collider cockCollider;

        [Space]
        [Header("LOADING")]
        public bool autoEject = false;
        public Transform ejectDir;
        public float ejectForce;
        public List<string> calibers;
        public List<Transform> mountPoints;
        public List<Collider> loadColliders;
        private Cartridge[] loadedCartridges;

        [Space]
        [Header("AUDIO")]
        public List<AudioSource> lockSounds;
        public List<AudioSource> unlockSounds;
        public List<AudioSource> ejectSounds;
        public List<AudioSource> loadSounds;

        bool closed = false;
        float lastOpenTime = 0f;
        bool allowInsert = false;
        MagazineSaveData data;
        int shotsSinceTriggerReset = 0;
        int currentChamber = 0;
        bool ejectedSinceLastOpen = false;

        public bool useGravityEject = true;

        private void Start()
        {
            Invoke("InvokedStart", FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            Lock(true);
            rotateBody.gameObject.AddComponent<CollisionRelay>().onCollisionEnterEvent += OnCollisionEvent;
            loadedCartridges = new Cartridge[mountPoints.Count];
            firearm.OnCockActionEvent += Firearm_OnCockActionEvent;
            firearm.OnCollisionEventTR += Firearm_OnCollisionEventTR;

            if (firearm.item.TryGetCustomData(out data))
            {
                for (int i = 0; i < data.contents.Length; i++)
                {
                    if (data.contents[i] != null)
                    {
                        int index = i;
                        Catalog.GetData<ItemData>(data.contents[index]).SpawnAsync(ci => { Cartridge c = ci.GetComponent<Cartridge>(); LoadChamber(index, c, false); }, transform.position + Vector3.up * 3);
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

        private void Firearm_OnCollisionEventTR(CollisionInstance collisionInstance)
        {
            if (cockCollider != null && collisionInstance.sourceCollider == cockCollider && collisionInstance.targetCollider.GetComponentInParent<Player>() != null)
            {
                ApplyNextChamber();
                Cock();
            }
        }

        private void Firearm_OnCockActionEvent()
        {
            if (cocked) Uncock();
            else
            {
                Cock();
                ApplyNextChamber();
            }
        }

        public void OnCollisionEvent(Collision collision)
        {
            if (allowInsert && collision.collider.GetComponentInParent<Cartridge>() is Cartridge car && !car.loaded)
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
            if (loadedCartridges[index] == null && Util.AllowLoadCatridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(loadSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.disallowDespawn = true;
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
            if (closed)
            {
                //Trigger pull
                if (firearm.item.mainHandleRight.handlers.Count > 0)
                {
                    RagdollHand hand = firearm.item.mainHandleRight.handlers[0];
                    if (hand.playerHand.controlHand.alternateUsePressed) triggerPull = 0f;
                    else triggerPull = Mathf.Clamp01(hand.playerHand.controlHand.useAxis / (triggerPullMax - FirearmsSettings.revolverTriggerDeadzone));
                }
                else triggerPull = 0;
                if (Mathf.Approximately(triggerPull, 0f))
                {
                    returnedTriggerSinceHammer = true;
                    if (triggerAxis != null && shotsSinceTriggerReset > 0) Util.PlayRandomAudioSource(triggerResetSound);
                    shotsSinceTriggerReset = 0;
                }

                //Hammer
                if (hammerAxis != null && !singleActionOnly)
                {
                    if (!cocked && triggerPull >= 1f && returnedTriggerSinceHammer && !singleActionOnly)
                    {
                        Cock();
                        ApplyNextChamber();
                        TryFire();
                    }
                    if (!cocked && returnedTriggerSinceHammer)
                    {
                        hammerAxis.localEulerAngles = new Vector3(Mathf.Lerp(hammerIdlePosition.localEulerAngles.x, hammerCockedPosition.localEulerAngles.x, triggerPull), 0, 0);
                    }
                }

                //Cylinder
                if ((!cocked || hammerAxis == null) && !singleActionOnly && returnedTriggerSinceHammer && closed && firearm.fireMode != FirearmBase.FireModes.Safe)
                {
                    if (shotsSinceTriggerReset == 0 && !cocked) rotateAxis.localEulerAngles = Vector3.Lerp(new Vector3(0, 0, chamberRotations[currentChamber]), GetNextTargetRotation(), triggerPull);
                }

                //Trigger
                triggerAxis.localEulerAngles = new Vector3(Mathf.Lerp(triggerIdlePosition.localEulerAngles.x, triggerPulledPosition.localEulerAngles.x, triggerPull), 0, 0);
                if ((cocked || hammerAxis == null) && triggerPull >= triggerPullForTrigger)
                {
                    TryFire();
                    if (hammerAxis != null && returnedTriggerSinceHammer) Util.PlayRandomAudioSource(triggerPullSound);
                }
            }

            if (useGravityEject && !closed && CheckEjectionGravity(ejectDir))
            {
                EjectCasings();
            }

            //Mathf.Abs(Mathf.Abs(foldBody.transform.localEulerAngles.z) - Mathf.Abs(foldClosedPosition.localEulerAngles.z))
            if (!closed && Time.time - lastOpenTime > 0.3f && Quaternion.Angle(foldBody.transform.rotation, foldClosedPosition.rotation) < 1f)
            {
                Lock();
            }

            //Mathf.Abs(Mathf.Abs(foldBody.transform.localEulerAngles.z) - Mathf.Abs(foldOpenedPosition.localEulerAngles.z))
            if (autoEject && !ejectedSinceLastOpen && Quaternion.Angle(foldBody.transform.rotation, foldOpenedPosition.rotation) < 1f)
            {
                ejectedSinceLastOpen = true;
                EjectCasings();
            }
        }

        private bool CheckEjectionGravity(Transform t)
        {
            float angle = Vector3.Angle(t.forward, Vector3.down);
            return angle < 50f;
        }

        public void EjectCasings()
        {
            if (closed) return;
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                TryEjectSingle(i);
            }
        }

        public void TryEjectSingle(int i)
        {
            if (loadedCartridges[i] != null)
            {
                Util.PlayRandomAudioSource(ejectSounds);
                Cartridge c = loadedCartridges[i];
                loadedCartridges[i] = null;
                //if (ejectPoints.Count > i && ejectPoints[i] != null)
                //{
                //    c.transform.position = ejectPoints[i].position;
                //    c.transform.rotation = ejectPoints[i].rotation;
                //}
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.ToggleCollision(true);
                Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
                Rigidbody rb = c.item.physicBody.rigidBody;
                c.item.disallowDespawn = false;
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

        public int GetNextChamberIndex() => currentChamber + 1 == chamberRotations.Count? 0 : currentChamber + 1;

        public void Cock()
        {
            if (hammerAxis == null || cocked) return;
            hammerAxis.localEulerAngles = hammerCockedPosition.localEulerAngles;
            cocked = true;
            Util.PlayRandomAudioSource(hammerCockSounds);
        }

        public void Uncock()
        {
            if (hammerAxis == null || !cocked) return;
            hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
            cocked = false;
        }

        public void ApplyNextChamber()
        {
            currentChamber = GetNextChamberIndex();
            rotateAxis.localEulerAngles = new Vector3(0, 0, chamberRotations[currentChamber]);
        }

        public override void TryFire()
        {
            if (state != BoltState.Locked || (!singleActionOnly && shotsSinceTriggerReset > 0) || firearm.fireMode == FirearmBase.FireModes.Safe) return;
            if (hammerAxis != null)
            {
                returnedTriggerSinceHammer = false;
                hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
                cocked = false;
                Util.PlayRandomAudioSource(hammerHitSounds);
            }
            else ApplyNextChamber();
            shotsSinceTriggerReset++;

            int ca = GetCurrentChamber();
            if (ca == -1) return;
            Cartridge loadedCartridge = loadedCartridges[ca];
            if (loadedCartridge == null || loadedCartridge.fired) return;

            foreach (RagdollHand hand in firearm.item.handlers)
            {
                if (hand.playerHand != null || hand.playerHand.controlHand != null) hand.playerHand.controlHand.HapticShort(50f);
            }
            if (loadedCartridge.additionalMuzzleFlash != null)
            {
                loadedCartridge.additionalMuzzleFlash.transform.position = firearm.actualHitscanMuzzle.position;
                loadedCartridge.additionalMuzzleFlash.transform.rotation = firearm.actualHitscanMuzzle.rotation;
                loadedCartridge.additionalMuzzleFlash.transform.SetParent(firearm.actualHitscanMuzzle);
                StartCoroutine(Explosives.Explosive.delayedDestroy(loadedCartridge.additionalMuzzleFlash.gameObject, loadedCartridge.additionalMuzzleFlash.main.duration));
            }
            firearm.PlayFireSound(loadedCartridge);
            if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
            {
                firearm.PlayMuzzleFlash(loadedCartridge);
            }
            FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, firearm.CalculateDamageMultiplier());
            if (!FirearmsSettings.infiniteAmmo)
            {
                loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle);
            }
            InvokeFireEvent();
        }

        private int GetCurrentChamber()
        {
            int car = -1;
            for (int i = loadedCartridges.Length - 1; i >= 0; i--)
            {
                if (Vector3.Distance(chamberPicker.position, chamberLocators[i].position) <= 0.01f) car = i;
            }
            return car;
        }

        private Vector3 GetNextTargetRotation()
        {
            float diff = chamberRotations[1] - chamberRotations[0];
            return new Vector3(0, 0, chamberRotations[currentChamber] + diff);
        }

        public void Lock(bool initial = false)
        {
            if (closed) return;
            ejectedSinceLastOpen = false;
            closed = true;
            float smallestDistance = 1000f;
            for (int i = 0; i < chamberRotations.Count; i++)
            {
                if (Vector3.Distance(chamberLocators[i].position, chamberPicker.position) < smallestDistance)
                {
                    smallestDistance = Vector3.Distance(chamberLocators[i].position, chamberPicker.position);
                    currentChamber = i;
                }
            }

            if (!initial)
            {
                foreach (AttachmentPoint p in foldAttachmentPoints)
                {
                    foreach (Handle h in p.GetComponentsInChildren<Handle>())
                    {
                        h.SetTouch(true);
                    }
                }
                foreach (Handle h in foregripHandles)
                {
                    h.SetTouch(true);
                }
            }

            state = BoltState.Locked;
            Util.PlayRandomAudioSource(lockSounds);
            InitializeFoldJoint(true);
            InitializeRotateJoint(true);
            onClose?.Invoke();
        }

        public Quaternion ToQuaternion(Vector3 vec)
        {
            return Quaternion.Euler(vec.x, vec.y, vec.z);
        }

        public void Unlock()
        {
            if (!closed) return;
            closed = false;
            state = BoltState.Moving;
            lastOpenTime = Time.time;
            Util.PlayRandomAudioSource(unlockSounds);

            foreach (Handle h in foregripHandles)
            {
                foreach (RagdollHand hand in h.handlers.ToArray())
                {
                    hand.UnGrab(false);
                }
                h.SetTouch(false);
            }

            foreach (AttachmentPoint p in foldAttachmentPoints)
            {
                foreach (Handle h in p.GetComponentsInChildren<Handle>())
                {
                    h.SetTouch(false);
                }
            }

            InitializeFoldJoint(false);
            InitializeRotateJoint(false);
            onOpen?.Invoke();
        }

        public void InitializeFoldJoint(bool closed)
        {
            if (closed)
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

            if (foldJoint == null)
            {
                foldJoint = firearm.item.gameObject.AddComponent<HingeJoint>();
                foldJoint.connectedBody = foldBody;
                foldJoint.massScale = 0.00001f;
                foldJoint.enableCollision = false;
            }
            foldJoint.autoConfigureConnectedAnchor = false;
            foldJoint.anchor = GrandparentLocalPosition(foldClosedPosition.transform, firearm.item.transform);
            foldJoint.connectedAnchor = Vector3.zero;
            foldJoint.axis = foldingAxis;
            foldJoint.useLimits = true;
            foldJoint.limits = closed ? new JointLimits() { min = 0f, max = 0f } : new JointLimits() { min = minFoldAngle, max = maxFoldAngle };
        }

        public void InitializeRotateJoint(bool closed)
        {
            if (closed)
            {
                rotateBody.transform.localPosition = rotateRoot.localPosition;
                rotateBody.transform.localEulerAngles = rotateRoot.localEulerAngles;

                rotateAxis.SetParent(rotateRoot);
                rotateAxis.localPosition = Vector3.zero;
                rotateAxis.localEulerAngles = new Vector3(0, 0, chamberRotations[currentChamber]);
            }
            else
            {
                rotateBody.transform.localPosition = rotateRoot.localPosition;
                rotateBody.transform.localEulerAngles = new Vector3(0, 0, chamberRotations[currentChamber]);

                rotateAxis.SetParent(rotateBody.transform);
                rotateAxis.localPosition = Vector3.zero;
                rotateAxis.localEulerAngles = Vector3.zero;
            }

            if (rotateJoint == null)
            {
                rotateJoint = foldBody.gameObject.AddComponent<HingeJoint>();
                rotateJoint.connectedBody = rotateBody;
                rotateJoint.massScale = 0.00001f;
            }
            rotateJoint.enableCollision = false;
            rotateJoint.autoConfigureConnectedAnchor = false;
            rotateJoint.anchor = GrandparentLocalPosition(rotateRoot.transform, foldBody.transform);
            rotateJoint.connectedAnchor = Vector3.zero;
            rotateJoint.axis = rotatingAxis;
            rotateJoint.useLimits = closed;
            rotateJoint.limits = new JointLimits() { min = 0f, max = 0f };
        }

        public override void TryRelease(bool forced = false)
        {
            if (state == BoltState.Locked) Unlock();
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

        public delegate void OnClose();
        public event OnClose onClose;

        public delegate void OnOpen();
        public event OnOpen onOpen;
    }
}
