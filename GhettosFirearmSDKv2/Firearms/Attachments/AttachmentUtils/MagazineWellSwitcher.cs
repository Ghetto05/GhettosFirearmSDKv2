using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
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

        private ItemSaveData _originalDefaultAmmo;
        public string newDefaultAmmo;

        private void Start()
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

            if (!string.IsNullOrWhiteSpace(newDefaultAmmo) && !attachment.addedByInitialSetup)
            {
                _originalDefaultAmmo = attachment.attachmentPoint.parentFirearm.defaultAmmoItem;
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = newDefaultAmmo;
                _originalDefaultAmmo = attachment.attachmentPoint.parentFirearm.GetAmmoItem();
                    
                attachment.attachmentPoint.parentFirearm.SetSavedAmmoItem(newDefaultAmmo, dataList.Any() ? dataList.ToArray() : null);
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

            if (!string.IsNullOrWhiteSpace(newDefaultAmmo) && !attachment.addedByInitialSetup)
            {
                attachment.attachmentPoint.parentFirearm.SetSavedAmmoItem(_originalDefaultAmmo);
            }
        }
    }
}
