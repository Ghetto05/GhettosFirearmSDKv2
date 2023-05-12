using System.Collections;
using System.Collections.Generic;
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
        public string defaultAmmoItem;

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

        public override void Awake()
        {
            base.Awake();
            if (item == null) item = this.GetComponent<Item>();
            if (!disableMainFireHandle) mainFireHandle = item.mainHandleLeft;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnSnapEvent += Item_OnSnapEvent2;
            item.OnUnSnapEvent += Item_OnUnSnapEvent2;
            item.mainCollisionHandler.OnCollisionStartEvent += InvokeCollisionTR;
            allAttachments = new List<Attachment>();
            OnAIFire = new FireableEvent(AIFire);
            foreach (AttachmentPoint ap in attachmentPoints)
            {
                ap.parentFirearm = this;
            }
            StartCoroutine(DelayedLoad());
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
                han.SetTouch(true);
            }
        }

        public void Item_OnSnapEvent2(Holder holder)
        {
            preSnapActiveHandles = new List<Handle>();
            foreach (Handle han in item.handles)
            {
                if (han.enabled && han.touchCollider.enabled && !(han.data.id.Equals("ObjectHandleHeavy") || han.data.id.Equals("ObjectHandleHeavyPistol")))
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
            yield return new WaitForSeconds(0.4f);

            if (!item.TryGetCustomData(out saveData))
            {
                saveData = new FirearmSaveData();
                saveData.firearmNode = new FirearmSaveData.AttachmentTreeNode();
                item.AddCustomData(saveData);
            }

            saveData.ApplyToFirearm(this);

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
            CalculateMuzzle();

            if (FirearmsSettings.debugMode)
            {
                foreach (Handle h in gameObject.GetComponentsInChildren<Handle>())
                {
                    if (h.GetType() != typeof(GhettoHandle)) Debug.LogWarning("Handle " + h.gameObject.name + " on firearm " + gameObject.name + " is not of type GhettoHandle!");
                }
            }

            yield return new WaitForSeconds(2.3f);
            if (item.holder != null)
            {
                Holder h = item.holder;
                item.holder.UnSnap(item, true);
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
                if (at.overridesMuzzleFlash) overridden = true;
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

        public override bool isSuppressed()
        {
            if (integrallySuppressed) return true;
            foreach (Attachment at in allAttachments)
            {
                if (at.isSuppressing) return true;
            }
            return false;
        }

        public override void CalculateMuzzle()
        {
            if (hitscanMuzzle == null) return;
            Transform t = hitscanMuzzle;
            foreach (Attachment a in allAttachments)
            {
                if (a.minimumMuzzlePosition != null && Vector3.Distance(transform.position, a.minimumMuzzlePosition.position) > Vector3.Distance(transform.position, t.position)) t = a.minimumMuzzlePosition;
            }
            actualHitscanMuzzle = t;
            base.CalculateMuzzle();
        }

        public bool AIFire(AIFireable fireable, RagdollHand hand, bool finished)
        {
            if (bolt != null) bolt.TryFire();
            return true;
        }
    }
}
