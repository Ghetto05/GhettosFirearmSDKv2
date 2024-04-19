using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class Firearm : FirearmBase
    {
        public List<AttachmentPoint> attachmentPoints;
        public List<Attachment> allAttachments;
        public Texture icon;
        public List<Handle> preSnapActiveHandles;
        public FirearmSaveData saveData;

        public override List<Handle> AllTriggerHandles()
        {
            List<Handle> hs = new List<Handle>();
            hs.AddRange(additionalTriggerHandles);
            if (disableMainFireHandle) return hs;
            hs.Add(item.mainHandleLeft);
            return hs;
        }

        public override float CalculateDamageMultiplier()
        {
            float multiply = 1f;
            foreach (Attachment a in allAttachments)
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
            base.Start();
            if (attachmentPoints.Count == 0 || attachmentPoints.Any(a => a == null))
            {
                attachmentPoints = GetComponentsInChildren<AttachmentPoint>().ToList();
            }

            if (item == null) item = GetComponent<Item>();
            item.OnDespawnEvent += Item_OnDespawnEvent;
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
            all.Add(this);
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            all.Remove(this);
        }

        public void InvokedStart()
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
            OnAIFire = AIFire;
            foreach (AttachmentPoint ap in attachmentPoints)
            {
                ap.parentFirearm = this;
            }
            StartCoroutine(DelayedLoad());
            
            if (!item.TryGetCustomData(out saveData))
            {
                saveData = new FirearmSaveData();
                saveData.firearmNode = new FirearmSaveData.AttachmentTreeNode();
                item.AddCustomData(saveData);
            }

            if (saveData.firearmNode.TryGetValue("Ammo item", out SaveNodeValueString value))
            {
                defaultAmmoItem = value.value;
            }

            saveData.ApplyToFirearm(this);
            CalculateMuzzle();

            #region load icon
            Addressables.LoadAssetAsync<Texture>(item.data.iconAddress).Completed += (handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    icon = handle.Result;
                }
                else
                {
                    Debug.LogWarning(("Unable to load icon texture from location " + item.data.iconAddress));
                    Addressables.Release(handle);
                }
            });
            #endregion load icon
            #region handle type validation
            if (FirearmsSettings.debugMode)
            {
                foreach (Handle h in gameObject.GetComponentsInChildren<Handle>())
                {
                    if (h.GetType() != typeof(GhettoHandle)) Debug.LogWarning("Handle " + h.gameObject.name + " on firearm " + gameObject.name + " is not of type GhettoHandle!");
                }
            }
            #endregion handle type validation
        }

        public override void Update()
        {
            base.Update();
            RefreshRecoilModifiers();
        }

        public void Item_OnUnSnapEvent2(Holder holder)
        {
            foreach (Handle han in preSnapActiveHandles)
            {
                if (han != null && han.touchCollider != null) han.SetTouch(true);
            }
        }

        public void Item_OnSnapEvent2(Holder holder)
        {
            preSnapActiveHandles = new List<Handle>();
            foreach (Handle han in item.handles)
            {
                if (han != null && han.enabled && han.touchCollider.enabled && !(han.data.id.Equals("ObjectHandleHeavy") || han.data.id.Equals("ObjectHandleHeavyPistol")))
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
            foreach (AttachmentPoint point in points)
            {
                if (point.currentAttachment != null)
                {
                    allAttachments.Add(point.currentAttachment);
                    AddAttachments(point.currentAttachment.attachmentPoints);
                }
            }
        }

        public IEnumerator DelayedLoad()
        {
            yield return new WaitForSeconds(2.3f);
            if (item.holder != null)
            {
                Holder h = item.holder;
                item.holder.UnSnap(item, true, false);
                h.Snap(item, true);
            }
        }

        public AttachmentPoint GetSlotFromId(string id)
        {
            foreach (AttachmentPoint point in attachmentPoints)
            {
                if (point.id.Equals(id)) return point;
            }
            return null;
        }

        public override void PlayMuzzleFlash(Cartridge cartridge)
        {
            bool overridden = false;
            foreach (Attachment at in allAttachments)
            {
                if (at.overridesMuzzleFlash)
                    overridden = true;
                if (at.overridesMuzzleFlash && NoMuzzleFlashOverridingAttachmentChildren(at))
                {
                    if (at.newFlash != null)
                    {
                        at.newFlash.Play();
                        StartCoroutine(PlayMuzzleFlashLight(cartridge));
                    }
                }
            }

            //default
            if (!overridden && defaultMuzzleFlash is ParticleSystem mf)
            {
                mf.Play();
                StartCoroutine(PlayMuzzleFlashLight(cartridge));
            }
        }

        public override bool IsSuppressed()
        {
            return integrallySuppressed || allAttachments.Any(at => at.isSuppressing && at.gameObject.activeInHierarchy);
        }
        
        public override void CalculateMuzzle()
        {
            if (hitscanMuzzle == null)
                return;
            actualHitscanMuzzle = allAttachments.Where(at => at.minimumMuzzlePosition != null).OrderByDescending(at => Vector3.Distance(hitscanMuzzle.position, at.minimumMuzzlePosition.position)).FirstOrDefault()?.minimumMuzzlePosition;
            if (actualHitscanMuzzle == null)
                actualHitscanMuzzle = hitscanMuzzle;
            base.CalculateMuzzle();
        }

        public bool AIFire(AIFireable fireAble, RagdollHand hand, bool finished)
        {
            if (fireMode == FireModes.Safe && GetComponentInChildren<FiremodeSelector>() is FiremodeSelector fs)
                fs.CycleFiremode();
            StartCoroutine(AIFireCoroutine(hand));
            return true;
        }

        private IEnumerator AIFireCoroutine(RagdollHand hand)
        {
            Item_OnHeldActionEvent(hand, item.GetMainHandle(hand.side), Interactable.Action.UseStart);
            if (fireMode == FireModes.Semi) yield return new WaitForSeconds(0.2f);
            if (fireMode == FireModes.Burst) yield return new WaitForSeconds(0.4f);
            if (fireMode == FireModes.Auto) yield return new WaitForSeconds(Random.Range(0.2f, 1.3f));
            Item_OnHeldActionEvent(hand, item.GetMainHandle(hand.side), Interactable.Action.UseStop);
        }

        private void UpdateAllLightVolumeReceivers(LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
        {
            foreach (LightVolumeReceiver lvr in GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != item.lightVolumeReceiver))
            {
                Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
            }
        }
    }
}
