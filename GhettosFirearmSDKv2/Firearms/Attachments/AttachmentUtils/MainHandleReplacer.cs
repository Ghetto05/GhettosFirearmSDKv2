using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class MainHandleReplacer : MonoBehaviour
    {
        public Attachment attachment;
        public Handle newMainHandle;
        public Handle oldMainHandle;

        private void Awake()
        {
            if (attachment.initialized) Attachment_OnDelayedAttachEvent();
            else attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            oldMainHandle.SetTouch(true);
            oldMainHandle.SetTelekinesis(true);
            oldMainHandle.enabled = true;
            oldMainHandle.gameObject.SetActive(true);

            attachment.attachmentPoint.parentFirearm.item.mainHandleLeft = oldMainHandle;
            attachment.attachmentPoint.parentFirearm.item.mainHandleRight = oldMainHandle;

            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;
        }

        public void Apply()
        {
            List<RagdollHand> handlers = oldMainHandle.handlers;
            oldMainHandle.Release();
            oldMainHandle.SetTouch(false);
            oldMainHandle.SetTelekinesis(false);
            oldMainHandle.enabled = false;
            oldMainHandle.gameObject.SetActive(false);


            newMainHandle.SetTouch(true);
            newMainHandle.SetTelekinesis(true);
            newMainHandle.enabled = true;
            newMainHandle.gameObject.SetActive(true);
            
            handlers.ForEach(h => h.Grab(newMainHandle));

            attachment.attachmentPoint.parentFirearm.item.mainHandleLeft = newMainHandle;
            attachment.attachmentPoint.parentFirearm.item.mainHandleRight = newMainHandle;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            oldMainHandle = attachment.attachmentPoint.parentFirearm.item.mainHandleLeft;
            attachment.attachmentPoint.parentFirearm.OnAttachmentAddedEvent += OnAttachmentChanged;
            Apply();
        }

        private void OnAttachmentChanged(Attachment attachment, AttachmentPoint attachmentPoint)
        {
        }
    }
}
