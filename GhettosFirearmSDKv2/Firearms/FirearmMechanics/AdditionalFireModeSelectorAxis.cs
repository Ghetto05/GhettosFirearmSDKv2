using System;
using System.Collections;
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

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        private void InvokedStart()
        {
            selector.onFiremodeChanged += SelectorOnFireModeChanged;
        }

        private void SelectorOnFireModeChanged(FirearmBase.FireModes newMode)
        {
            var pos = positions[selector.currentIndex];
            axis.SetPositionAndRotation(pos.position, pos.rotation);
        }
    }
}
