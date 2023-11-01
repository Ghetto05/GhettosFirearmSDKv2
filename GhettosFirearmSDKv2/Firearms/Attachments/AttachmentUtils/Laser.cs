using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class Laser : TacticalDevice
    {
        public GameObject sourceObject;
        public Transform source;
        public GameObject endPointObject;
        public Transform cylinderRoot;
        public float range;
        public bool activeByDefault;
        public Text distanceDisplay;
        public Item item;
        public Attachment attachment;
        private Item actualItem;

        private void Start()
        {
            physicalSwitch = activeByDefault;
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

        private void Update()
        {
            if (!tacSwitch || !physicalSwitch || (actualItem != null && actualItem.holder != null))
            {
                if (cylinderRoot != null)
                {
                    cylinderRoot.localScale = Vector3.zero;
                    cylinderRoot.gameObject.SetActive(false);
                }
                if (endPointObject != null && endPointObject.activeInHierarchy) endPointObject.SetActive(false);
                if (distanceDisplay != null) distanceDisplay.text = "";
                return;
            }
            if (Physics.Raycast(source.position, source.forward, out RaycastHit hit, range, LayerMask.GetMask("NPC", "Ragdoll", "Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject", "Avatar", "PlayerHandAndFoot")))
            {
                if (cylinderRoot != null)
                {
                    cylinderRoot.localScale = LengthScale(hit.distance);
                    cylinderRoot.gameObject.SetActive(true);
                }
                if (endPointObject != null && !endPointObject.activeInHierarchy) endPointObject.SetActive(true);
                if (endPointObject != null) endPointObject.transform.localPosition = LengthPosition(hit.distance);
                if (distanceDisplay != null) distanceDisplay.text = hit.distance.ToString();
            }
            else
            {
                if (cylinderRoot != null)
                {
                    cylinderRoot.localScale = LengthScale(200);
                    cylinderRoot.gameObject.SetActive(true);
                }
                if (endPointObject != null && endPointObject.activeInHierarchy) endPointObject.SetActive(false);
                if (distanceDisplay != null) distanceDisplay.text = "---";
            }
        }

        Vector3 LengthScale(float length)
        {
            return new Vector3(1, 1, length);
        }

        Vector3 LengthPosition(float length)
        {
            return new Vector3(0, 0, length);
        }

        public void SetActive()
        {
            if (sourceObject != null) sourceObject.SetActive(true);
            physicalSwitch = true;
        }

        public void SetNotActive()
        {
            if (sourceObject != null) sourceObject.SetActive(false);
            physicalSwitch = false;
        }
    }
}