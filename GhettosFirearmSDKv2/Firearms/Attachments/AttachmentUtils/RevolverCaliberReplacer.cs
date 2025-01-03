﻿using System.Linq;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class RevolverCaliberReplacer : MonoBehaviour
    {
        public Attachment attachment;
        private string[] _originalCalibers;
        public string[] newCalibers;
        private ItemSaveData _originalAmmoItem;
        public string newAmmoItem;
        private ItemSaveData _newAmmoItem;

        private BoltBase _bolt;
        
        private void Awake()
        {
            if (attachment.initialized)
                Attachment_OnDelayedAttachEvent();
            else
                attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;

            _newAmmoItem = new ItemSaveData() { ItemID = newAmmoItem };
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            attachment.OnDelayedAttachEvent -= Attachment_OnDelayedAttachEvent;
            
            _bolt = attachment.attachmentPoint == null ? null : attachment.attachmentPoint.parentFirearm != null ? attachment.attachmentPoint.parentFirearm.bolt : null;
            if (_bolt == null)
                return;
            
            if (_bolt.GetType() == typeof(Revolver))
                ApplyRevolver();
            else if (_bolt.GetType() == typeof(GateLoadedRevolver))
                ApplyGateLoaded();
            else
                return;
                
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            attachment.OnDetachEvent -= Attachment_OnDetachEvent;
            
            if (_bolt.GetType() == typeof(Revolver))
                RevertRevolver();
            else if (_bolt.GetType() == typeof(GateLoadedRevolver))
                RevertGateLoaded();
        }

        public void ApplyRevolver()
        {
            if (newCalibers != null && newCalibers.Length > 0)
            {
                _originalCalibers = ((Revolver)attachment.attachmentPoint.parentFirearm.bolt).calibers.ToArray();
                ((Revolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = newCalibers.ToList();
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem) && !attachment.addedByInitialSetup)
            {
                _originalAmmoItem = attachment.attachmentPoint.parentFirearm.GetAmmoItem();
                attachment.attachmentPoint.parentFirearm.SetSavedAmmoItem(_newAmmoItem);
            }
        }

        public void ApplyGateLoaded()
        {
            if (newCalibers != null && newCalibers.Length > 0)
            {
                _originalCalibers = ((GateLoadedRevolver)attachment.attachmentPoint.parentFirearm.bolt).calibers.ToArray();
                ((GateLoadedRevolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = newCalibers;
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem) && !attachment.addedByInitialSetup)
            {
                _originalAmmoItem = attachment.attachmentPoint.parentFirearm.GetAmmoItem();
                attachment.attachmentPoint.parentFirearm.SetSavedAmmoItem(_newAmmoItem);
            }
        }

        public void RevertRevolver()
        {
            if (newCalibers != null && newCalibers.Length > 0)
            {
                ((Revolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = _originalCalibers.ToList();
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem) && !attachment.addedByInitialSetup)
            {
                attachment.attachmentPoint.parentFirearm.SetSavedAmmoItem(_originalAmmoItem);
            }
        }

        public void RevertGateLoaded()
        {
            if (newCalibers != null && newCalibers.Length > 0)
            {
                ((GateLoadedRevolver)attachment.attachmentPoint.parentFirearm.bolt).calibers = _originalCalibers;
            }

            if (!string.IsNullOrWhiteSpace(newAmmoItem) && !attachment.addedByInitialSetup)
            {
                attachment.attachmentPoint.parentFirearm.SetSavedAmmoItem(_originalAmmoItem);
            }
        }
    }
}