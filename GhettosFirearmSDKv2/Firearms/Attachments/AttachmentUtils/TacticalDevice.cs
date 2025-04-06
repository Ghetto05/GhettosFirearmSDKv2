using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class TacticalDevice : MonoBehaviour
{
    public int channel = 1;
    public Item item;
    public Attachment attachment;
    protected Item ActualItem;
    public bool physicalSwitch;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    protected virtual void InvokedStart()
    {
        if (item)
        {
            ActualItem = item;
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
        ActualItem = attachment.attachmentPoint.ConnectedManager.Item;
    }

    protected bool TacSwitchActive
    {
        get
        {
            if (!ActualItem)
            {
                return false;
            }
            var switches = ActualItem.GetComponentsInChildren<PressureSwitch>();
            Debug.Log($"Switches: {switches.Length}\n  {string.Join("\n  ", switches.Select(x => $"Active: {x.Active(channel)} Not exclusive: {!x.exclusiveDevice} Exclusive device: {x.exclusiveDevice}"))}");
            return !switches.Any() || switches.Any(x => x.Active(channel) && (!x.exclusiveDevice || x.exclusiveDevice == this));
        }
    }
}