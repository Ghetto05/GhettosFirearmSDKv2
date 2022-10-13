using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Switches/Handle action based")]
    public class OnActionSwitch : MonoBehaviour
    {
        public enum Actions
        {
            TriggerPull,
            TriggerRelease,
            AlternateButtonPress,
            AlternateButtonRelease
        }

        public Handle handle;
        public Actions switchAction;
        public AudioSource switchSound;
        private int current = 0;
        public List<UnityEvent> events;
        public Item parentItem;
        public Attachment parentAttachment;
        public List<SwitchRelation> switches;
        public float lastSwitchTime;

        private void Awake()
        {
            if (parentItem != null) parentItem.OnHeldActionEvent += OnHeldActionEvent;
            else if (parentAttachment != null) parentAttachment.OnHeldActionEvent += OnHeldActionEvent;
        }

        private void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if ((switchAction == Actions.AlternateButtonRelease && action == Interactable.Action.AlternateUseStop) || (switchAction == Actions.AlternateButtonPress && action == Interactable.Action.AlternateUseStart) || (switchAction == Actions.TriggerRelease && action == Interactable.Action.UseStop) || (switchAction == Actions.TriggerPull && action == Interactable.Action.UseStart))
            {
                if (Time.time - lastSwitchTime > 0.3f)
                {
                    lastSwitchTime = Time.time;
                    Switch();
                }
            }
        }

        [EasyButtons.Button]
        public void Switch()
        {
            if (switchSound != null) switchSound.Play();
            if (current + 1 < events.Count)
            {
                current++;
            }
            else
            {
                current = 0;
            }
            if (switchSound != null) switchSound.Play();
            events[current]?.Invoke();
            foreach (SwitchRelation swi in switches)
            {
                if (swi != null) swi.AlignSwitch(current);
            }
        }

        [System.Serializable]
        public class SwitchRelation
        {
            public Transform switchObject;
            public bool usePositionsAsDifferentObjects = false;
            public List<Transform> modePositions;

            public void AlignSwitch(int index)
            {
                if (!usePositionsAsDifferentObjects && switchObject != null && modePositions.Count > index && modePositions[index] != null)
                {
                    switchObject.localPosition = modePositions[index].localPosition;
                    switchObject.localEulerAngles = modePositions[index].localEulerAngles;
                }
                else
                {
                    foreach (Transform t in modePositions)
                    {
                        t.gameObject.SetActive(modePositions.IndexOf(t) == index);
                    }
                }
            }
        }
    }
}
