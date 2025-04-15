using System;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2;

public class FireModeBasedSwitch : MonoBehaviour
{
    private FirearmBase _firearm;
    public GameObject firearm;
    public Attachment attachment;
    public UnityEvent onSafe;
    public UnityEvent onSemi;
    public UnityEvent onBurst;
    public UnityEvent onAuto;
    public UnityEvent onAttachment;

    private void Start()
    {
        Util.GetParent(firearm, attachment).GetInitialization(Init);
    }

    public void Init(IAttachmentManager manager, IComponentParent parent)
    {
        if (manager is FirearmBase f)
        {
            _firearm = f;
            _firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;
            Firearm_OnFiremodeChangedEvent();
        }
        Util.DelayedExecute(1f, Firearm_OnFiremodeChangedEvent, this);
    }

    private void Firearm_OnFiremodeChangedEvent()
    {
        switch (_firearm.fireMode)
        {
            default:
            case FirearmBase.FireModes.Safe:
                onSafe?.Invoke();
                break;
            case FirearmBase.FireModes.Semi:
                onSemi?.Invoke();
                break;
            case FirearmBase.FireModes.Burst:
                onBurst?.Invoke();
                break;
            case FirearmBase.FireModes.Auto:
                onAuto?.Invoke();
                break;
            case FirearmBase.FireModes.AttachmentFirearm:
                onAttachment?.Invoke();
                break;
        }
    }
}