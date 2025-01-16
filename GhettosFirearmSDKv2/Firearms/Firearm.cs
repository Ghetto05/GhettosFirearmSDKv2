using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2
{
    public class Firearm : FirearmBase
    {
        public List<AttachmentPoint> attachmentPoints;
        public List<Attachment> allAttachments;
        public Texture icon;
        public List<Handle> preSnapActiveHandles;
        public FirearmSaveData SaveData;

        private ItemMetaData _metaData;

        public ItemMetaData MetaData
        {
            get
            {
                if (_metaData == null)
                {
                    _metaData = (ItemMetaData)item.data.modules.FirstOrDefault(x => x.GetType() == typeof(ItemMetaData));
                }
                return _metaData;
            }
        }

        private CustomReference _preferredForegripReference;

        public CustomReference PreferredForegripReference
        {
            get
            {
                if (_preferredForegripReference == null)
                {
                    _preferredForegripReference = new CustomReference()
                                                  {
                                                      name = "FirearmSecondaryHandle", 
                                                      transform = GetPreferredForegrip()
                                                  };
                    item.customReferences.Add(_preferredForegripReference);
                }
                return _preferredForegripReference;
            }
        }

        private static readonly Vector3 StandardAimPointOffset = new(0.2f, -0.3f, 0.4f);

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
            var multiply = 1f;
            foreach (var a in allAttachments)
            {
                if (a.multiplyDamage)
                {
                    multiply *= a.damageMultiplier;
                }
            }
            return multiply;
        }

        public override void Start()
        {
            if (GameModeManager.instance?.currentGameMode?.name.Equals("CrystalHunt") == true)
            {
                item.Despawn();
                return;
            }

            base.Start();
            if (attachmentPoints.Count == 0 || attachmentPoints.Any(a => !a))
            {
                attachmentPoints = GetComponentsInChildren<AttachmentPoint>().ToList();
            }

            if (!item)
                item = GetComponent<Item>();
            item.OnDespawnEvent += Item_OnDespawnEvent;
            Invoke(nameof(InvokedStart), Settings.invokeTime);
            all.Add(this);

            var aiModule = new ItemModuleAI
                           {
                               primaryClass = ItemModuleAI.WeaponClass.Melee,
                               secondaryClass = ItemModuleAI.WeaponClass.Melee,
                               weaponHandling = ItemModuleAI.WeaponHandling.OneHanded,
                               secondaryHandling = ItemModuleAI.WeaponHandling.OneHanded,
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
                                                      weaponHoldAngleOffset = Vector3.zero,
                                                      weaponHoldPositionOffset = StandardAimPointOffset,
                                                      customRangedAttackAnimationData = null
                                                  },
                               armResistanceMultiplier = 3f,
                               allowDynamicHeight = false,
                               defenseHasPriority = false
                           };
            item.data.modules.Add(aiModule);
            item.data.moduleAI = aiModule;
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            all.Remove(this);
        }

        public override void InvokedStart()
        {
            if (!disableMainFireHandle) mainFireHandle = item.mainHandleLeft;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnSnapEvent += Item_OnSnapEvent2;
            item.OnUnSnapEvent += Item_OnUnSnapEvent2;
            item.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;
            item.mainCollisionHandler.OnCollisionStartEvent += InvokeCollisionTR;
            allAttachments = new List<Attachment>();
            fireEvent.AddListener(AIFire);
            foreach (var ap in attachmentPoints)
            {
                ap.parentFirearm = this;
            }

            if (!item.TryGetCustomData(out SaveData))
            {
                SaveData = new FirearmSaveData();
                SaveData.FirearmNode = new FirearmSaveData.AttachmentTreeNode();
                item.AddCustomData(SaveData);
            }

            SaveData.ApplyToFirearm(this);
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

        public override void Update()
        {
            base.Update();
            RefreshRecoilModifiers();
            
            item.data.moduleAI.primaryClass = HeldByAI() ? ItemModuleAI.WeaponClass.Firearm : ItemModuleAI.WeaponClass.Melee;
            item.data.moduleAI.weaponHandling = IsTwoHanded && HeldByAI() ? ItemModuleAI.WeaponHandling.TwoHanded : ItemModuleAI.WeaponHandling.OneHanded;
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

        public void Item_OnUnSnapEvent2(Holder holder)
        {
            foreach (var han in preSnapActiveHandles)
            {
                if (han && han.touchCollider) han.SetTouch(true);
            }
        }

        public void Item_OnSnapEvent2(Holder holder)
        {
            preSnapActiveHandles = new List<Handle>();
            foreach (var han in item.handles)
            {
                if (han && han.enabled && han.touchCollider.enabled && !(han.data.id.Equals("ObjectHandleHeavy") || han.data.id.Equals("ObjectHandleHeavyPistol")))
                {
                    preSnapActiveHandles.Add(han);
                    han.SetTouch(false);
                }
            }
        }

        public void UpdateAttachments(bool initialSetup = false)
        {
            allAttachments = new List<Attachment>();
            AddAttachments(attachmentPoints);
            CalculateMuzzle();
        }

        public void AddAttachments(List<AttachmentPoint> points)
        {
            foreach (var point in points.Where(x => x))
            {
                if (point.currentAttachments.Any())
                {
                    allAttachments.AddRange(point.currentAttachments);
                    AddAttachments(point.currentAttachments.SelectMany(x => x.attachmentPoints).ToList());
                }
            }
        }

        public void DelayedLoad()
        {
            if (item.holder != null)
            {
                var h = item.holder;
                item.holder.UnSnap(item, true);
                h.Snap(item, true);
            }
        }

        public AttachmentPoint GetSlotFromId(string id)
        {
            foreach (var point in attachmentPoints)
            {
                if (point.id.Equals(id)) return point;
            }
            return null;
        }

        public override void PlayMuzzleFlash(Cartridge cartridge)
        {
            var overridden = false;
            foreach (var at in allAttachments)
            {
                if (at.overridesMuzzleFlash && !at.attachmentPoint.dummyMuzzleSlot)
                    overridden = true;
                if (at.overridesMuzzleFlash && !at.attachmentPoint.dummyMuzzleSlot && NoMuzzleFlashOverridingAttachmentChildren(at))
                {
                    if (at.newFlash != null)
                    {
                        at.newFlash.Play();
                        StartCoroutine(PlayMuzzleFlashLight(cartridge));
                    }
                }
            }

            //default
            if (!overridden && defaultMuzzleFlash is { } mf)
            {
                mf.Play();
                StartCoroutine(PlayMuzzleFlashLight(cartridge));
            }
        }

        public override bool IsSuppressed()
        {
            return integrallySuppressed || allAttachments.Any(at => at.isSuppressing && !at.attachmentPoint.dummyMuzzleSlot && at.gameObject.activeInHierarchy);
        }

        public override void CalculateMuzzle()
        {
            if (!hitscanMuzzle)
                return;
            actualHitscanMuzzle = allAttachments.Where(at => at.minimumMuzzlePosition && !at.attachmentPoint.dummyMuzzleSlot).OrderByDescending(at => Vector3.Distance(transform.position, at.minimumMuzzlePosition.position)).FirstOrDefault()?.minimumMuzzlePosition;
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
            foreach (var lvr in GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != item.lightVolumeReceiver))
            {
                Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
            }
        }

        public override bool HeldByAI()
        {
            return !(item?.handlers?.FirstOrDefault()?.creature.isPlayer ?? true);
        }

        public Handle GetPreferredForegrip()
        {
            return item.handles
                       .Where(x => x.GetType() == typeof(GhettoHandle))
                       .Cast<GhettoHandle>()
                       .Where(x => x.type != GhettoHandle.HandleType.Bolt && x.type != GhettoHandle.HandleType.MainGrip && x.aiPriority != GhettoHandle.HandlePriority.NoAI)
                       .OrderBy(x => x.aiPriority)
                       .FirstOrDefault();
        }

        private bool IsTwoHanded
        {
            get
            {
                return GetPreferredForegrip();
            }
        }

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
    }
}