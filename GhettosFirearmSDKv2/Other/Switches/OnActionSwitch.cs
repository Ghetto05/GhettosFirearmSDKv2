using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
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
        private int _current;
        public List<UnityEvent> events;
        public Item parentItem;
        public Attachment parentAttachment;
        public List<SwitchRelation> switches;
        public float lastSwitchTime;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            if (parentItem != null) parentItem.OnHeldActionEvent += OnHeldActionEvent;
            else if (parentAttachment != null) parentAttachment.OnHeldActionEvent += OnHeldActionEvent;

            if (parentAttachment != null && parentAttachment.Node.TryGetValue("Switch" + gameObject.name, out SaveNodeValueInt value))
            {
                _current = value.Value;
            }
            else if (parentItem != null && parentItem.TryGetComponent(out Firearm firearm) && firearm.SaveData.FirearmNode.TryGetValue("Switch" + gameObject.name, out SaveNodeValueInt value2))
            {
                _current = value2.Value;
            }
            Util.DelayedExecute(1f, Delay, this);
        }

        public void Delay()
        {
            events[_current]?.Invoke();
            foreach (var swi in switches)
            {
                if (swi != null) AlignSwitch(swi, _current);
            }
        }

        private void OnHeldActionEvent(RagdollHand ragdollHand, Handle actionHandle, Interactable.Action action)
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

        public void Switch()
        {
            if (switchSound != null) switchSound.Play();
            if (_current + 1 < events.Count)
            {
                _current++;
            }
            else
            {
                _current = 0;
            }
            if (switchSound != null) switchSound.Play();
            events[_current]?.Invoke();
            foreach (var swi in switches)
            {
                if (swi != null) AlignSwitch(swi, _current);
            }

            if (parentAttachment != null) parentAttachment.Node.GetOrAddValue("Switch" + gameObject.name, new SaveNodeValueInt()).Value = _current;
            else if (parentItem != null && parentItem.TryGetComponent(out Firearm firearm)) firearm.SaveData.FirearmNode.GetOrAddValue("Switch" + gameObject.name, new SaveNodeValueInt()).Value = _current;
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
                foreach (var t in swi.modePositions)
                {
                    t.gameObject.SetActive(false);
                }
                swi.modePositions[_current].gameObject.SetActive(true);
            }
        }
    }
}