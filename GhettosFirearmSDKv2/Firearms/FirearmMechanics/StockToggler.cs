using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class StockToggler : MonoBehaviour
    {
        public AudioSource toggleSound;
        public Handle toggleHandle;
        public Transform pivot;
        public Transform[] positions;
        public Firearm connectedFirearm;
        public Attachment connectedAttachment;
        public int currentIndex = 0;
        SaveNodeValueInt stockPosition;

        void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            if (connectedFirearm != null)
            {
                connectedFirearm.item.OnHeldActionEvent += OnAction;
                stockPosition = connectedFirearm.saveData.firearmNode.GetOrAddValue("StockPosition" + name, new SaveNodeValueInt());
                currentIndex = stockPosition.value;
                ApplyPosition(stockPosition.value, false);
            }
            else if (connectedAttachment != null)
            {
                if (!connectedAttachment.initialized) connectedAttachment.OnDelayedAttachEvent += ConnectedAttachment_OnDelayedAttachEvent;
                else ConnectedAttachment_OnDelayedAttachEvent();
            }
        }

        private void ConnectedAttachment_OnDelayedAttachEvent()
        {
            connectedAttachment.OnHeldActionEvent += OnAction;
            stockPosition = connectedAttachment.node.GetOrAddValue("StockPosition" + name, new SaveNodeValueInt());
            currentIndex = stockPosition.value;
            ApplyPosition(stockPosition.value, false);
        }

        private void OnAction(RagdollHand hand, Handle handle, Interactable.Action action)
        {
            if (handle == toggleHandle && action == Interactable.Action.UseStart)
            {
                if (currentIndex + 1 == positions.Length) currentIndex = 0;
                else currentIndex++;
                ApplyPosition(currentIndex);
            }
            else if (handle == toggleHandle && action == Interactable.Action.AlternateUseStart)
            {
                if (currentIndex - 1 == -1) currentIndex = positions.Length - 1;
                else currentIndex--;
                ApplyPosition(currentIndex);
            }
            stockPosition.value = currentIndex;
        }

        public void ApplyPosition(int index, bool playSound = true)
        {
            try
            {
                if (toggleSound != null && playSound) toggleSound.Play();
                pivot.localPosition = positions[index].localPosition;
                pivot.localEulerAngles = positions[index].localEulerAngles;
                OnToggleEvent?.Invoke(index, playSound);
            }
            catch (System.Exception)
            {
                if (connectedAttachment != null) Debug.Log($"FAILED TO APPLY STOCK POSITION! Attachment {connectedAttachment.name} on firearm {connectedAttachment.attachmentPoint.parentFirearm.name}: Index {index}, list is {positions.Length} entires long!");
                else if (connectedFirearm != null) Debug.Log($"FAILED TO APPLY STOCK POSITION! Firearm {connectedFirearm.name}: Index {index}, list is {positions.Length} entires long!");
            }
        }

        public delegate void OnToggle(int newIndex, bool playSound);
        public event OnToggle OnToggleEvent;
    }
}
