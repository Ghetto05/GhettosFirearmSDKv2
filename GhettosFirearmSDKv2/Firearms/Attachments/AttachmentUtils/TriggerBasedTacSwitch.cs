using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class TriggerBasedTacSwitch : ThunderBehaviour
    {
        public enum Mode
        {
            FullPull,
            PartialPull
        }

        private const float PartialPullPoint = 0.2f;
        
        public Firearm firearm;
        public Mode mode;

        private bool _active;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            if (mode == Mode.FullPull)
            {
                firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;
            }
            
            Invoke(nameof(InitialSet), 1f);
        }

        protected override void ManagedUpdate()
        {
            if (mode == Mode.PartialPull)
            {
                var currentPull = firearm.item.mainHandleRight?.handlers.FirstOrDefault()?.playerHand?.controlHand.useAxis ?? 0f;
                if (currentPull >= PartialPullPoint && !_active)
                {
                    Toggle(true);
                }
                else if (currentPull < PartialPullPoint && _active)
                {
                    Toggle(false);
                }
            }
        }

        private void FirearmOnOnTriggerChangeEvent(bool isPulled)
        {
            Toggle(isPulled);
        }

        public void InitialSet()
        {
            Toggle(false);
        }

        public void Toggle(bool active)
        {
            _active = active;
            foreach (var td in firearm.GetComponentsInChildren<TacticalDevice>())
            {
                td.tacSwitch = _active;
            }
        }

        private void OnDestroy()
        {
            if (mode == Mode.FullPull)
            {
                firearm.OnTriggerChangeEvent -= FirearmOnOnTriggerChangeEvent;
            }

            Toggle(true);
        }
    }
}
