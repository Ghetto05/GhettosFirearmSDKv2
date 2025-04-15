using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
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
    protected IComponentParent Parent;

    private void Start()
    {
        Util.GetParent(attachmentManager, attachment).GetInitialization(Init);
    }

    protected virtual void Init(IAttachmentManager manager, IComponentParent parent)
    {
        AttachmentManager = manager;
        Parent = parent;
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