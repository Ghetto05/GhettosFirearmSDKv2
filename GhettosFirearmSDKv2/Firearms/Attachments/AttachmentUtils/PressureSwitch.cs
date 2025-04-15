using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class PressureSwitch : TacticalSwitch
{
    public bool toggleMode;

    public Attachment attachment;
    public GameObject attachmentManager;
    public List<Handle> handles;

    public List<AudioSource> pressSounds;
    public List<AudioSource> releaseSounds;

    private IAttachmentManager _attachmentManager;
    private IComponentParent _parent;

    private void Start()
    {
        Util.GetParent(attachmentManager, attachment).GetInitialization(Init);
    }

    public void Init(IAttachmentManager manager, IComponentParent parent)
    {
        _parent = parent;
        _attachmentManager = manager;
        _parent.OnUnhandledHeldAction += OnUnhandledHeldAction;
    }

    private void OnUnhandledHeldAction(IComponentParent.HeldActionData e)
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
                TriggerState = !toggleMode || !TriggerState;
                if (toggleMode)
                {
                    pressSounds.RandomChoice().Play();
                }
                else
                {
                    (TriggerState ? pressSounds : releaseSounds).RandomChoice().Play();
                }
                break;

            case Interactable.Action.UseStop when TriggerState && (dualMode || !useAltUse):
                TriggerState = false;
                releaseSounds.RandomChoice().Play();
                break;

            case Interactable.Action.AlternateUseStart when dualMode || useAltUse:
                AlternateUseState = !AlternateUseState;
                (AlternateUseState ? pressSounds : releaseSounds).RandomChoice().Play();
                break;
        }
    }
}