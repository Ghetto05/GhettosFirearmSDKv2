using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Magazines/Magazine well collider switcher")]
    public class MagazineWellColliderReplacer : MonoBehaviour
    {
        public Attachment attachment;
        public Collider newCollider;
        [HideInInspector]
        public Collider oldCollider;

        private void Awake()
        {
            attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            oldCollider.enabled = true;
            attachment.attachmentPoint.parentFirearm.magazineWell.loadingCollider = oldCollider;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            oldCollider = attachment.attachmentPoint.parentFirearm.magazineWell.loadingCollider;
            oldCollider.enabled = false;
            attachment.attachmentPoint.parentFirearm.magazineWell.loadingCollider = newCollider;
        }
    }
}
