using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Magazines/Magazine well collider switcher")]
    public class MagazineWellColliderReplacer : MonoBehaviour
    {
        public Attachment attachment;
        public Collider newCollider;
        public Transform newMount;
        [HideInInspector]
        public Collider oldCollider;
        [HideInInspector]
        public Transform oldMount;

        private void Awake()
        {
            if (attachment.initialized) Attachment_OnDelayedAttachEvent();
            else attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            if (despawnDetach)
                return;
            if (newCollider != null)
            {
                if (oldCollider != null)
                    oldCollider.enabled = true;
                attachment.attachmentPoint.parentFirearm.magazineWell.loadingCollider = oldCollider;
            }
            if (newMount != null)
            {
                attachment.attachmentPoint.parentFirearm.magazineWell.mountPoint = oldMount;
            }
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            if (newCollider != null)
            {
                oldCollider = attachment.attachmentPoint.parentFirearm.magazineWell.loadingCollider;
                if (oldCollider != null)
                    oldCollider.enabled = false;
                attachment.attachmentPoint.parentFirearm.magazineWell.loadingCollider = newCollider;
            }
            if (newMount != null)
            {
                oldMount = attachment.attachmentPoint.parentFirearm.magazineWell.mountPoint;
                attachment.attachmentPoint.parentFirearm.magazineWell.mountPoint = newMount;
            }
        }
    }
}
