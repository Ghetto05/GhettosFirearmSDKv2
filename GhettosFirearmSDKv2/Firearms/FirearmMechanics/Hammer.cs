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
        SaveNodeValueBool hammerState;

        private void Start()
        {
            Invoke("InvokedStart", FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            if (firearm == null && item.gameObject.TryGetComponent(out Firearm f)) firearm = f;
            firearm.OnCockActionEvent += Firearm_OnCockActionEvent;
            hammerState = firearm.saveData.firearmNode.GetOrAddValue("HammerState", new SaveNodeValueBool());
            if (hammerState.value) Cock(true);
            else Fire(true);
        }

        private void Firearm_OnCockActionEvent()
        {
            if (cocked) Fire(true);
            else Cock();
        }

        public void Cock(bool silent = false)
        {
            if (cocked) return;
            hammerState.value = true;
            cocked = true;
            if (hammer != null)
            {
                hammer.localPosition = cockedPosition.localPosition;
                hammer.localEulerAngles = cockedPosition.localEulerAngles;
            }
            if (!silent) Util.PlayRandomAudioSource(cockSounds);
        }

        public void Fire(bool silent = false)
        {
            if (!cocked) return;
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
