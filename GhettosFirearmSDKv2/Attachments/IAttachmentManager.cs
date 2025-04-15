using System.Collections.Generic;
using GhettosFirearmSDKv2.Common;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Attachments;

public interface IAttachmentManager : IComponentParent
{
    /// <summary>
    ///     Gets the item
    /// </summary>
    Item Item
    {
        get;
    }

    /// <summary>
    ///     Gets the transform
    /// </summary>
    Transform Transform
    {
        get;
    }

    /// <summary>
    ///     The attachment save data
    /// </summary>
    FirearmSaveData SaveData
    {
        get;
        set;
    }

    /// <summary>
    ///     A list of all attachment points
    /// </summary>
    List<AttachmentPoint> AttachmentPoints
    {
        get;
        set;
    }

    /// <summary>
    ///     A list of all attachments currently attached
    /// </summary>
    List<Attachment> CurrentAttachments
    {
        get;
        set;
    }

    /// <summary>
    ///     Locates a slot on the base manager by the ID it was given
    /// </summary>
    /// <param name="id">The ID which the slot should be located by</param>
    /// <returns>The located slot</returns>
    AttachmentPoint GetSlotFromId(string id);

    /// <summary>
    ///     Refreshes the list of all currently attached attachments
    /// </summary>
    void UpdateAttachments();

    void InvokeAttachmentAdded(Attachment attachment, AttachmentPoint attachmentPoint);

    void InvokeAttachmentRemoved(Attachment attachment, AttachmentPoint attachmentPoint);

    public delegate void Collision(UnityEngine.Collision collision);

    public event Collision OnCollision;

    public delegate void AttachmentAdded(Attachment attachment, AttachmentPoint attachmentPoint);

    public event AttachmentAdded OnAttachmentAdded;

    public delegate void AttachmentRemoved(Attachment attachment, AttachmentPoint attachmentPoint);

    public event AttachmentRemoved OnAttachmentRemoved;
}