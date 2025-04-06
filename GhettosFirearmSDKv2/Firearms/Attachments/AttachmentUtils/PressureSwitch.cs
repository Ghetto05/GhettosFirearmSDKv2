using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PressureSwitch : TacticalSwitch
    {
        public bool toggleMode;

        public Attachment attachment;
        public List<Handle> handles;
        public Item item;

        public List<AudioSource> pressSounds;
        public List<AudioSource> releaseSounds;

        private bool _active;
        private SaveNodeValueBool _saveData;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            if (attachment)
                attachment.attachmentPoint.ConnectedManager.Item.OnHeldActionEvent += OnHeldAction;
            else if (item)
                item.OnHeldActionEvent += OnHeldAction;
        }

        private void OnHeldAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == handle.item.mainHandleLeft ||
                (attachment?.attachmentPoint.ConnectedManager is FirearmBase f && f.AllTriggerHandles().Contains(handle)))
                return;

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (action)
            {
                case Interactable.Action.UseStart when !TriggerState && (dualMode || !useAltUse):
                    TriggerState = toggleMode ? !TriggerState : true;
                    if (toggleMode)
                        pressSounds.RandomChoice().Play();
                    else
                        (TriggerState ? pressSounds : releaseSounds).RandomChoice().Play();
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
}
