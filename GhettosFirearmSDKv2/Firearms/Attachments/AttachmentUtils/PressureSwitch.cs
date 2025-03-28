using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PressureSwitch : MonoBehaviour
    {
        public bool toggleMode;

        public Attachment attachment;
        public List<Handle> handles;
        public Item item;

        public List<AudioSource> pressSounds;
        public List<AudioSource> releaseSounds;

        private bool _active;
        private SaveNodeValueBool _saveData;


        public bool dualMode;
        public bool useAltUse;
        public int triggerChannel = 1;
        public int alternateUseChannel = 2;
        public TacticalDevice exclusiveDevice;

        private bool _triggerState;
        private bool _alternateUseState;

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
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (action)
            {
                case Interactable.Action.UseStart when !_triggerState && (!dualMode || !useAltUse):
                    _triggerState = true;
                    pressSounds.RandomChoice().Play();
                    break;
                case Interactable.Action.UseStop when _triggerState && (!dualMode || !useAltUse):
                    _triggerState = false;
                    releaseSounds.RandomChoice().Play();
                    break;
                case Interactable.Action.UseStart when !dualMode || useAltUse:
                    _triggerState = !_triggerState;
                    if (_triggerState)
                        pressSounds.RandomChoice().Play();
                    else
                        releaseSounds.RandomChoice().Play();
                    break;
            }
        }

        public bool Active(int channel)
        {
            if (!dualMode)
                return true;
            return (channel == triggerChannel && _triggerState) || (channel == alternateUseChannel && _alternateUseState);
        }
    }
}
