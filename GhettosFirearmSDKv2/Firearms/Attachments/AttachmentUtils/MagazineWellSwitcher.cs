using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

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

    private FirearmBase _firearm;

    private void Start()
    {
        if (attachment.initialized)
        {
            Attachment_OnDelayedAttachEvent();
        }
        else
        {
            attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        }
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        if (attachment.attachmentPoint.ConnectedManager is not FirearmBase f)
        {
            return;
        }

        _firearm = f;

        attachment.OnDetachEvent += Attachment_OnDetachEvent;

        if (!string.IsNullOrWhiteSpace(newType))
        {
            _originalType = _firearm.magazineWell.acceptedMagazineType;
            _firearm.magazineWell.acceptedMagazineType = newType;
        }

        if (!string.IsNullOrWhiteSpace(newCaliber))
        {
            _originalCaliber = _firearm.magazineWell.caliber;
            _firearm.magazineWell.caliber = newCaliber;
        }

        if (!string.IsNullOrWhiteSpace(newDefaultAmmo) && !attachment.addedByInitialSetup)
        {
            _originalDefaultAmmo = _firearm.GetAmmoItem();

            var dataList = new List<ContentCustomData>();

            if (overrideMagazineLoad)
            {
                dataList.Add(overrideMagazineLoad.ToSaveData());
            }

            _firearm.SetSavedAmmoItem(newDefaultAmmo, dataList.Any() ? dataList.ToArray() : null);
        }

        attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
    }

    private void Attachment_OnDetachEvent(bool despawnDetach)
    {
        attachment.OnDetachEvent -= Attachment_OnDetachEvent;

        if (despawnDetach || !_firearm)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(newType))
        {
            _firearm.magazineWell.acceptedMagazineType = _originalType;
        }

        if (!string.IsNullOrWhiteSpace(newCaliber))
        {
            _firearm.magazineWell.caliber = _originalCaliber;
        }

        if (!string.IsNullOrWhiteSpace(newDefaultAmmo) && !attachment.addedByInitialSetup)
        {
            _firearm.SetSavedAmmoItem(_originalDefaultAmmo);
        }
    }
}