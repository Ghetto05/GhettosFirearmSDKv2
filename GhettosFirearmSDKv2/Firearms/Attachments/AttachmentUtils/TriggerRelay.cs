using ThunderRoad;
using UnityEngine;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    public class TriggerRelay : MonoBehaviour
    {
        public FirearmBase source;
        public FirearmBase target;
        public AttachmentPoint targetPoint;
        public bool onlyFireWithSpecficFiremode;
        public FirearmBase.FireModes firemode;

        private void Start()
        {
            source.item.OnHeldActionEvent += Item_OnHeldActionEvent;
            if (target != null) target.additionalTriggerHandles.Add(source.mainFireHandle);
            if (targetPoint != null) targetPoint.OnAttachmentAddedEvent += TargetPoint_OnAttachmentAddedEvent;
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (target != null && (!onlyFireWithSpecficFiremode || source.fireMode == firemode)) target.Item_OnHeldActionEvent(ragdollHand, handle, action);
        }

        private void TargetPoint_OnAttachmentAddedEvent(Attachment attachment)
        {
            if (attachment.GetComponent<AttachmentFirearm>() is { } targetFire)
            {
                target = targetFire;
                if (target != null)
                {
                    targetFire.additionalTriggerHandles.Add(source.mainFireHandle);
                    targetFire.fireHandle?.SetTouch(false);
                    targetFire.fireHandle?.SetTelekinesis(false);
                }
            }
        }
    }
}
