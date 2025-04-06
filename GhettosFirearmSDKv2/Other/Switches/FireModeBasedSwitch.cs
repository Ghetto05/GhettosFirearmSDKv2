using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2;

public class FireModeBasedSwitch : MonoBehaviour
{
    public FirearmBase firearm;
    public Attachment attachment;
    public UnityEvent onSafe;
    public UnityEvent onSemi;
    public UnityEvent onBurst;
    public UnityEvent onAuto;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        if (!firearm && attachment)
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

        if (firearm)
        {
            firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;
            Firearm_OnFiremodeChangedEvent();
        }
        Util.DelayedExecute(1f, Firearm_OnFiremodeChangedEvent, this);
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        if (attachment.attachmentPoint.ConnectedManager is not FirearmBase f)
        {
            return;
        }
        firearm = f;
        firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;
        Util.DelayedExecute(1f, Firearm_OnFiremodeChangedEvent, this);
    }

    private void Firearm_OnFiremodeChangedEvent()
    {
        if (firearm.fireMode == FirearmBase.FireModes.Safe)
        {
            onSafe?.Invoke();
        }
        else if (firearm.fireMode == FirearmBase.FireModes.Semi)
        {
            onSemi?.Invoke();
        }
        else if (firearm.fireMode == FirearmBase.FireModes.Burst)
        {
            onBurst?.Invoke();
        }
        else if (firearm.fireMode == FirearmBase.FireModes.Auto)
        {
            onAuto?.Invoke();
        }
    }
}