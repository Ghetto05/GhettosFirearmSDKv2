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

        private void Awake()
        {
            source.item.OnHeldActionEvent += Item_OnHeldActionEvent;
            if (target != null) target.additionalTriggerHandles.Add(source.mainFireHandle);
            if (targetPoint != null) targetPoint.OnAttachmentAddedEvent += TargetPoint_OnAttachmentAddedEvent;
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (target != null && (!onlyFireWithSpecficFiremode || source.fireMode == firemode)) target.Item_OnHeldActionEvent(ragdollHand, handle, action);
        }

        private IEnumerator Delayed(AttachmentFirearm targetFire)
        {
            yield return new WaitForSeconds(0.05f);
            if (target != null)
            {
                targetFire.additionalTriggerHandles.Add(source.mainFireHandle);
                targetFire.fireHandle.SetTouch(false);
                targetFire.fireHandle.SetTelekinesis(false);
            }
        }

        private void TargetPoint_OnAttachmentAddedEvent(Attachment attachment)
        {
            if (attachment.GetComponent<AttachmentFirearm>() is AttachmentFirearm targetFire)
            {
                target = targetFire;
                StartCoroutine(Delayed(targetFire));
            }
        }
    }
}
