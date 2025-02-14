using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AdditionalFireModeSelectorAxis : MonoBehaviour
    {
        public FiremodeSelector selector;
        public Transform axis;
        public List<Transform> positions;

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
            selector.OnFiremodeChanged += SelectorOnFireModeChanged;
        }

        private void SelectorOnFireModeChanged(FirearmBase.FireModes newMode)
        {
            Transform pos = null;
            
            if (positions.Any())
                pos = positions[selector.currentIndex];
            
            switch (newMode)
            {
                case FirearmBase.FireModes.Safe:
                    pos = safePosition ?? pos;
                    break;
                case FirearmBase.FireModes.Semi:
                    pos = semiPosition ?? pos;
                    break;
                case FirearmBase.FireModes.Burst:
                    pos = burstPosition ?? pos;
                    break;
                case FirearmBase.FireModes.Auto:
                    pos = autoPosition ?? pos;
                    break;
                case FirearmBase.FireModes.AttachmentFirearm:
                    pos = attachmentFirearmPosition ?? pos;
                    break;
            }

            if (!pos)
                return;
            
            axis.SetPositionAndRotation(pos.position, pos.rotation);
        }
    }
}
