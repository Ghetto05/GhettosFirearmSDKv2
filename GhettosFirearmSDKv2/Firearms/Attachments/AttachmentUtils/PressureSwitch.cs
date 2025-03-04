using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

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
        if (!attachment && !item && handles.Count > 0)
            item = handles[0].item;

        if (attachment)
            attachment.attachmentPoint.ConnectedManager.Item.OnHeldActionEvent += OnAttachmentsAction;
        else if (item)
            item.OnHeldActionEvent += OnOffhandAction;

        if (toggleMode)
        {
            if (attachment)
            {
                _saveData = attachment.Node.GetOrAddValue("PressureSwitchState", new SaveNodeValueBool {Value = true});
            }
            else if (item.GetComponent<IAttachmentManager>() is { } manager)
            {
                _saveData = manager.SaveData.FirearmNode.GetOrAddValue("PressureSwitchState", new SaveNodeValueBool {Value = true});
            }

            if (_saveData != null)
                _active = _saveData.Value;
        }

        Invoke(nameof(InitialSet), 1f);
    }

    public void InitialSet()
    {
        item = item ? item : attachment ? attachment.attachmentPoint.ConnectedManager.Item : null;
        if (!item)
            return;

        foreach (var td in item.GetComponentsInChildren<TacticalDevice>())
        {
            td.tacSwitch = _active;
        }
    }

    private void OnDestroy()
    {
        if (attachment) attachment.attachmentPoint.ConnectedManager.Item.OnHeldActionEvent -= OnAttachmentsAction;
        else if (item) item.OnHeldActionEvent -= OnOffhandAction;

        item = !attachment ? item : attachment.attachmentPoint.ConnectedManager.Item;
        if (!item) return;

        foreach (var td in item.GetComponentsInChildren<TacticalDevice>())
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
        if (handle == handle.item.mainHandleLeft ||
            (attachment.attachmentPoint.ConnectedManager is FirearmBase f && f.AllTriggerHandles().Contains(handle))) return;
        switch (action)
        {
            case Interactable.Action.UseStart:
                Toggle(true, handle.item);
                break;
            case Interactable.Action.UseStop:
                Toggle(false, handle.item);
                break;
        }
    }

    public void Toggle(bool active, Item itemToToggleOn)
    {
        if (toggleMode && active)
            _active = !_active;
        else if (!toggleMode)
            _active = active;

        if (_active)
            Util.PlayRandomAudioSource(pressSounds);
        else
            Util.PlayRandomAudioSource(releaseSounds);

        foreach (var td in itemToToggleOn.GetComponentsInChildren<TacticalDevice>())
        {
            td.tacSwitch = _active;
        }
            
        if (_saveData != null)
            _saveData.Value = _active;
    }
}