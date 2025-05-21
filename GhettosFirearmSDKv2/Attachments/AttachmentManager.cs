using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Attachments;

public class AttachmentManager : MonoBehaviour, IAttachmentManager
{
    public Item Item
    {
        get
        {
            return item;
        }
    }

    public Transform Transform
    {
        get
        {
            return transform;
        }
    }

    public FirearmSaveData SaveData
    {
        get;
        set;
    }

    public List<AttachmentPoint> AttachmentPoints
    {
        get
        {
            return attachmentPoints;
        }
        set
        {
            attachmentPoints = value;
        }
    }

    public List<Attachment> CurrentAttachments
    {
        get;
        set;
    }

    public Item item;
    public List<AttachmentPoint> attachmentPoints;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
        item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
        item.OnDespawnEvent += OnDespawnEvent;
    }

    private void OnDespawnEvent(EventTime eventTime)
    {
        if (eventTime != EventTime.OnStart)
        {
            return;
        }
        item.OnHeldActionEvent -= ItemOnOnHeldActionEvent;
        item.OnDespawnEvent -= OnDespawnEvent;
        OnTeardown?.Invoke();
    }

    private void OnCollisionEnter(Collision other)
    {
        OnCollision?.Invoke(other);
    }

    private void ItemOnOnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        var e = new IAttachmentManager.HeldActionData(ragdollHand, handle, action);
        OnHeldAction?.Invoke(e);
        if (!e.Handled)
        {
            OnUnhandledHeldAction?.Invoke(e);
        }
    }

    private void InvokedStart()
    {
        SharedAttachmentManagerFunctions.LoadAndApplyData(this);
    }

    public AttachmentPoint GetSlotFromId(string id)
    {
        return SharedAttachmentManagerFunctions.GetSlotFromId(this, id);
    }

    public void UpdateAttachments()
    {
        SharedAttachmentManagerFunctions.UpdateAttachments(this);
    }

    public void InvokeAttachmentAdded(Attachment attachment, AttachmentPoint attachmentPoint)
    {
        OnAttachmentAdded?.Invoke(attachment, attachmentPoint);
    }

    public void InvokeAttachmentRemoved(Attachment attachment, AttachmentPoint attachmentPoint)
    {
        OnAttachmentRemoved?.Invoke(attachment, attachmentPoint);
    }

    public event IInteractionProvider.Teardown OnTeardown;
    public event IAttachmentManager.Collision OnCollision;
    public event IAttachmentManager.AttachmentAdded OnAttachmentAdded;
    public event IAttachmentManager.AttachmentRemoved OnAttachmentRemoved;
    public event IAttachmentManager.HeldAction OnHeldAction;
    public event IAttachmentManager.HeldAction OnUnhandledHeldAction;
}