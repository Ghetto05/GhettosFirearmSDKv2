using UnityEngine;
using ThunderRoad;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Illuminators/Tactical Light")]
    public class TacLight : TacticalDevice
    {
        public GameObject lights;
        public Item item;
        public Attachment attachment;
        private Item actualItem;

        public void Start()
        {
            if (item != null) actualItem = item;
            else if (attachment != null)
            {
                if (attachment.initialized) Attachment_OnDelayedAttachEvent();
                else attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            }
            else actualItem = null;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            actualItem = attachment.attachmentPoint.parentFirearm.item;
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
            lights.SetActive(tacSwitch && physicalSwitch && (actualItem == null || actualItem.holder == null));
        }
    }
}
