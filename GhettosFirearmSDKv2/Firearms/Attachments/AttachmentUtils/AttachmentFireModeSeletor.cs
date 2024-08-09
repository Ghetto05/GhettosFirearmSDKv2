using System;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AttachmentFireModeSelector : MonoBehaviour
    {
        private Firearm _connectedFirearm;
        public Attachment attachment;
        public Transform selector;
        public Transform safePosition;
        public Transform semiPosition;
        public Transform burstPosition;
        public Transform autoPosition;
        public Transform attachmentFirearmPosition;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        private void InvokedStart()
        {
            _connectedFirearm = attachment.attachmentPoint.parentFirearm;
            UpdatePosition();
            _connectedFirearm.OnFiremodeChangedEvent += ConnectedFirearmOnOnFiremodeChangedEvent;
        }

        private void ConnectedFirearmOnOnFiremodeChangedEvent()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (selector == null)
                return;

            Transform target;
            
            switch (_connectedFirearm.fireMode)
            {
                case FirearmBase.FireModes.Auto:
                    target = autoPosition;
                    break;
                case FirearmBase.FireModes.Semi:
                    target = semiPosition;
                    break;
                case FirearmBase.FireModes.Burst:
                    target = burstPosition;
                    break;
                case FirearmBase.FireModes.AttachmentFirearm:
                    target = attachmentFirearmPosition;
                    break;
                default:
                    target = safePosition;
                    break;
            }

            if (target == null)
                return;
            
            selector.SetLocalPositionAndRotation(target.localPosition, target.localRotation);
        }
    }
}