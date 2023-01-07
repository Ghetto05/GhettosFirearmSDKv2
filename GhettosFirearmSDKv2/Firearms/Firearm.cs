using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using GhettosFirearmSDKv2.SaveData;

namespace GhettosFirearmSDKv2
{
    public class Firearm : FirearmBase
    {
        public List<AttachmentPoint> attachmentPoints;
        public List<Attachment> allAttachments;
        public AttachmentTree attachmentTree;
        public Texture icon;
        public List<Handle> preSnapActiveHandles;

        private void Awake()
        {
            if (item == null) item = this.GetComponent<Item>();
            if (!disableMainFireHandle) mainFireHandle = item.mainHandleLeft;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnSnapEvent += Item_OnSnapEvent2;
            item.OnUnSnapEvent += Item_OnUnSnapEvent2;
            allAttachments = new List<Attachment>();
            foreach (AttachmentPoint ap in attachmentPoints)
            {
                ap.parentFirearm = this;
            }
            StartCoroutine(DelayedLoadAttachments());
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
                if (han.enabled && han.touchCollider.enabled && han != item.mainHandleLeft && han != item.mainHandleRight)
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
            if (!initialSetup)
            {
                attachmentTree = new SaveData.AttachmentTree();
                attachmentTree.GetFromFirearm(this);
                item.RemoveCustomData<SaveData.AttachmentTree>();
                item.AddCustomData(attachmentTree);
            }
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

        public IEnumerator DelayedLoadAttachments()
        {
            yield return new WaitForSeconds(1f);

            Addressables.LoadAssetAsync<Texture>(item.data.iconAddress).Completed += (System.Action<AsyncOperationHandle<Texture>>)(handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    icon = handle.Result;
                }
                else
                {
                    Debug.LogWarning((object)("Unable to load icon texture from location " + item.data.iconAddress));
                    Addressables.Release<Texture>(handle);
                }
            });

            item.TryGetCustomData(out SaveData.AttachmentTree tree);
            if (tree != null)
            {
                attachmentTree = tree;
                tree.ApplyToFirearm(this);
            }
            else
            {
                foreach (AttachmentPoint ap in attachmentPoints)
                {
                    ap.SpawnDefaultAttachment();
                }
            }
            CalculateMuzzle();
        }

        public AttachmentPoint GetSlotFromId(string id)
        {
            foreach (AttachmentPoint point in attachmentPoints)
            {
                if (point.id.Equals(id)) return point;
            }
            return null;
        }

        public override void PlayMuzzleFlash()
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
                    }
                }
            }

            //default
            if (!overridden && defaultMuzzleFlash is ParticleSystem mf) mf.Play();
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
            Transform t = hitscanMuzzle;
            foreach (Attachment a in allAttachments)
            {
                if (a.minimumMuzzlePosition != null && Vector3.Distance(transform.position, a.minimumMuzzlePosition.position) > Vector3.Distance(transform.position, t.position)) t = a.minimumMuzzlePosition;
            }
            actualHitscanMuzzle = t;
        }

        public override bool SaveChamber()
        {
            return true;
        }
    }
}
