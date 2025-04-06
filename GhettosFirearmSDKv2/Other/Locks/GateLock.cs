using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Locks/Gate lock")]
public class GateLock : Lock
{
    public Item item;
    public Attachment attachment;
    public List<Handle> handles;
    public bool useFireHandle;

    public Transform gate;
    public Transform locked;
    public Transform unlocked;

    public List<AudioSource> openSounds;
    public List<AudioSource> closeSounds;

    private bool _isLocked = true;

    public override bool GetIsUnlocked()
    {
        return !_isLocked;
    }

    private void Start()
    {
        if (item)
        {
            if (useFireHandle && item.GetComponent<Firearm>() is { } firearm)
            {
                firearm.OnAltActionEvent += FirearmOnOnAltActionEvent;
            }
            else
            {
                item.OnHeldActionEvent += OnHeldActionEvent;
            }
        }
        else if (attachment)
        {
            attachment.OnHeldActionEvent += OnHeldActionEvent;
            if (useFireHandle && attachment.GetComponent<AttachmentFirearm>() is { } firearm)
            {
                firearm.OnAltActionEvent += FirearmOnOnAltActionEvent;
            }
        }
        Toggle(true);
    }

    private void FirearmOnOnAltActionEvent(bool longpress)
    {
        if (!longpress)
        {
            Toggle();
        }
    }

    private void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (handles.Contains(handle) && action == Interactable.Action.AlternateUseStart)
        {
            Toggle();
        }
    }

    public void Toggle(bool initial = false)
    {
        if (!initial)
        {
            _isLocked = !_isLocked;
        }
        var t = _isLocked ? locked : unlocked;
        gate.SetLocalPositionAndRotation(t.localPosition, t.localRotation);
        if (!initial)
        {
            Util.PlayRandomAudioSource(_isLocked ? openSounds : closeSounds);
        }
    }
}