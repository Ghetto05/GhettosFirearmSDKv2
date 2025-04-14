using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2;

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
    public GameObject attachmentManager;
    public Attachment parentAttachment;
    public List<SwitchRelation> switches;
    public float lastSwitchTime;
    private IAttachmentManager _attachmentManager;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        if (attachmentManager)
        {
            _attachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
            _attachmentManager.OnHeldAction += OnHeldActionEvent;
        }
        else if (parentAttachment)
        {
            parentAttachment.OnHeldAction += OnHeldActionEvent;
        }

        if (parentAttachment?.Node.TryGetValue("Switch" + gameObject.name, out SaveNodeValueInt value) is not null)
        {
            _current = value.Value;
        }
        else if (_attachmentManager?.SaveData.FirearmNode.TryGetValue("Switch" + gameObject.name, out SaveNodeValueInt value2) is not null)
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
            if (swi)
            {
                AlignSwitch(swi, _current);
            }
        }
    }

    private void OnHeldActionEvent(IAttachmentManager.HeldActionData e)
    {
        if (handle == e.Handle &&
            ((switchAction == Actions.AlternateButtonRelease && e.Action == Interactable.Action.AlternateUseStop) ||
            (switchAction == Actions.AlternateButtonPress && e.Action == Interactable.Action.AlternateUseStart) ||
            (switchAction == Actions.TriggerRelease && e.Action == Interactable.Action.UseStop) ||
            (switchAction == Actions.TriggerPull && e.Action == Interactable.Action.UseStart)))
        {
            e.Handled = true;
            if (Time.time - lastSwitchTime > 0.3f)
            {
                lastSwitchTime = Time.time;
                Switch();
            }
        }
    }

    public void Switch()
    {
        if (switchSound)
        {
            switchSound.Play();
        }
        if (_current + 1 < events.Count)
        {
            _current++;
        }
        else
        {
            _current = 0;
        }
        if (switchSound)
        {
            switchSound.Play();
        }
        events[_current]?.Invoke();
        foreach (var swi in switches)
        {
            if (swi)
            {
                AlignSwitch(swi, _current);
            }
        }

        if (parentAttachment)
        {
            parentAttachment.Node.GetOrAddValue("Switch" + gameObject.name, new SaveNodeValueInt()).Value = _current;
        }
        else if (_attachmentManager is not null)
        {
            _attachmentManager.SaveData.FirearmNode.GetOrAddValue("Switch" + gameObject.name, new SaveNodeValueInt()).Value = _current;
        }
    }

    public void AlignSwitch(SwitchRelation swi, int index)
    {
        if (!swi.usePositionsAsDifferentObjects && swi.switchObject && swi.modePositions.Count > index && swi.modePositions[index])
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