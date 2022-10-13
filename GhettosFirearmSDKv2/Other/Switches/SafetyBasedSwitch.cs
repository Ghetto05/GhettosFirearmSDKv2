using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class SafetyBasedSwitch : MonoBehaviour
    {
        public FiremodeSelector selector;
        public UnityEvent onSafe;
        public UnityEvent onSemi;
        public UnityEvent onBurst;
        public UnityEvent onAuto;

        private void Awake()
        {
            selector.onFiremodeChanged += Selector_onFiremodeChanged;
        }

        private void Selector_onFiremodeChanged(Firearm.FireModes newMode)
        {
            if (newMode == Firearm.FireModes.Safe)
            {
                onSafe?.Invoke();
            }
            else if (newMode == Firearm.FireModes.Semi)
            {
                onSemi?.Invoke();
            }
            else if (newMode == Firearm.FireModes.Burst)
            {
                onBurst?.Invoke();
            }
            else if (newMode == Firearm.FireModes.Auto)
            {
                onAuto?.Invoke();
            }
        }
    }
}
