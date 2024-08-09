using System;
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

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            if (attachment == null && item == null && handles.Count > 0)
                item = handles[0].item;

            if (attachment != null)
                attachment.attachmentPoint.parentFirearm.item.OnHeldActionEvent += OnAttachmentsAction;
            else if (item != null)
                item.OnHeldActionEvent += OnOffhandAction;

            if (toggleMode)
            {
                if (attachment != null)
                {
                    _saveData = attachment.Node.GetOrAddValue("PressureSwitchState", new SaveNodeValueBool {value = true});
                }
                else if (item.GetComponent<Firearm>() is Firearm firearm)
                {
                    _saveData = firearm.saveData.firearmNode.GetOrAddValue("PressureSwitchState", new SaveNodeValueBool {value = true});
                }

                if (_saveData != null)
                    _active = _saveData.value;
            }

            Invoke(nameof(InitialSet), 1f);
        }

        public void InitialSet()
        {
            item = item != null ? item : attachment != null ? attachment.attachmentPoint.parentFirearm.item : null;
            if (item == null)
                return;

            foreach (TacticalDevice td in item.GetComponentsInChildren<TacticalDevice>())
            {
                td.tacSwitch = _active;
            }
        }

        private void OnDestroy()
        {
            if (attachment != null) attachment.attachmentPoint.parentFirearm.item.OnHeldActionEvent -= OnAttachmentsAction;
            else if (item != null) item.OnHeldActionEvent -= OnOffhandAction;

            item = attachment == null ? item : attachment.attachmentPoint.parentFirearm.item;
            if (item == null) return;

            foreach (TacticalDevice td in item.GetComponentsInChildren<TacticalDevice>())
            {
                td.tacSwitch = true;
            }
        }

        private void OnOffhandAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handles.Contains(handle))
            {
                if (action == Interactable.Action.UseStart)
                {
                    Toggle(true, handle.item);
                }
                else if (action == Interactable.Action.UseStop)
                {
                    Toggle(false, handle.item);
                }
            }
        }

        private void OnAttachmentsAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle != handle.item.mainHandler && !attachment.attachmentPoint.parentFirearm.AllTriggerHandles().Contains(handle))
            {
                if (action == Interactable.Action.UseStart)
                {
                    Toggle(true, handle.item);
                }
                else if (action == Interactable.Action.UseStop)
                {
                    Toggle(false, handle.item);
                }
            }
        }

        public void Toggle(bool active, Item item)
        {
            if (toggleMode && active)
                _active = !_active;
            else if (!toggleMode)
                _active = active;

            if (_active)
                Util.PlayRandomAudioSource(pressSounds);
            else
                Util.PlayRandomAudioSource(releaseSounds);

            foreach (TacticalDevice td in item.GetComponentsInChildren<TacticalDevice>())
            {
                td.tacSwitch = _active;
            }
            
            if (_saveData != null)
                _saveData.value = _active;
        }
    }
}
