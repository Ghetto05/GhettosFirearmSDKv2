using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;

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
        private Item _actualItem;
        public float lastHitDistance;

        private void Start()
        {
            physicalSwitch = activeByDefault;
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
            _actualItem = attachment.attachmentPoint.parentFirearm.item;
        }

        private void Update()
        {
            if (!tacSwitch || !physicalSwitch || (_actualItem != null && _actualItem.holder != null))
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
            if (Physics.Raycast(source.position, source.forward, out var hit, range, LayerMask.GetMask("NPC", "Ragdoll", "Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject", "Avatar", "PlayerHandAndFoot")))
            {
                if (cylinderRoot != null)
                {
                    cylinderRoot.localScale = LengthScale(hit.distance);
                    cylinderRoot.gameObject.SetActive(true);
                }
                if (endPointObject != null && !endPointObject.activeInHierarchy) endPointObject.SetActive(true);
                if (endPointObject != null) endPointObject.transform.localPosition = LengthPosition(hit.distance);
                if (distanceDisplay != null) distanceDisplay.text = hit.distance.ToString();
                lastHitDistance = hit.distance;
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

        private Vector3 LengthScale(float length)
        {
            return new Vector3(1, 1, length);
        }

        private Vector3 LengthPosition(float length)
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