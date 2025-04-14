using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class TacticalDevice : MonoBehaviour
{
    public int channel = 1;
    public GameObject attachmentManager;
    public Attachment attachment;
    public bool physicalSwitch;
    protected IAttachmentManager AttachmentManager;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    protected virtual void InvokedStart()
    {
        if (attachmentManager)
        {
            AttachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
        }
        else if (attachment)
        {
            if (attachment.initialized)
            {
                Attachment_OnDelayedAttachEvent();
            }
            else
            {
                attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            }
        }
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
        AttachmentManager = attachment.attachmentPoint.ConnectedManager;
    }

    protected bool TacSwitchActive
    {
        get
        {
            if (AttachmentManager is null)
            {
                return false;
            }
            var switches = AttachmentManager.Transform.GetComponentsInChildren<PressureSwitch>();
            return !switches.Any() || switches.Any(x => x.Active(channel) && (!x.exclusiveDevice || x.exclusiveDevice == this));
        }
    }
}