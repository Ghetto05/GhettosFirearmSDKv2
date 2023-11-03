using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class FiremodeSelector : MonoBehaviour
    {
        public FirearmBase firearm;
        public Transform SafetySwitch;
        public Transform SafePosition;
        public Transform SemiPosition;
        public Transform BurstPosition;
        public Transform AutoPosition;
        public Transform AttachmentFirearmPosition;
        public AudioSource switchSound;
        public FirearmBase.FireModes[] firemodes;
        private int currentIndex = 0;
        SaveNodeValueInt fireModeIndex;
        public Hammer hammer;
        public bool allowSwitchingModeIfHammerIsUncocked = true;

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            firearm.OnAltActionEvent += Firearm_OnAltActionEvent;
            firearm.fireMode = firemodes[currentIndex];
            UpdatePosition();

            fireModeIndex = FirearmSaveData.GetNode(firearm).GetOrAddValue("Firemode", new SaveNodeValueInt());
            firearm.SetFiremode(firemodes[fireModeIndex.value]);
            currentIndex = fireModeIndex.value;
            onFiremodeChanged?.Invoke(firearm.fireMode);
            UpdatePosition();
            onFiremodeChanged?.Invoke(firearm.fireMode);
        }

        private void Firearm_OnAltActionEvent(bool longPress)
        {
            if (longPress && (allowSwitchingModeIfHammerIsUncocked || (hammer != null && hammer.cocked)))
            {
                CycleFiremode();
            }
        }

        public void CycleFiremode()
        {
            if (currentIndex + 1 < firemodes.Length) currentIndex++;
            else currentIndex = 0;
            firearm.SetFiremode(firemodes[currentIndex]);
            if (switchSound != null) switchSound.Play();
            fireModeIndex.value = currentIndex;
            UpdatePosition();
            onFiremodeChanged?.Invoke(firearm.fireMode);
        }

        private void UpdatePosition()
        {
            if (SafetySwitch == null)
            {
                return;
            }
            FirearmBase.FireModes mode = firearm.fireMode;
            if (mode == FirearmBase.FireModes.Safe && SafePosition != null)
            {
                SafetySwitch.position = SafePosition.position;
                SafetySwitch.rotation = SafePosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.Semi && SemiPosition != null)
            {
                SafetySwitch.position = SemiPosition.position;
                SafetySwitch.rotation = SemiPosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.Burst && BurstPosition != null)
            {
                SafetySwitch.position = BurstPosition.position;
                SafetySwitch.rotation = BurstPosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.Auto && AutoPosition != null)
            {
                SafetySwitch.position = AutoPosition.position;
                SafetySwitch.rotation = AutoPosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.AttachmentFirearm && AttachmentFirearmPosition != null)
            {
                SafetySwitch.position = AttachmentFirearmPosition.position;
                SafetySwitch.rotation = AttachmentFirearmPosition.rotation;
            }
        }

        public delegate void OnModeChangedDelegate(FirearmBase.FireModes newMode);
        public event OnModeChangedDelegate onFiremodeChanged;
    }
}
