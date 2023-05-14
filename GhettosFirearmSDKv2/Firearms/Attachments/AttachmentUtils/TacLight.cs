using UnityEngine;
using ThunderRoad;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Illuminators/Tactical Light")]
    public class TacLight : MonoBehaviour
    {
        public GameObject lights;
        public Item item;
        public Attachment attachment;

        private Item actualItem;
        private bool switchActive;

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
            switchActive = true;
        }

        public void SetNotActive()
        {
            switchActive = false;
        }

        private void Update()
        {
            lights.SetActive(switchActive && (actualItem == null || actualItem.holder == null));
        }
    }
}
