using System.Linq;
using GhettosFirearmSDKv2.Attachments;
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
    public GameObject attachmentManager;
    private bool _following;
    private Item _item;

    private void Start()
    {
        Invoke(nameof(InvokedStart), 1f);
        UnGrab();
    }

    private void InvokedStart()
    {
        if (attachment)
        {
            _item = attachment.attachmentPoint.ConnectedManager.Item;
        }
        if (attachmentManager)
        {
            _item = attachmentManager.GetComponent<IAttachmentManager>().Item;
        }

        _item.OnGrabEvent += OnGrab;
        _item.OnUngrabEvent += OnUnGrab;
    }

    private void OnUnGrab(Handle handle, RagdollHand ragdollHand, bool throwing)
    {
        if (handles.Contains(handle))
        {
            UnGrab();
        }
    }

    private void OnGrab(Handle handle, RagdollHand ragdollHand)
    {
        if (handles.Contains(handle))
        {
            Grab();
        }
    }

    private void FixedUpdate()
    {
        if (_following)
        {
            axis.localEulerAngles = new Vector3(rb.transform.localEulerAngles.x, 0, 0);
        }
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