using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Attachments/Systems/Illuminators/Tactical Light")]
public class TacLight : TacticalDevice
{
    public GameObject lights;
    public Item item;
    public Attachment attachment;
    private Item _actualItem;

    public void Start()
    {
        if (item != null) _actualItem = item;
        else if (attachment != null)
        {
            if (attachment.initialized) Attachment_OnDelayedAttachEvent();
            else attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        }
        else _actualItem = null;
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        _actualItem = attachment.attachmentPoint.ConnectedManager.Item;
    }

    public void SetActive()
    {
        physicalSwitch = true;
    }

    public void SetNotActive()
    {
        physicalSwitch = false;
    }

    private void Update()
    {
        lights.SetActive(tacSwitch && physicalSwitch && (_actualItem == null || _actualItem.holder == null));
    }
}