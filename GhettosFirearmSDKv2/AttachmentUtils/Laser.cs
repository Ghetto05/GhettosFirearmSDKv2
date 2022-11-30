using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2
{
    public class Laser : MonoBehaviour
    {
        public GameObject sourceObject;
        public Transform source;
        public GameObject endPointObject;
        public Transform cylinderRoot;
        public float range;
        public bool activeByDefault;
        bool active = false;
        public Text distanceDisplay;

        void Awake()
        {
            active = activeByDefault;
        }

        void Update()
        {
            if (!active)
            {
                if (cylinderRoot != null) cylinderRoot.localScale = Vector3.zero;
                if (endPointObject != null && endPointObject.activeInHierarchy) endPointObject.SetActive(false);
                return;
            }
            if (Physics.Raycast(source.position, source.forward, out RaycastHit hit, range, LayerMask.GetMask("NPC", "Ragdoll", "Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject", "Avatar", "PlayerHandAndFoot")))
            {
                if (cylinderRoot != null) cylinderRoot.localScale = lengthScale(hit.distance);
                if (endPointObject != null && !endPointObject.activeInHierarchy) endPointObject.SetActive(true);
                if (endPointObject != null) endPointObject.transform.localPosition = lengthPosition(hit.distance);
                if (distanceDisplay != null) distanceDisplay.text = hit.distance.ToString();
            }
            else
            {
                if (cylinderRoot != null) cylinderRoot.localScale = lengthScale(8000f);
                if (endPointObject != null && endPointObject.activeInHierarchy) endPointObject.SetActive(false);
                if (distanceDisplay != null) distanceDisplay.text = "---";
            }
        }

        Vector3 lengthScale(float length)
        {
            return new Vector3(1, 1, length);
        }

        Vector3 lengthPosition(float length)
        {
            return new Vector3(0, 0, length);
        }

        public void SetActive()
        {
            if (sourceObject != null) sourceObject.SetActive(true);
            active = true;
        }

        public void SetNotActive()
        {
            if (sourceObject != null) sourceObject.SetActive(false);
            active = false;
        }
    }
}