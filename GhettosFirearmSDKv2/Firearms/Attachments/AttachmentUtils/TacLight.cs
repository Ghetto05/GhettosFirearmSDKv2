using UnityEngine;
using ThunderRoad;

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
            else if (attachment != null) actualItem = attachment.attachmentPoint.parentFirearm.item;
            else actualItem = null;
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
