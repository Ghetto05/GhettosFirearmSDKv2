using System;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class FiremodeSelector : MonoBehaviour
    {
        public FirearmBase firearm;
        public Attachment attachment;
        public Transform safetySwitch;
        public Transform safePosition;
        public Transform semiPosition;
        public Transform burstPosition;
        public Transform autoPosition;
        public Transform attachmentFirearmPosition;
        public AudioSource switchSound;
        public FirearmBase.FireModes[] firemodes;
        public float[] fireRates;
        public Transform[] irregularPositions;
        public int currentIndex;
        private SaveNodeValueInt _fireModeIndex;
        public Hammer hammer;
        public bool allowSwitchingModeIfHammerIsUncocked = true;
        public bool onlyAllowSwitchingIfBoltHasState;
        public BoltBase.BoltState switchAllowedState;

        private FirearmBase.FireModes _preAttachFireMode;

        private void OnDestroy()
        {
            firearm.OnAltActionEvent -= Firearm_OnAltActionEvent;
            if (attachment)
                attachment.OnDetachEvent -= AttachmentOnOnDetachEvent;
        }

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            if (!firearm && attachment)
            {
                firearm = attachment.attachmentPoint.parentFirearm;
                attachment.OnDetachEvent += AttachmentOnOnDetachEvent;
                _preAttachFireMode = firearm.fireMode;
            }

            firearm.OnAltActionEvent += Firearm_OnAltActionEvent;
            firearm.fireMode = firemodes[currentIndex];
            UpdatePosition();

            _fireModeIndex = FirearmSaveData.GetNode(firearm).GetOrAddValue("Firemode", new SaveNodeValueInt());
            firearm.SetFiremode(firemodes[_fireModeIndex.Value]);
            currentIndex = _fireModeIndex.Value;
            UpdatePosition();
            OnFiremodeChanged?.Invoke(firearm.fireMode);
        }

        private void AttachmentOnOnDetachEvent(bool despawndetach)
        {
            firearm.fireMode = _preAttachFireMode;
        }

        private void Firearm_OnAltActionEvent(bool longPress)
        {
            if (longPress && (allowSwitchingModeIfHammerIsUncocked || (hammer != null && hammer.cocked) && (!onlyAllowSwitchingIfBoltHasState || firearm.bolt == null || firearm.bolt.state == switchAllowedState)))
            {
                CycleFiremode();
            }
        }

        public void CycleFiremode()
        {
            if (currentIndex + 1 < firemodes.Length) currentIndex++;
            else currentIndex = 0;
            firearm.SetFiremode(firemodes[currentIndex]);
            if (fireRates != null && fireRates.Length > currentIndex)
                firearm.roundsPerMinute = fireRates[currentIndex];
            if (switchSound != null)
                switchSound.Play();
            if (irregularPositions != null && irregularPositions.Length > currentIndex)
                safetySwitch.SetPositionAndRotation(irregularPositions[currentIndex].position, irregularPositions[currentIndex].rotation);
            else
                UpdatePosition();
            _fireModeIndex.Value = currentIndex;
            OnFiremodeChanged?.Invoke(firearm.fireMode);
        }

        private void UpdatePosition()
        {
            if (safetySwitch == null)
            {
                return;
            }
            var mode = firearm.fireMode;
            if (mode == FirearmBase.FireModes.Safe && safePosition != null)
            {
                safetySwitch.position = safePosition.position;
                safetySwitch.rotation = safePosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.Semi && semiPosition != null)
            {
                safetySwitch.position = semiPosition.position;
                safetySwitch.rotation = semiPosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.Burst && burstPosition != null)
            {
                safetySwitch.position = burstPosition.position;
                safetySwitch.rotation = burstPosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.Auto && autoPosition != null)
            {
                safetySwitch.position = autoPosition.position;
                safetySwitch.rotation = autoPosition.rotation;
            }
            else if (mode == FirearmBase.FireModes.AttachmentFirearm && attachmentFirearmPosition != null)
            {
                safetySwitch.position = attachmentFirearmPosition.position;
                safetySwitch.rotation = attachmentFirearmPosition.rotation;
            }
        }

        public delegate void OnModeChangedDelegate(FirearmBase.FireModes newMode);
        public event OnModeChangedDelegate OnFiremodeChanged;
    }
}
