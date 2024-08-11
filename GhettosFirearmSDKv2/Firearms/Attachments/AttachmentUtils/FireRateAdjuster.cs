using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class FireRateAdjuster : MonoBehaviour
    {
        public Attachment attachment;
        public float newFireRate;
        private float _oldFireRate;

        private void Awake()
        {
            if (attachment.initialized)
                Attachment_OnDelayedAttachEvent();
            else
                attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            attachment.attachmentPoint.parentFirearm.roundsPerMinute = _oldFireRate;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            _oldFireRate = attachment.attachmentPoint.parentFirearm.roundsPerMinute;
            attachment.attachmentPoint.parentFirearm.roundsPerMinute = newFireRate;
        }
    }
}
