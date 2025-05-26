using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AmmunitionBeltLink : MonoBehaviour
{
    public Rigidbody rb;
    public Rigidbody staticRb;
    public Transform nextLinkPosition;
    public ConfigurableJoint joint;
    public Transform cartridgePosition;
}