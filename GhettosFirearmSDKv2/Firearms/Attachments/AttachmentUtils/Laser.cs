using System.Globalization;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2;

public class Laser : TacticalDevice
{
    public GameObject sourceObject;
    public Transform source;
    public GameObject endPointObject;
    public Transform cylinderRoot;
    public float range;
    public bool activeByDefault;
    public Text distanceDisplay;
    public float lastHitDistance;

    protected override void Init(IAttachmentManager manager, IComponentParent parent)
    {
        base.Init(manager, parent);
        physicalSwitch = activeByDefault;
    }

    private void Update()
    {
        if (!TacSwitchActive || !physicalSwitch || (AttachmentManager is not null && AttachmentManager.Item.holder))
        {
            if (cylinderRoot)
            {
                cylinderRoot.localScale = Vector3.zero;
                cylinderRoot.gameObject.SetActive(false);
            }

            if (endPointObject && endPointObject.activeInHierarchy)
            {
                endPointObject.SetActive(false);
            }
            if (distanceDisplay)
            {
                distanceDisplay.text = "";
            }
            return;
        }

        if (Physics.Raycast(source.position, source.forward, out var hit, range,
                LayerMask.GetMask("NPC", "Ragdoll", "Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject",
                    "Avatar", "PlayerHandAndFoot")))
        {
            if (cylinderRoot)
            {
                cylinderRoot.localScale = LengthScale(hit.distance);
                cylinderRoot.gameObject.SetActive(true);
            }

            if (endPointObject && !endPointObject.activeInHierarchy)
            {
                endPointObject.SetActive(true);
            }
            if (endPointObject)
            {
                endPointObject.transform.localPosition = LengthPosition(hit.distance);
            }
            if (distanceDisplay)
            {
                distanceDisplay.text = hit.distance.ToString(CultureInfo.InvariantCulture);
            }
            lastHitDistance = hit.distance;
        }
        else
        {
            if (cylinderRoot)
            {
                cylinderRoot.localScale = LengthScale(200);
                cylinderRoot.gameObject.SetActive(true);
            }

            if (endPointObject && endPointObject.activeInHierarchy)
            {
                endPointObject.SetActive(false);
            }
            if (distanceDisplay)
            {
                distanceDisplay.text = "---";
            }
        }
    }

    private static Vector3 LengthScale(float length)
    {
        return new Vector3(1, 1, length);
    }

    private static Vector3 LengthPosition(float length)
    {
        return new Vector3(0, 0, length);
    }

    public void SetActive()
    {
        if (sourceObject)
        {
            sourceObject.SetActive(true);
        }
        physicalSwitch = true;
    }

    public void SetNotActive()
    {
        if (sourceObject)
        {
            sourceObject.SetActive(false);
        }
        physicalSwitch = false;
    }
}