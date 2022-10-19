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
        public Handle fireHandle;

        private void Awake()
        {
            item = attachment.transform.parent.GetComponent<AttachmentPoint>().parentFirearm.item;
            attachment.transform.parent.GetComponent<AttachmentPoint>().parentFirearm.OnCollisionEvent += OnCollisionEnter;
            fireHandle.OnHeldActionEvent += FireHandle_OnHeldActionEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
        }

        private void FireHandle_OnHeldActionEvent(RagdollHand ragdollHand, Interactable.Action action)
        {
            base.Item_OnHeldActionEvent(ragdollHand, fireHandle, action);
        }

        public override void PlayMuzzleFlash()
        {
            if (defaultMuzzleFlash is ParticleSystem mf) mf.Play();
        }

        public override bool isSuppressed()
        {
            if (integrallySuppressed) return true;
            return false;
        }

        public override void CalculateMuzzle()
        {
            //Transform t = hitscanMuzzle;
            //foreach (Attachment a in attachment.attachmentPoint.parentFirearm.allAttachments)
            //{
            //    if (a.minimumMuzzlePosition != null && Vector3.Distance(hitscanMuzzle.position, a.minimumMuzzlePosition.position) > Vector3.Distance(hitscanMuzzle.position, t.position)) t = a.minimumMuzzlePosition;
            //}
            //actualHitscanMuzzle = t;
        }
    }
}
