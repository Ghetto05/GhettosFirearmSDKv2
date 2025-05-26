using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class PressureSwitch : TacticalSwitch
{
    public bool toggleMode;

    public Attachment attachment;
    public List<Handle> handles;
    public GameObject attachmentManager;

    public List<AudioSource> pressSounds;
    public List<AudioSource> releaseSounds;

    private IAttachmentManager _attachmentManager;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        if (attachment)
        {
            _attachmentManager = attachment.attachmentPoint.ConnectedManager;
        }
        else if (attachmentManager)
        {
            _attachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
        }
        _attachmentManager.OnUnhandledHeldAction += OnUnhandledHeldAction;
        OnInvokedStart();
    }

    private void OnUnhandledHeldAction(IInteractionProvider.HeldActionData e)
    {
        if (e.Handle == e.Handle.item.mainHandleLeft ||
            (_attachmentManager is FirearmBase f && f.AllTriggerHandles().Contains(e.Handle)))
        {
            return;
        }

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (e.Action)
        {
            case Interactable.Action.UseStart when !TriggerState && (dualMode || !useAltUse):
                TriggerState = toggleMode ? !TriggerState : true;
                if (toggleMode)
                {
                    Util.PlayRandomAudioSource(pressSounds);
                }
                else
                {
                    Util.PlayRandomAudioSource(TriggerState ? pressSounds : releaseSounds);
                }
                break;

            case Interactable.Action.UseStop when TriggerState && (dualMode || !useAltUse):
                TriggerState = false;
                Util.PlayRandomAudioSource(releaseSounds);
                break;

            case Interactable.Action.AlternateUseStart when dualMode || useAltUse:
                AlternateUseState = !AlternateUseState;
                Util.PlayRandomAudioSource(AlternateUseState ? pressSounds : releaseSounds);
                break;
        }
    }

    public override FirearmSaveData.AttachmentTreeNode GetNode()
    {
        return attachment ? attachment.Node : _attachmentManager != null ? _attachmentManager.SaveData.FirearmNode : null;
    }
}