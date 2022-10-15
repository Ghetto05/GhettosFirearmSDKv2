using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Trigger : MonoBehaviour
    {
        public FirearmBase firearm;

        public Transform trigger;
        public Transform idlePosition;
        public Transform pulledPosition;

        public AudioSource pullSound;
        public AudioSource resetSound;

        void Awake()
        {
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (isPulled && trigger != null && pulledPosition != null && idlePosition != null)
            {
                trigger.localPosition = pulledPosition.localPosition;
                trigger.localRotation = pulledPosition.localRotation;
                if (pullSound != null) pullSound.Play();
            }
            else if (trigger != null && pulledPosition != null && idlePosition != null)
            {
                trigger.localPosition = idlePosition.localPosition;
                trigger.localRotation = idlePosition.localRotation;
                if (resetSound != null) resetSound.Play();
            }
        }
    }
}
