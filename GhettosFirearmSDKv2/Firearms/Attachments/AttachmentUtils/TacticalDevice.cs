using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class TacticalDevice : MonoBehaviour
{
    public string channelName;
    public int channel = 1;
    public GameObject attachmentManager;
    public Attachment attachment;
    public bool physicalSwitch;
    protected IAttachmentManager AttachmentManager;
    private SaveNodeValueInt _channelSaveData;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    protected virtual void InvokedStart()
    {
        if (attachmentManager)
        {
            AttachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
            LoadData(AttachmentManager.SaveData.FirearmNode);
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

    private void LoadData(FirearmSaveData.AttachmentTreeNode node)
    {
        _channelSaveData = node.GetOrAddValue($"TacticalDeviceChannel_{channelName}", new SaveNodeValueInt { Value = channel }, out var addedNew);
        if (!addedNew)
        {
            channel = _channelSaveData.Value;
        }
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
        AttachmentManager = attachment.attachmentPoint.ConnectedManager;
        LoadData(attachment.Node);
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

    public void SetChannel(int ch)
    {
        channel = ch;
        _channelSaveData.Value = ch;
    }
}