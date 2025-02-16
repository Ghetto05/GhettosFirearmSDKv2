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

        private FirearmBase _firearm;

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
                _firearm.magazineWell.loadingCollider = oldCollider;
            }
            if (newMount != null)
            {
                _firearm.magazineWell.mountPoint = oldMount;
            }
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            if (attachment.attachmentPoint.ConnectedManager is not FirearmBase f)
                return;
            _firearm = f;
            
            if (newCollider != null)
            {
                oldCollider = _firearm.magazineWell.loadingCollider;
                if (oldCollider != null)
                    oldCollider.enabled = false;
                _firearm.magazineWell.loadingCollider = newCollider;
            }
            if (newMount != null)
            {
                oldMount = _firearm.magazineWell.mountPoint;
                _firearm.magazineWell.mountPoint = newMount;
            }
        }
    }
}
