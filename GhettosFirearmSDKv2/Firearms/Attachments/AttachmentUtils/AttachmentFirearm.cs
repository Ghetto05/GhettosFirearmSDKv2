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
            mainFireHandle = fireHandle;
            attachment.transform.parent.GetComponent<AttachmentPoint>().parentFirearm.OnCollisionEvent += OnCollisionEnter;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            CalculateMuzzle();
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            if (handle == fireHandle && bolt != null) bolt.Initialize();
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
            actualHitscanMuzzle = hitscanMuzzle;
        }
    }
}
