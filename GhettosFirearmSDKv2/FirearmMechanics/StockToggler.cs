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
        Item item;

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
                item = connectedItem;
            }
            else if (connectedAttachment != null)
            {
                connectedAttachment.attachmentPoint.parentFirearm.item.OnHeldActionEvent += OnAction;
                item = connectedAttachment.attachmentPoint.parentFirearm.item;
            }
            ApplyPosition(0, false);
        }

        private void OnAction(RagdollHand hand, Handle handle, Interactable.Action action)
        {
            if (handle == toggleHandle && action == Interactable.Action.UseStart)
            {
                if (currentIndex + 1 >= positions.Length) currentIndex = -1;
                else currentIndex++;
                ApplyPosition(currentIndex);
            }
            else if (handle == toggleHandle && action == Interactable.Action.AlternateUseStart)
            {
                if (currentIndex - 1 < 0) currentIndex = positions.Length;
                else currentIndex--;
                ApplyPosition(currentIndex);
            }
        }

        public void ApplyPosition(int index, bool playSound = true)
        {
            if (toggleSound != null && playSound) toggleSound.Play();
            pivot.localPosition = positions[index].localPosition;
            pivot.localEulerAngles = positions[index].localEulerAngles;
            OnToggleEvent?.Invoke(index, playSound);
        }

        public delegate void OnToggle(int newIndex, bool playSound);
        public event OnToggle OnToggleEvent;
    }
}
