using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    public class Hammer : MonoBehaviour
    {
        public Item item;
        public Firearm firearm;
        public Transform hammer;
        public Transform idlePosition;
        public Transform cockedPosition;
        public List<AudioSource> hitSounds;
        public List<AudioSource> cockSounds;
        public bool cocked;
        public bool hasDecocker = false;
        public bool allowManualCock = false;
        public bool allowCockUncockWhenSafetyIsOn = true;
        SaveNodeValueBool hammerState;

        private void Start()
        {
            Invoke("InvokedStart", FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            if (firearm == null && item.gameObject.TryGetComponent(out Firearm f)) firearm = f;
            firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;
            firearm.OnCockActionEvent += Firearm_OnCockActionEvent;
            hammerState = firearm.saveData.firearmNode.GetOrAddValue("HammerState", new SaveNodeValueBool());
            if (hammerState.value) Cock(true, true);
            else Fire(true, true);
        }

        private void Firearm_OnFiremodeChangedEvent()
        {
            if (hasDecocker && firearm.fireMode == FirearmBase.FireModes.Safe) Fire();
        }

        private void Firearm_OnCockActionEvent()
        {
            if (!allowManualCock || (!allowCockUncockWhenSafetyIsOn && firearm.fireMode == FirearmBase.FireModes.Safe)) return;
            if (cocked) Fire(true);
            else Cock();
        }

        public void Cock(bool silent = false, bool forced = false)
        {
            if (cocked && !forced) return;
            hammerState.value = true;
            cocked = true;
            if (hammer != null)
            {
                hammer.localPosition = cockedPosition.localPosition;
                hammer.localEulerAngles = cockedPosition.localEulerAngles;
            }
            if (!silent) Util.PlayRandomAudioSource(cockSounds);
        }

        public void Fire(bool silent = false, bool forced = false)
        {
            if (!cocked && !forced) return;
            hammerState.value = false;
            cocked = false;
            if (hammer != null)
            {
                hammer.localPosition = idlePosition.localPosition;
                hammer.localEulerAngles = idlePosition.localEulerAngles;
            }
            if (!silent) Util.PlayRandomAudioSource(hitSounds);
        }
    }
}
