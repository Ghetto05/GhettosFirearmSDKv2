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

        bool ac = false;

        private void Start()
        {
            Invoke("InvokedStart", FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            if (attachment == null && item == null && handles.Count > 0) item = handles[0].item;

            if (attachment != null) attachment.attachmentPoint.parentFirearm.item.OnHeldActionEvent += OnAttachmentsAction;
            else if (item != null) item.OnHeldActionEvent += OnOffhandAction;

            Invoke(nameof(InitialSet), 1f);
        }

        public void InitialSet()
        {
            item = attachment == null ? item : attachment.attachmentPoint.parentFirearm.item;
            if (item == null) return;

            foreach (TacticalDevice td in item.GetComponentsInChildren<TacticalDevice>())
            {
                td.tacSwitch = false;
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
            if (toggleMode && active) ac = !ac;
            else if (!toggleMode) ac = active;

            if (ac) Util.PlayRandomAudioSource(pressSounds);
            else Util.PlayRandomAudioSource(releaseSounds);

            foreach (TacticalDevice td in item.GetComponentsInChildren<TacticalDevice>())
            {
                td.tacSwitch = ac;
            }
        }
    }
}
