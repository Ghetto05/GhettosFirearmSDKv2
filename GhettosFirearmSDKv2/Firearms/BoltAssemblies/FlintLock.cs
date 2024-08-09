using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class FlintLock : BoltBase
    {
        [Header("Firing")]
        public float fireDelay;
        public ParticleSystem panEffect;
        public PowderReceiver mainReceiver;
        public float baseRecoil = 20;
        
        [Header("Hammer")]
        public Transform hammer;
        public Transform hammerIdlePosition;
        public Transform hammerCockedPosition;
        private bool _hammerState;

        [Header("Pan")]
        public Transform pan;
        public Transform panOpenedPosition;
        public Transform panClosedPosition;
        public PowderReceiver panReceiver;
        private bool _panClosed;

        [Header("Round loading")]
        public string caliber;
        public Collider roundInsertCollider;
        public Transform roundMountPoint;
        public Cartridge loadedCartridge;
        private float lastRoundPosition;
        public Transform roundEjectDir;
        public Transform roundEjectPoint;
        public float roundEjectForce;

        [Header("Ram rod")]
        public Transform rodFrontEnd;
        public Transform rodRearEnd;
        public string ramRodItem;
        private Item currentRamRod;
        public Collider ramRodInsertCollider;
        private ConfigurableJoint joint;
        private bool rodAwayFromBreach;

        [Header("Ram rod store")]
        public Transform rodStoreFrontEnd;
        public Transform rodStoreRearEnd;
        private Item currentStoredRamRod;
        public Collider ramRodStoreInsertCollider;
        private ConfigurableJoint storeJoint;
        private bool rodAwayFromStoreEnd;

        [Header("Audio")]
        public AudioSource[] sizzleSound; 
        [Space]
        public AudioSource[] hammerCockSounds;
        public AudioSource[] hammerFireSounds;
        [Space]
        public AudioSource[] panOpenSounds;
        public AudioSource[] panCloseSounds;
        [Space]
        public AudioSource[] ramRodInsertSound;
        public AudioSource[] ramRodExtractSound;
        [Space]
        public AudioSource[] ramRodStoreInsertSound;
        public AudioSource[] ramRodStoreExtractSound;
        [Space]
        public AudioSource[] roundInsertSounds;

        private ProjectileData emptyFireData;
        private bool ramRodLocked;
        private bool ramRodStoreLocked;

        private SaveNodeValueInt panFillLevelSaveData;
        private SaveNodeValueInt barrelFillLevelSaveData;
        private SaveNodeValueBool rodStoreSaveData;
        private SaveNodeValueBool hammerStateSaveData;
        private SaveNodeValueBool panStateSaveData;

        private void Start()
        {
            GenerateFireData();
            OpenPan(true);
            Invoke(nameof(InvokedStart), Settings.invokeTime * 2);
        }

        private void GenerateFireData()
        {
            emptyFireData = gameObject.AddComponent<ProjectileData>();
            emptyFireData.recoil = 10;
            emptyFireData.forceDestabilize = false;
            emptyFireData.forceIncapitate = false;
            emptyFireData.isHitscan = true;
            emptyFireData.lethalHeadshot = false;
            emptyFireData.penetrationPower = ProjectileData.PenetrationLevels.None;
            emptyFireData.projectileCount = 30;
            emptyFireData.projectileRange = 1;
            emptyFireData.projectileSpread = 25;
            emptyFireData.damagePerProjectile = 0.3f;
            emptyFireData.hasBodyImpactEffect = false;
            emptyFireData.hasImpactEffect = false;
            emptyFireData.forcePerProjectile = 0f;
            emptyFireData.drawsImpactDecal = false;
        }

        private void InvokedStart()
        {
            firearm.OnCollisionEvent += FirearmOnOnCollisionEvent;
            firearm.OnCockActionEvent += FirearmOnOnCockActionEvent;
            firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;
            firearm.OnAltActionEvent += FirearmOnOnAltActionEvent;

            FirearmSaveData.AttachmentTreeNode node = FirearmSaveData.GetNode(firearm);
            panFillLevelSaveData = node.GetOrAddValue("FlintLock_PanPowderFillLevel", new SaveNodeValueInt());
            barrelFillLevelSaveData = node.GetOrAddValue("FlintLock_BarrelPowderFillLevel", new SaveNodeValueInt());
            SaveNodeValueBool newRodData = new SaveNodeValueBool();
            newRodData.value = true;
            rodStoreSaveData = node.GetOrAddValue("FlintLock_RodStored", newRodData);
            hammerStateSaveData = node.GetOrAddValue("FlintLock_HammerCockState", new SaveNodeValueBool());
            panStateSaveData = node.GetOrAddValue("FlintLock_PanOpenState", new SaveNodeValueBool());
            
            if (hammerStateSaveData.value)
                CockHammer(true);
            if (panStateSaveData.value)
                ClosePan(true);
            mainReceiver.currentAmount = barrelFillLevelSaveData.value;
            panReceiver.currentAmount = panFillLevelSaveData.value;
            mainReceiver.UpdatePositions();

            if (rodStoreSaveData.value && !string.IsNullOrWhiteSpace(ramRodItem))
            {
                Util.SpawnItem(ramRodItem, "Flint lock rod", rod =>
                {
                    InitializeRamRodJoint(rod, true);
                    currentStoredRamRod = rod;
                    ramRodStoreLocked = true;
                    storeJoint.anchor = GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform);
                    storeJoint.zMotion = ConfigurableJointMotion.Locked;
                    storeJoint.angularZMotion = ConfigurableJointMotion.Locked;
                }, rodStoreRearEnd.position, rodStoreRearEnd.rotation);
            }
            ChamberSaved();
        }

        private void FirearmOnOnCockActionEvent()
        {
            CockHammer();
        }

        private void FirearmOnOnAltActionEvent(bool longpress)
        {
            if (!longpress)
            {
                if (_panClosed)
                    OpenPan();
                else
                    ClosePan();
            }
        }

        private void FirearmOnOnTriggerChangeEvent(bool ispulled)
        {
            if (ispulled)
            {
                TryFire();
            }
        }

        private void FirearmOnOnCollisionEvent(Collision collision)
        {
            if (collision.rigidbody != null && collision.rigidbody.TryGetComponent(out Item hitItem))
            {
                if (currentRamRod == null && (currentStoredRamRod == null || hitItem != currentStoredRamRod) && hitItem.itemId.Equals(ramRodItem) && Util.CheckForCollisionWithThisCollider(collision, ramRodInsertCollider))
                {
                    InitializeRamRodJoint(hitItem);
                    currentRamRod = hitItem;
                    currentRamRod.DisallowDespawn = true;
                    rodAwayFromBreach = false;
                    Util.PlayRandomAudioSource(ramRodInsertSound);
                }
                if (currentStoredRamRod == null && (currentRamRod == null || hitItem != currentRamRod) && hitItem.itemId.Equals(ramRodItem) && Util.CheckForCollisionWithThisCollider(collision, ramRodStoreInsertCollider))
                {
                    InitializeRamRodJoint(hitItem, true);
                    currentStoredRamRod = hitItem;
                    currentStoredRamRod.DisallowDespawn = true;
                    rodAwayFromStoreEnd = false;
                    Util.PlayRandomAudioSource(ramRodStoreInsertSound);
                }
                else if (hitItem.TryGetComponent(out Cartridge c) && Util.CheckForCollisionWithThisCollider(collision, roundInsertCollider))
                {
                    nextLoadIsMuzzle = true;
                    LoadChamber(c);
                }
            }
        }

        [EasyButtons.Button]
        public override void TryFire()
        {
            if (!_hammerState)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            
            Util.PlayRandomAudioSource(hammerFireSounds);
            hammer.SetPositionAndRotation(hammerIdlePosition.position, hammerIdlePosition.rotation);
            _hammerState = false;
            hammerStateSaveData.value = false;

            if (!_panClosed)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
            
            OpenPan();

            if (!panReceiver.Sufficient())
            {
                InvokeFireLogicFinishedEvent();
                return;
            }

            if (!Settings.infiniteAmmo)
                panReceiver.currentAmount = 0;
            Util.PlayRandomAudioSource(sizzleSound);
            if (panEffect != null)
                panEffect.Play();
            
            Invoke(nameof(DelayedFire), fireDelay);

            base.TryFire();
        }

        public void DelayedFire()
        {
            if (!mainReceiver.Sufficient())
            {
                EjectRound();
                InvokeFireLogicFinishedEvent();
                return;
            }

            if (!Settings.infiniteAmmo)
                mainReceiver.currentAmount = 0;
            if (loadedCartridge != null)
            {
                if (Vector3.Distance(loadedCartridge.transform.position, rodRearEnd.position) < Settings.boltPointTreshold)
                {
                    firearm.PlayFireSound(loadedCartridge);
                    if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                        firearm.PlayMuzzleFlash(loadedCartridge);
                    FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hitPoints, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
                    FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
                    loadedCartridge.Fire(hitPoints, trajectories, firearm.actualHitscanMuzzle, hitCreatures, !HeldByAI() && !Settings.infiniteAmmo);
                }
            }
            else
            {
                firearm.PlayFireSound(null);
                FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, emptyFireData, out List<Vector3> hitPoints, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
                FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, baseRecoil, 1, firearm.recoilModifier, firearm.recoilModifiers);
                firearm.PlayMuzzleFlash(null);
            }
            IncrementBreachSmokeTime();

            if (currentRamRod != null)
            { 
                InitializeRamRodJoint(null);
                Util.DisableCollision(currentRamRod, false);
                currentRamRod.DisallowDespawn = false;
                currentRamRod = null;
                Util.PlayRandomAudioSource(ramRodExtractSound);
            }
            EjectRound();
        }

        [EasyButtons.Button]
        public void CockHammer(bool forced = false)
        {
            if (_hammerState)
                return;
            if (!forced)
                Util.PlayRandomAudioSource(hammerCockSounds);
            hammer.SetPositionAndRotation(hammerCockedPosition.position, hammerCockedPosition.rotation);
            _hammerState = true;
            hammerStateSaveData.value = true;
        }

        [EasyButtons.Button]
        public void OpenPan(bool forced = false)
        {
            if (!_panClosed && !forced)
                return;
            if (!forced)
                Util.PlayRandomAudioSource(panOpenSounds);
            pan.SetPositionAndRotation(panOpenedPosition.position, panOpenedPosition.rotation);
            _panClosed = false;
            if (!forced)
                panStateSaveData.value = false;
        }

        [EasyButtons.Button]
        public void ClosePan(bool forced = false)
        {
            if (_panClosed || !_hammerState)
                return;
            if (!forced)
                Util.PlayRandomAudioSource(panCloseSounds);
            pan.SetPositionAndRotation(panClosedPosition.position, panClosedPosition.rotation);
            _panClosed = true;
            panStateSaveData.value = true;
        }

        private void FixedUpdate()
        {
            panReceiver.blocked = _panClosed || !_hammerState;
            mainReceiver.blocked = loadedCartridge != null || currentRamRod != null;

            if (barrelFillLevelSaveData != null)
                barrelFillLevelSaveData.value = mainReceiver.currentAmount;
            if (panFillLevelSaveData != null)
                panFillLevelSaveData.value = panReceiver.currentAmount;

            if (currentRamRod != null && loadedCartridge != null)
            {
                float currentPos = Vector3.Distance(rodFrontEnd.position, currentRamRod.transform.position);
                float targetPos = Vector3.Distance(rodFrontEnd.position, rodRearEnd.position);
                float posTime = currentPos / targetPos;
                if (posTime > lastRoundPosition)
                    lastRoundPosition = posTime;
                loadedCartridge.transform.position = Vector3.LerpUnclamped(rodFrontEnd.position, rodRearEnd.position, lastRoundPosition);
            }

            #region Ram rod movement
            
            if (currentRamRod != null && !rodAwayFromBreach &&
                Vector3.Distance(currentRamRod.transform.position, rodRearEnd.position) < Settings.boltPointTreshold)
                rodAwayFromBreach = true;

            if (currentRamRod != null && rodAwayFromBreach &&
                Vector3.Distance(currentRamRod.transform.position, rodFrontEnd.position) < Settings.boltPointTreshold)
            {
                InitializeRamRodJoint(null);
                Util.DisableCollision(currentRamRod, false);
                currentRamRod.DisallowDespawn = false;
                currentRamRod = null;
                Util.PlayRandomAudioSource(ramRodExtractSound);
            }

            if (currentRamRod != null && currentRamRod.handlers.Count == 0 && !ramRodLocked)
            {
                ramRodLocked = true;
                joint.anchor = new Vector3(GrandparentLocalPosition(rodRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).y, GrandparentLocalPosition(currentRamRod.transform, firearm.item.transform).z);
                joint.zMotion = ConfigurableJointMotion.Locked;
            }
            else if (currentRamRod != null && currentRamRod.handlers.Count > 0 && ramRodLocked)
            {
                ramRodLocked = false;
                joint.anchor = new Vector3(GrandparentLocalPosition(rodRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).y, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).z + ((rodFrontEnd.localPosition.z - rodRearEnd.localPosition.z) / 2));
                joint.zMotion = ConfigurableJointMotion.Limited;
            }

            #endregion

            #region Ram rod store movement
            
            if (currentStoredRamRod != null && !rodAwayFromStoreEnd &&
                Vector3.Distance(currentStoredRamRod.transform.position, rodStoreRearEnd.position) < Settings.boltPointTreshold)
                rodAwayFromStoreEnd = true;

            if (currentStoredRamRod != null && rodAwayFromStoreEnd &&
                Vector3.Distance(currentStoredRamRod.transform.position, rodStoreFrontEnd.position) < Settings.boltPointTreshold)
            {
                InitializeRamRodJoint(null, true);
                Util.DisableCollision(currentStoredRamRod, false);
                currentStoredRamRod.DisallowDespawn = false;
                currentStoredRamRod = null;
                Util.PlayRandomAudioSource(ramRodStoreExtractSound);
            }

            if (currentStoredRamRod != null && currentStoredRamRod.handlers.Count == 0 && !ramRodStoreLocked)
            {
                ramRodStoreLocked = true;
                storeJoint.anchor = new Vector3(GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).y, GrandparentLocalPosition(currentStoredRamRod.transform, firearm.item.transform).z);
                storeJoint.zMotion = ConfigurableJointMotion.Locked;
                storeJoint.angularZMotion = ConfigurableJointMotion.Locked;
            }
            else if (currentStoredRamRod != null && currentStoredRamRod.handlers.Count > 0 && ramRodStoreLocked)
            {
                ramRodStoreLocked = false;
                storeJoint.anchor = new Vector3(GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).y, GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).z + ((rodStoreFrontEnd.localPosition.z - rodStoreRearEnd.localPosition.z) / 2));
                storeJoint.zMotion = ConfigurableJointMotion.Limited;
                storeJoint.angularZMotion = ConfigurableJointMotion.Free;
            }

            #endregion
        }

        private void Update()
        {
            BaseUpdate();
        }

        private void InitializeRamRodJoint(Item item, bool store = false)
        {
            ConfigurableJoint j = store ? storeJoint : joint;
            Transform frontEnd = store ? rodStoreFrontEnd : rodFrontEnd;
            Transform rearEnd = store ? rodStoreRearEnd : rodRearEnd;
            
            if (j != null)
                Destroy(j);
            if (item == null)
            {
                if (store)
                    rodStoreSaveData.value = false;
                return;
            }

            if (store)
                rodStoreSaveData.value = true;
            RagdollHand[] oldHandlers = item.handlers.ToArray();
            foreach (Handle handle in item.handles)
            {
                handle.Release();
            }
            j = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
            j.massScale = 0.00001f;
            j.linearLimit = new SoftJointLimit
            {
                limit = Vector3.Distance(frontEnd.position, rearEnd.position) / 2
            };
            j.autoConfigureConnectedAnchor = false;
            j.connectedAnchor = Vector3.zero;
            j.anchor = new Vector3(GrandparentLocalPosition(rearEnd, firearm.item.transform).x, GrandparentLocalPosition(rearEnd, firearm.item.transform).y, GrandparentLocalPosition(rearEnd, firearm.item.transform).z + ((frontEnd.localPosition.z - rearEnd.localPosition.z) / 2));
            j.xMotion = ConfigurableJointMotion.Locked;
            j.yMotion = ConfigurableJointMotion.Locked;
            j.zMotion = ConfigurableJointMotion.Limited;
            j.angularXMotion = ConfigurableJointMotion.Locked;
            j.angularYMotion = ConfigurableJointMotion.Locked;
            if (!store)
                j.angularZMotion = ConfigurableJointMotion.Free;
            else
                j.angularZMotion = ConfigurableJointMotion.Locked;
            item.transform.position = frontEnd.position;
            item.transform.eulerAngles = new Vector3(frontEnd.eulerAngles.x, frontEnd.eulerAngles.y, item.transform.localEulerAngles.z);
            j.connectedBody = item.physicBody.rigidBody;
            foreach (RagdollHand handler in oldHandlers)
            {
                handler.Grab(item.GetMainHandle(handler.side));
            }
            Util.DisableCollision(item, true);

            if (store)
                storeJoint = j;
            else
                joint = j;
        }

        public override Cartridge GetChamber()
        {
            return loadedCartridge;
        }

        private void SetPositionToPowder()
        {
            if (loadedCartridge != null)
                loadedCartridge.transform.position = Vector3.LerpUnclamped(rodFrontEnd.position, rodRearEnd.position, (float)mainReceiver.currentAmount / (float)mainReceiver.grainCapacity);
        }

        private bool nextLoadIsMuzzle;
        public override bool LoadChamber(Cartridge c, bool forced = false)
        {
            if (loadedCartridge == null && (Util.AllowLoadCartridge(c, caliber) || forced))
            {
                if (!forced)
                    Util.PlayRandomAudioSource(roundInsertSounds);
                lastRoundPosition = 0f;
                loadedCartridge = c;
                c.item.DisallowDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                c.item.physicBody.isKinematic = true;
                c.transform.parent = rodFrontEnd;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                SaveChamber(c.item.itemId);
                Invoke(nameof(Rechamber), 1f);
                if (!nextLoadIsMuzzle)
                { 
                    Invoke(nameof(SetPositionToPowder), 1.2f);
                    nextLoadIsMuzzle = false;
                }

                return true;
            }
            return false;
        }

        private void Rechamber()
        {
            if (loadedCartridge != null)
            {
                loadedCartridge.transform.parent = rodFrontEnd;
                loadedCartridge.transform.localPosition = Vector3.zero;
                loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
            }
        }

        public override void EjectRound()
        {
            if (loadedCartridge == null)
                return;
            SaveChamber("");
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
            Rigidbody rb = c.item.physicBody.rigidBody;
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
    }
}
