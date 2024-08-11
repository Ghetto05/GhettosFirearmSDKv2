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

        private void Start()
        {
            selector.OnFiremodeChanged += Selector_onFiremodeChanged;
            Util.DelayedExecute(1f, Init, this);
        }

        private void Init()
        {
            Selector_onFiremodeChanged(selector.firearm.fireMode);
        }

        private void Selector_onFiremodeChanged(FirearmBase.FireModes newMode)
        {
            if (newMode == FirearmBase.FireModes.Safe)
            {
                onSafe?.Invoke();
            }
            else if (newMode == FirearmBase.FireModes.Semi)
            {
                onSemi?.Invoke();
            }
            else if (newMode == FirearmBase.FireModes.Burst)
            {
                onBurst?.Invoke();
            }
            else if (newMode == FirearmBase.FireModes.Auto)
            {
                onAuto?.Invoke();
            }
        }
    }
}
