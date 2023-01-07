using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Events;
using System;

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
            events[current]?.Invoke();
            foreach (SwitchRelation swi in switches)
            {
                if (swi != null) AlignSwitch(swi, current);
            }
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
                if (swi != null) AlignSwitch(swi, current);
            }
        }

        public void AlignSwitch(SwitchRelation swi, int index)
        {
            if (!swi.usePositionsAsDifferentObjects && swi.switchObject != null && swi.modePositions.Count > index && swi.modePositions[index] != null)
            {
                swi.switchObject.localPosition = swi.modePositions[index].localPosition;
                swi.switchObject.localEulerAngles = swi.modePositions[index].localEulerAngles;
            }
            else
            {
                foreach (Transform t in swi.modePositions)
                {
                    t.gameObject.SetActive(false);
                }
                swi.modePositions[current].gameObject.SetActive(true);
            }
        }
    }
}