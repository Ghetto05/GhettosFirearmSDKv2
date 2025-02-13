using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2
{
    public class Firearm : FirearmBase, IAttachmentManager
    {
        public Item Item => item ?? GetComponent<Item>();
        public Transform Transform => transform;

        public List<AttachmentPoint> AttachmentPoints
        {
            get => attachmentPoints;
            set => attachmentPoints = value;
        }

        public List<Attachment> CurrentAttachments { get; set; }

        public FirearmSaveData SaveData { get; set; }

        public List<AttachmentPoint> attachmentPoints;
        public List<Handle> preSnapActiveHandles;

        private ItemMetaData _metaData;

        public ItemMetaData MetaData
        {
            get
            {
                return _metaData ??= (ItemMetaData)item.data.modules.FirstOrDefault(x => x.GetType() == typeof(ItemMetaData));
            }
        }

        private CustomReference _preferredForegripReference;

        private CustomReference PreferredForegripReference
        {
            get
            {
                if (_preferredForegripReference != null)
                    return _preferredForegripReference;
                
                _preferredForegripReference = new CustomReference()
                {
                    name = "FirearmSecondaryHandle", 
                    transform = GetPreferredForegrip()
                };
                item.customReferences.Add(_preferredForegripReference);
                
                return _preferredForegripReference;
            }
        }

        private static readonly Vector3 StandardAimPointOffset = new(0.2f, -0.3f, 0.4f);

        private static readonly Vector3 TwoHandedAimPointOffset = new(0.1f, -0.3f, 0.15f);

        public override List<Handle> AllTriggerHandles()
        {
            var hs = new List<Handle>();
            hs.AddRange(additionalTriggerHandles);
            if (disableMainFireHandle || !item || !item.mainHandleLeft)
                return hs;

            hs.Add(item.mainHandleLeft);
            return hs;
        }

        public override float CalculateDamageMultiplier()
        {
            return CurrentAttachments.Where(a => a.multiplyDamage).Aggregate(1f, (current, a) => current * a.damageMultiplier);
        }

        public override void Start()
        {
            if (GameModeManager.instance?.currentGameMode?.name.Equals("CrystalHunt") == true)
            {
                item.Despawn();
                return;
            }

            base.Start();

            if (!item)
                item = GetComponent<Item>();
            Invoke(nameof(InvokedStart), Settings.invokeTime);

            var aiModule = new ItemModuleAI
                           {
                               primaryClass = ItemModuleAI.WeaponClass.Firearm,
                               secondaryClass = ItemModuleAI.WeaponClass.Melee,
                               weaponHandling = ItemModuleAI.WeaponHandling.TwoHanded,
                               secondaryHandling = ItemModuleAI.WeaponHandling.TwoHanded,
                               weaponAttackTypes = ItemModuleAI.AttackTypeFlags.None,
                               ignoredByDefense = false,
                               alwaysPrimary = false,
                               defaultStanceInfo = new ItemModuleAI.StanceInfo
                                                   {
                                                       offhand = ItemModuleAI.StanceInfo.Offhand.SameItem,
                                                       stanceDataID = "HumanMeleeShieldStance",
                                                       grabAIHandleRadius = 0
                                                   },
                               rangedWeaponData = new ItemModuleAI.RangedWeaponData
                                                  {
                                                      spread = Vector2.zero,
                                                      ammoType = defaultAmmoItem,
                                                      projectileSpeed = Mathf.Infinity,
                                                      accountForGravity = false,
                                                      tooCloseDistance = PreferredEngagementDistance(),
                                                      weaponAimAngleOffset = Vector3.zero,
                                                      weaponHoldAngleOffset = IsTwoHanded ? new Vector3(0, -45, 0) : Vector3.zero,
                                                      weaponHoldPositionOffset = IsTwoHanded ? TwoHandedAimPointOffset : StandardAimPointOffset,
                                                      customRangedAttackAnimationData = null
                                                  },
                               armResistanceMultiplier = 3f,
                               allowDynamicHeight = false,
                               defenseHasPriority = false
                           };
            item.data.modules.Add(aiModule);
            item.data.moduleAI = aiModule;
        }

        public override void InvokedStart()
        {
            all.Add(this);
            if (!disableMainFireHandle) mainFireHandle = item.mainHandleLeft;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnSnapEvent += Item_OnSnapEvent2;
            item.OnUnSnapEvent += Item_OnUnSnapEvent2;
            item.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;
            item.mainCollisionHandler.OnCollisionStartEvent += InvokeCollisionTR;
            item.OnDespawnEvent += OnDespawn;
            CurrentAttachments = [];
            fireEvent.AddListener(AIFire);

            SharedAttachmentManagerFunctions.LoadAndApplyData(this);
            
            CalculateMuzzle();

            #region handle type validation

            if (Settings.debugMode)
            {
                foreach (var h in gameObject.GetComponentsInChildren<Handle>())
                {
                    if (h.GetType() != typeof(GhettoHandle)) Debug.LogWarning("Handle " + h.gameObject.name + " on firearm " + gameObject.name + " is not of type GhettoHandle!");
                }
            }

            #endregion handle type validation
            
            Invoke(nameof(DelayedLoad), 2.3f);

            base.InvokedStart();
        }

        private void OnDespawn(EventTime eventTime)
        {
            if (eventTime != EventTime.OnStart)
                return;
            item.OnHeldActionEvent -= Item_OnHeldActionEvent;
            item.OnSnapEvent -= Item_OnSnapEvent;
            item.OnUnSnapEvent -= Item_OnUnSnapEvent;
            item.OnSnapEvent -= Item_OnSnapEvent2;
            item.OnUnSnapEvent -= Item_OnUnSnapEvent2;
            item.OnDespawnEvent -= OnDespawn;
            item.lightVolumeReceiver.onVolumeChangeEvent -= UpdateAllLightVolumeReceivers;
            item.mainCollisionHandler.OnCollisionStartEvent -= InvokeCollisionTR;
            all.Remove(this);
        }

        public override void Update()
        {
            base.Update();
            RefreshRecoilModifiers();
            
            item.data.moduleAI.primaryClass = (HeldByAI() || item.holder) ? ItemModuleAI.WeaponClass.Firearm : ItemModuleAI.WeaponClass.Melee;
            item.data.moduleAI.weaponHandling = IsTwoHanded && (HeldByAI() || item.holder) ? ItemModuleAI.WeaponHandling.TwoHanded : ItemModuleAI.WeaponHandling.OneHanded;
            item.data.moduleAI.secondaryHandling = IsTwoHanded && (HeldByAI() || item.holder) ? ItemModuleAI.WeaponHandling.TwoHanded : ItemModuleAI.WeaponHandling.OneHanded;
            item.data.moduleAI.rangedWeaponData.weaponHoldAngleOffset = IsTwoHanded ? new Vector3(0, -45, 0) : Vector2.zero;
            if (IsTwoHanded)
            {
                PreferredForegripReference.transform = GetPreferredForegrip();
            }

            var data = bolt?.GetChamber()?.data;
            if (data)
            {
                item.data.moduleAI.rangedWeaponData.projectileSpeed = data.isHitscan ? Mathf.Infinity : data.muzzleVelocity;
                item.data.moduleAI.rangedWeaponData.accountForGravity = !data.isHitscan;
            }
        }

        public override void OnCollisionEnter(Collision collision)
        {
            OnCollision?.Invoke(collision);
            base.OnCollisionEnter(collision);
        }

        public void Item_OnUnSnapEvent2(Holder holder)
        {
            foreach (var han in preSnapActiveHandles.Where(han => han && han.touchCollider))
            {
                han.SetTouch(true);
            }
        }

        public void Item_OnSnapEvent2(Holder holder)
        {
            preSnapActiveHandles = [];
            foreach (var han in item.handles.Where(han => han && han.enabled && han.touchCollider.enabled && !(han.data.id.Equals("ObjectHandleHeavy") || han.data.id.Equals("ObjectHandleHeavyPistol"))))
            {
                preSnapActiveHandles.Add(han);
                han.SetTouch(false);
            }
        }

        public void UpdateAttachments()
        {
            SharedAttachmentManagerFunctions.UpdateAttachments(this);
            CalculateMuzzle();
        }

        public void DelayedLoad()
        {
            if (!item.holder)
                return;
            var h = item.holder;
            if (h.GetComponentInParent<AmmunitionPouch>() is { } pouch)
                pouch.nextSnapFromFirearmLoad = true;
            item.holder.UnSnap(item, true);
            h.Snap(item, true);
        }

        public AttachmentPoint GetSlotFromId(string id)
        {
            return SharedAttachmentManagerFunctions.GetSlotFromId(this, id);
        }

        public override void PlayMuzzleFlash(Cartridge cartridge)
        {
            var overridden = false;
            foreach (var at in CurrentAttachments)
            {
                if (at.overridesMuzzleFlash && !at.attachmentPoint.dummyMuzzleSlot)
                    overridden = true;
                if (!at.overridesMuzzleFlash || at.attachmentPoint.dummyMuzzleSlot ||
                    !NoMuzzleFlashOverridingAttachmentChildren(at) || !at.newFlash)
                    continue;
                at.newFlash.Play();
                StartCoroutine(PlayMuzzleFlashLight(cartridge));
            }

            //default
            if (overridden || defaultMuzzleFlash is not { } mf)
                return;
            mf.Play();
            StartCoroutine(PlayMuzzleFlashLight(cartridge));
        }

        public override bool IsSuppressed()
        {
            return integrallySuppressed || CurrentAttachments.Any(at => at.isSuppressing && !at.attachmentPoint.dummyMuzzleSlot && at.gameObject.activeInHierarchy);
        }

        public override void CalculateMuzzle()
        {
            if (!hitscanMuzzle)
                return;
            actualHitscanMuzzle = CurrentAttachments.Where(at => at.minimumMuzzlePosition && !at.attachmentPoint.dummyMuzzleSlot).OrderByDescending(at => Vector3.Distance(transform.position, at.minimumMuzzlePosition.position)).FirstOrDefault()?.minimumMuzzlePosition;
            if (!actualHitscanMuzzle)
                actualHitscanMuzzle = hitscanMuzzle;
            base.CalculateMuzzle();
        }

        public void AIFire()
        {
            if (fireMode == FireModes.Safe && GetComponentsInChildren<FiremodeSelector>().FirstOrDefault(x => x.firearm == this) is { } fs)
                fs.CycleFiremode();
            StartCoroutine(AIFireCoroutine());
        }

        private IEnumerator AIFireCoroutine()
        {
            Item_OnHeldActionEvent(item.mainHandler, item.GetMainHandle(item.mainHandler.side), Interactable.Action.UseStart);
            if (fireMode == FireModes.Semi)
                yield return new WaitForSeconds(0.2f);
            if (fireMode == FireModes.Burst)
                yield return new WaitForSeconds(0.4f);
            if (fireMode == FireModes.Auto)
                yield return new WaitForSeconds(Random.Range(0.2f, 1.3f));
            Item_OnHeldActionEvent(item.mainHandler, item.GetMainHandle(item.mainHandler.side), Interactable.Action.UseStop);
        }

        private void UpdateAllLightVolumeReceivers(LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
        {
            SharedAttachmentManagerFunctions.UpdateAllLightVolumeReceivers(this, currentLightProbeVolume, lightProbeVolumes);
        }

        public override bool HeldByAI()
        {
            return !(item?.handlers?.FirstOrDefault()?.creature.isPlayer ?? true);
        }

        private Handle GetPreferredForegrip()
        {
            return item.handles
                       .Where(x => x.GetType() == typeof(GhettoHandle))
                       .Cast<GhettoHandle>()
                       .Where(x => x.type != GhettoHandle.HandleType.Bolt && x.type != GhettoHandle.HandleType.MainGrip && x.aiPriority != GhettoHandle.HandlePriority.NoAI)
                       .OrderBy(x => x.aiPriority)
                       .FirstOrDefault();
        }

        private bool IsTwoHanded => GetPreferredForegrip();

        private float PreferredEngagementDistance()
        {
            return 8f;
        }

        public override bool CanFire
        {
            get
            {
                var attachmentPointsAllow = !GetComponentsInChildren<AttachmentPoint>().Any(x => x.requiredToFire && !x.currentAttachments.Any());
                return base.CanFire && attachmentPointsAllow;
            }
        }

        public void InvokeAttachmentAdded(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            OnAttachmentAdded?.Invoke(attachment, attachmentPoint);
        }

        public void InvokeAttachmentRemoved(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            OnAttachmentRemoved?.Invoke(attachment, attachmentPoint);
        }

        public event IAttachmentManager.Collision OnCollision;
        public event IAttachmentManager.AttachmentAdded OnAttachmentAdded;
        public event IAttachmentManager.AttachmentRemoved OnAttachmentRemoved;
    }
}