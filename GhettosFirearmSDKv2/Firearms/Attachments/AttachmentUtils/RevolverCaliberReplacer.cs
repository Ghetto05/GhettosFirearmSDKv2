using System.Linq;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class RevolverCaliberReplacer : MonoBehaviour
    {
        public Attachment attachment;
        private string[] originalCalibers;
        public string[] newCalibers;
        private string originalAmmoItem;
        public string newAmmoItem;
        
        private void Awake()
        {
            if (attachment.initialized)
                Attachment_OnDelayedAttachEvent();
            else
                attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
            
            if (newCalibers != null && newCalibers.Length > 0)
            {
                originalCalibers = ((Revolver)attachment.attachmentPoint.parentFirearm.bolt).calibers.ToArray();
                ((Revolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = newCalibers.ToList();
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem))
            {
                originalAmmoItem = attachment.attachmentPoint.parentFirearm.defaultAmmoItem;
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = newAmmoItem;
            }

            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;
            
            if (newCalibers != null && newCalibers.Length > 0)
            {
                ((Revolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = originalCalibers.ToList();
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem))
            {
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = originalAmmoItem;
            }
        }
    }
}