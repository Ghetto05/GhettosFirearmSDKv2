using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using GhettosFirearmSDKv2.SaveData;

namespace GhettosFirearmSDKv2
{
    public class AttachmentFirearm : FirearmBase
    {
        public Attachment attachment;

        private void Awake()
        {
            item = attachment.attachmentPoint.parentFirearm.item;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
        }

        public override void PlayMuzzleFlash()
        {
            bool overridden = false;
            foreach (Attachment at in attachment.attachmentPoint.parentFirearm.allAttachments)
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
            foreach (Attachment at in attachment.attachmentPoint.parentFirearm.allAttachments)
            {
                if (at.isSuppressing) return true;
            }
            return false;
        }

        public override void CalculateMuzzle()
        {
            Transform t = hitscanMuzzle;
            foreach (Attachment a in attachment.attachmentPoint.parentFirearm.allAttachments)
            {
                if (a.minimumMuzzlePosition != null && Vector3.Distance(hitscanMuzzle.position, a.minimumMuzzlePosition.position) > Vector3.Distance(hitscanMuzzle.position, t.position)) t = a.minimumMuzzlePosition;
            }
            actualHitscanMuzzle = t;
        }
    }
}
