using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MainHandleReplacer : MonoBehaviour
{
    public Attachment attachment;
    public Handle newMainHandle;
    public Handle oldMainHandle;
    private bool _applied;

    private void Awake()
    {
        if (attachment.initialized)
        {
            Attachment_OnDelayedAttachEvent();
        }
        else
        {
            attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        }
        attachment.OnDetachEvent += Attachment_OnDetachEvent;
    }

    private void Attachment_OnDetachEvent(bool despawnDetach)
    {
        if (GetComponentInParent<AttachmentValidator>())
        {
            return;
        }

        oldMainHandle.SetTouch(true);
        oldMainHandle.SetTelekinesis(true);
        oldMainHandle.enabled = true;
        oldMainHandle.gameObject.SetActive(true);

        attachment.attachmentPoint.ConnectedManager.Item.mainHandleLeft = oldMainHandle;
        attachment.attachmentPoint.ConnectedManager.Item.mainHandleRight = oldMainHandle;

        attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
        attachment.OnDetachEvent -= Attachment_OnDetachEvent;
    }

    public void Apply()
    {
        if (_applied && GetComponentInParent<AttachmentValidator>())
        {
            return;
        }

        _applied = true;
        var handlers = oldMainHandle.handlers.ToArray();
        oldMainHandle.Release();
        oldMainHandle.SetTouch(false);
        oldMainHandle.SetTelekinesis(false);
        oldMainHandle.enabled = false;
        oldMainHandle.gameObject.SetActive(false);

        newMainHandle.SetTouch(true);
        newMainHandle.SetTelekinesis(true);
        newMainHandle.enabled = true;
        newMainHandle.gameObject.SetActive(true);

        attachment.attachmentPoint.ConnectedManager.Item.mainHandleLeft = newMainHandle;
        attachment.attachmentPoint.ConnectedManager.Item.mainHandleRight = newMainHandle;

        foreach (var h in handlers)
        {
            h.Grab(newMainHandle);
        }
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        oldMainHandle = attachment.attachmentPoint.ConnectedManager.Item.mainHandleLeft;
        attachment.attachmentPoint.ConnectedManager.OnAttachmentAdded += OnAttachmentChanged;
        Invoke(nameof(Apply), 0.05f);
    }

    private void OnAttachmentChanged(Attachment changedAttachment, AttachmentPoint attachmentPoint)
    { }
}