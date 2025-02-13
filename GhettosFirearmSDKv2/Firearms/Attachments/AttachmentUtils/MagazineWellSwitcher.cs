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
        public MagazineLoad overrideMagazineLoad;

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
                _originalType = attachment.attachmentPoint.parentManager.magazineWell.acceptedMagazineType;
                attachment.attachmentPoint.parentManager.magazineWell.acceptedMagazineType = newType;
            }

            if (!string.IsNullOrWhiteSpace(newCaliber))
            {
                _originalCaliber = attachment.attachmentPoint.parentManager.magazineWell.caliber;
                attachment.attachmentPoint.parentManager.magazineWell.caliber = newCaliber;
            }

            if (!string.IsNullOrWhiteSpace(newDefaultAmmo) && !attachment.addedByInitialSetup)
            {
                _originalDefaultAmmo = attachment.attachmentPoint.parentManager.GetAmmoItem();

                var dataList = new List<ContentCustomData>();
                
                if (overrideMagazineLoad)
                    dataList.Add(overrideMagazineLoad.ToSaveData());
                    
                attachment.attachmentPoint.parentManager.SetSavedAmmoItem(newDefaultAmmo, dataList.Any() ? dataList.ToArray() : null);
            }

            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;

            if (despawnDetach)
                return;

            if (!string.IsNullOrWhiteSpace(newType))
            {
                attachment.attachmentPoint.parentManager.magazineWell.acceptedMagazineType = _originalType;
            }

            if (!string.IsNullOrWhiteSpace(newCaliber))
            {
                attachment.attachmentPoint.parentManager.magazineWell.caliber = _originalCaliber;
            }

            if (!string.IsNullOrWhiteSpace(newDefaultAmmo) && !attachment.addedByInitialSetup)
            {
                attachment.attachmentPoint.parentManager.SetSavedAmmoItem(_originalDefaultAmmo);
            }
        }
    }
}
