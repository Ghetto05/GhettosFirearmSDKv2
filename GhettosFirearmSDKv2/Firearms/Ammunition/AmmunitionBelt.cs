using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AmmunitionBelt : MonoBehaviour
{
    public Item item;
    public Collider loadCollider;
    public AmmunitionBeltLink linkPrefab;
    private List<AmmunitionBeltLink> _links = [];

    private void Start()
    {
        StartCoroutine(Util.RequestItemInitialization(item, Initialization));
    }

    private void Initialization()
    {
        var root = AttachLink(null);
        root.rb.isKinematic = true;
        root.transform.SetParent(transform);
        root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private AmmunitionBeltLink AttachLink(AmmunitionBeltLink target)
    {
        var link = Instantiate(linkPrefab, transform).GetComponent<AmmunitionBeltLink>();
        _links.Add(link);

        if (!target)
            return link;
        
        link.transform.SetParent(target.nextLinkPosition);
        link.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        link.joint = target.staticRb.gameObject.AddComponent<ConfigurableJoint>();
        link.joint.anchor = BoltBase.GrandparentLocalPosition(target.nextLinkPosition, target.transform);
        link.joint.autoConfigureConnectedAnchor = false;
        link.joint.connectedAnchor = Vector3.zero;
        link.joint.connectedBody = link.rb;
        link.joint.xMotion = ConfigurableJointMotion.Locked;
        link.joint.yMotion = ConfigurableJointMotion.Locked;
        link.joint.zMotion = ConfigurableJointMotion.Locked;
        
        return link;
    }
}