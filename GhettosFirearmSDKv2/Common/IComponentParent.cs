using System;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Common;

public interface IComponentParent
{
    FirearmSaveData.AttachmentTreeNode SaveNode { get; }

    GameObject GameObject { get; }

    void GetInitialization(Action<IAttachmentManager, IComponentParent> initializationCallback);

    #region Held actions

    public delegate void HeldAction(HeldActionData e);

    public event HeldAction OnHeldAction;
    public event HeldAction OnUnhandledHeldAction;

    public class HeldActionData
    {
        public HeldActionData(RagdollHand handler, Handle handle, Interactable.Action action)
        {
            Handler = handler;
            Handle = handle;
            Action = action;
        }

        public RagdollHand Handler;
        public Handle Handle;
        public Interactable.Action Action;
        public bool Handled;

        public override string ToString()
        {
            return $"Action: {Action} Handle: {Handle.name}";
        }
    }

    #endregion
}