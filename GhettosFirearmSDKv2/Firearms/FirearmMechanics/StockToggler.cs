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
        public Item connectedItem;
        public Attachment connectedAttachment;
        public int currentIndex = 0;

        void Awake()
        {
            StartCoroutine(delayedLoad());
        }

        IEnumerator delayedLoad()
        {
            yield return new WaitForSeconds(0.1f);
            if (connectedItem != null)
            {
                connectedItem.OnHeldActionEvent += OnAction;
            }
            else if (connectedAttachment != null)
            {
                connectedAttachment.OnHeldActionEvent += OnAction;
            }
            ApplyPosition(0, false);
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
                else if (connectedItem != null) Debug.Log($"FAILED TO APPLY STOCK POSITION! Firearm {connectedItem.name}: Index {index}, list is {positions.Length} entires long!");
            }
        }

        public delegate void OnToggle(int newIndex, bool playSound);
        public event OnToggle OnToggleEvent;
    }
}
