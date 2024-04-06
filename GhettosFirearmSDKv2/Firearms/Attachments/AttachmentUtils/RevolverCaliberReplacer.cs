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

        private BoltBase bolt;
        
        private void Awake()
        {
            if (attachment.initialized)
                Attachment_OnDelayedAttachEvent();
            else
                attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
            
            bolt = attachment.attachmentPoint == null ? null : attachment.attachmentPoint.parentFirearm != null ? attachment.attachmentPoint.parentFirearm.bolt : null;
            if (bolt == null)
                return;
            
            if (bolt.GetType() == typeof(Revolver))
                ApplyRevolver();
            else if (bolt.GetType() == typeof(GateLoadedRevolver))
                ApplyGateLoaded();
            else
                return;
                
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;
            
            if (bolt.GetType() == typeof(Revolver))
                RevertRevolver();
            else if (bolt.GetType() == typeof(GateLoadedRevolver))
                RevertGateLoaded();
        }

        public void ApplyRevolver()
        {
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
        }

        public void ApplyGateLoaded()
        {
            if (newCalibers != null && newCalibers.Length > 0)
            {
                originalCalibers = ((GateLoadedRevolver)attachment.attachmentPoint.parentFirearm.bolt).calibers.ToArray();
                ((GateLoadedRevolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = newCalibers;
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem))
            {
                originalAmmoItem = attachment.attachmentPoint.parentFirearm.defaultAmmoItem;
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = newAmmoItem;
            }
        }

        public void RevertRevolver()
        {
            if (newCalibers != null && newCalibers.Length > 0)
            {
                ((Revolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = originalCalibers.ToList();
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem))
            {
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = originalAmmoItem;
            }
        }

        public void RevertGateLoaded()
        {
            if (newCalibers != null && newCalibers.Length > 0)
            {
                ((GateLoadedRevolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = originalCalibers;
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem))
            {
                attachment.attachmentPoint.parentFirearm.defaultAmmoItem = originalAmmoItem;
            }
        }
    }
}