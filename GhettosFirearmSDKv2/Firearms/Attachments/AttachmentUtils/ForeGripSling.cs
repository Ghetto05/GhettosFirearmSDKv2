using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class ForeGripSling : MonoBehaviour
{
    public Rigidbody rb;
    public Transform axis;
    public Transform heldPosition;
    public Handle[] handles;
    public Attachment attachment;
    public Item item;
    private bool _following;

    private void Start()
    {
        Invoke(nameof(InvokedStart), 1f);
        UnGrab();
    }

    private void InvokedStart()
    {
        if (attachment != null)
            item = attachment.attachmentPoint.ConnectedManager.Item;
            
        item.OnGrabEvent += OnGrab;
        item.OnUngrabEvent += OnUnGrab;
    }

    private void OnUnGrab(Handle handle, RagdollHand ragdollHand, bool throwing)
    {
        if (handles.Contains(handle))
            UnGrab();
    }

    private void OnGrab(Handle handle, RagdollHand ragdollHand)
    {
        if (handles.Contains(handle))
            Grab();
    }

    private void FixedUpdate()
    {
        if (_following)
            axis.localEulerAngles = new Vector3(rb.transform.localEulerAngles.x, 0, 0);
    }

    private void Grab()
    {
        _following = false;
        rb.isKinematic = true;
        axis.localEulerAngles = heldPosition.localEulerAngles;
        rb.transform.localEulerAngles = heldPosition.localEulerAngles;
    }

    private void UnGrab()
    {
        _following = true;
        rb.isKinematic = false;
    }
}