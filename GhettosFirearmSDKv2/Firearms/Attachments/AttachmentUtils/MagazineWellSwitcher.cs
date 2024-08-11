using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class MagazineWellSwitcher : MonoBehaviour
    {
        public Attachment attachment;
        private string _originalType;
        public string newType;

        private string _originalCaliber;
        public string newCaliber;

        private string _originalDefaultAmmo;
        public string newDefaultAmmo;

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
                _originalType = attachment.attachmentPoint.parentFirearm.magazineWell.acceptedMagazineType;
                attachment.attachmentPoint.parentFirearm.magazineWell.acceptedMagazineType = newType;
            }

            if (!string.IsNullOrWhiteSpace(newCaliber))
            {
                _originalCaliber = attachment.attachmentPoint.parentFirearm.magazineWell.caliber;
                attachment.attachmentPoint.parentFirearm.magazineWell.caliber = newCaliber;
            }

            if (!string.IsNullOrWhiteSpace(newDefaultAmmo))
            {
                _originalDefaultAmmo = attachment.attachmentPoint.parentFirearm.defaultAmmoItem;
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = newDefaultAmmo;
            }

            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;

            if (!string.IsNullOrWhiteSpace(newType))
            {
                attachment.attachmentPoint.parentFirearm.magazineWell.acceptedMagazineType = _originalType;
            }

            if (!string.IsNullOrWhiteSpace(newCaliber))
            {
                attachment.attachmentPoint.parentFirearm.magazineWell.caliber = _originalCaliber;
            }

            if (!string.IsNullOrWhiteSpace(newDefaultAmmo))
            {
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = _originalDefaultAmmo;
            }
        }
    }
}
