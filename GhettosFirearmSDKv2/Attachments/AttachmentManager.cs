﻿using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Attachments;

public class AttachmentManager : MonoBehaviour, IAttachmentManager
{
    public Item Item => item;
    public Transform Transform => transform;
    public FirearmSaveData SaveData { get; set; }

    public List<AttachmentPoint> AttachmentPoints
    {
        get => attachmentPoints;
        set => attachmentPoints = value;
    }
    public List<Attachment> CurrentAttachments { get; set; }

    public Item item;
    public List<AttachmentPoint> attachmentPoints;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
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

    public event IAttachmentManager.Collision OnCollision;
    public event IAttachmentManager.AttachmentAdded OnAttachmentAdded;
    public event IAttachmentManager.AttachmentRemoved OnAttachmentRemoved;
}