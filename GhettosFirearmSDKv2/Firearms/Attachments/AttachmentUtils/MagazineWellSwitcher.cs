using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class MagazineWellSwitcher : MonoBehaviour
    {
        public Attachment attachment;
        private string originalType;
        public string newType;

        private string originalCaliber;
        public string newCaliber;

        private void Awake()
        {
            if (attachment.initialized) Attachment_OnDelayedAttachEvent();
            else attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            attachment.OnDetachEvent += Attachment_OnDetachEvent;

            if (!string.IsNullOrWhiteSpace(newType))
            {
                originalType = attachment.attachmentPoint.parentFirearm.magazineWell.acceptedMagazineType;
                attachment.attachmentPoint.parentFirearm.magazineWell.acceptedMagazineType = newType;
            }

            if (!string.IsNullOrWhiteSpace(newCaliber))
            {
                originalCaliber = attachment.attachmentPoint.parentFirearm.magazineWell.caliber;
                attachment.attachmentPoint.parentFirearm.magazineWell.caliber = newCaliber;
            }

            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;

            if (!string.IsNullOrWhiteSpace(newType))
            {
                attachment.attachmentPoint.parentFirearm.magazineWell.acceptedMagazineType = originalType;
            }

            if (!string.IsNullOrWhiteSpace(newCaliber))
            {
                attachment.attachmentPoint.parentFirearm.magazineWell.caliber = originalCaliber;
            }
        }
    }
}
