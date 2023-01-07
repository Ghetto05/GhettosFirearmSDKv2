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
            attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent()
        {
            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;

            oldMainHandle.SetTouch(true);
            oldMainHandle.SetTelekinesis(true);
            oldMainHandle.enabled = true;

            attachment.attachmentPoint.parentFirearm.item.mainHandleLeft = oldMainHandle;
            attachment.attachmentPoint.parentFirearm.item.mainHandleRight = oldMainHandle;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            oldMainHandle = attachment.attachmentPoint.parentFirearm.item.mainHandleLeft;
            oldMainHandle.SetTouch(false);
            oldMainHandle.SetTelekinesis(false);
            oldMainHandle.enabled = false;

            attachment.attachmentPoint.parentFirearm.item.mainHandleLeft = newMainHandle;
            attachment.attachmentPoint.parentFirearm.item.mainHandleRight = newMainHandle;
        }
    }
}
