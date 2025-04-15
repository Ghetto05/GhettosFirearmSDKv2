using System;
using System.Collections.Generic;
using GhettosFirearmSDKv2.Common;
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
    public FirearmSaveData.AttachmentTreeNode SaveNode => SaveData.FirearmNode;

    public GameObject GameObject => gameObject;

    private List<Action<IAttachmentManager, IComponentParent>> _requestedInitializations = [];
    private bool _initialized;
    
    public void GetInitialization(Action<IAttachmentManager, IComponentParent> initializationCallback)
    {
        if (!_initialized)
        {
            _requestedInitializations.Add(initializationCallback);
            return;
        }
        initializationCallback.Invoke(this, this);
    }

    private void Start()
    {
        item.OnSpawnEvent += OnItemSpawn;
        item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
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

    private void OnItemSpawn(EventTime eventTime)
    {
        if (eventTime != EventTime.OnEnd)
        {
            return;
        }
        item.OnSpawnEvent -= OnItemSpawn;

        SharedAttachmentManagerFunctions.LoadAndApplyData(this);
        _initialized = true;
        _requestedInitializations.ForEach(x => x.Invoke(this, this));
        _requestedInitializations = null;
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

    public event IAttachmentManager.Collision OnCollision;
    public event IAttachmentManager.AttachmentAdded OnAttachmentAdded;
    public event IAttachmentManager.AttachmentRemoved OnAttachmentRemoved;

    public event IAttachmentManager.HeldAction OnHeldAction;
    public event IAttachmentManager.HeldAction OnUnhandledHeldAction;
}