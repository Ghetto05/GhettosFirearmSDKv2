using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class FiremodeSelector : MonoBehaviour
    {
        public Firearm firearm;
        public Transform SafetySwitch;
        public Transform SafePosition;
        public Transform SemiPosition;
        public Transform BurstPosition;
        public Transform AutoPosition;
        public AudioSource switchSound;
        public Firearm.FireModes[] firemodes;
        private int currentIndex = 0;

        private void Awake()
        {
            firearm.OnAltActionEvent += Firearm_OnAltActionEvent;
            firearm.fireMode = firemodes[currentIndex];
            StartCoroutine(delayedLoad());
            UpdatePosition();
        }

        private void SaveFiremode()
        {
            firearm.item.RemoveCustomData<FiremodeSaveData>();
            FiremodeSaveData data = new FiremodeSaveData();
            data.fireMode = firearm.fireMode;
            data.index = currentIndex;
            firearm.item.AddCustomData(data);
        }

        private IEnumerator delayedLoad()
        {
            yield return new WaitForSeconds(0.05f);
            if (firearm.item.TryGetCustomData(out FiremodeSaveData data))
            {
                firearm.SetFiremode(data.fireMode);
                currentIndex = data.index;
                onFiremodeChanged?.Invoke(data.fireMode);
                UpdatePosition();
            }
            onFiremodeChanged?.Invoke(firearm.fireMode);
        }

        private void Firearm_OnAltActionEvent(bool longPress)
        {
            if (longPress)
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
            onFiremodeChanged?.Invoke(firearm.fireMode);
            SaveFiremode();
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (SafetySwitch == null) return;
            Firearm.FireModes mode = firearm.fireMode;
            if (mode == Firearm.FireModes.Safe && SafePosition != null)
            {
                SafetySwitch.position = SafePosition.position;
                SafetySwitch.rotation = SafePosition.rotation;
            }
            else if (mode == Firearm.FireModes.Semi && SemiPosition != null)
            {
                SafetySwitch.position = SemiPosition.position;
                SafetySwitch.rotation = SemiPosition.rotation;
            }
            else if (mode == Firearm.FireModes.Burst && BurstPosition != null)
            {
                SafetySwitch.position = BurstPosition.position;
                SafetySwitch.rotation = BurstPosition.rotation;
            }
            else if (mode == Firearm.FireModes.Auto && AutoPosition != null)
            {
                SafetySwitch.position = AutoPosition.position;
                SafetySwitch.rotation = AutoPosition.rotation;
            }
        }

        public delegate void OnModeChangedDelegate(Firearm.FireModes newMode);
        public event OnModeChangedDelegate onFiremodeChanged;
    }
}
