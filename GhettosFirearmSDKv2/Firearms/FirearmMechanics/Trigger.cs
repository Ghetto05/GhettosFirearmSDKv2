using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR;
using Valve.VR;

namespace GhettosFirearmSDKv2
{
    public class Trigger : MonoBehaviour
    {
        public FirearmBase firearm;
        public Attachment attachment;

        public Transform trigger;
        public Transform idlePosition;
        public Transform pulledPosition;

        public AudioSource pullSound;
        public AudioSource resetSound;

        public float onTriggerWeight = 0.8f;
        public float lastTriggerPull = 0f;

        public bool triggerEnabled = true;

        void Start()
        {
            if (firearm != null) 
                firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!triggerEnabled)
                return;
            
            if (isPulled && trigger != null && pulledPosition != null && idlePosition != null)
            {
                trigger.localPosition = pulledPosition.localPosition;
                trigger.localRotation = pulledPosition.localRotation;
                if (firearm != null && pullSound != null && firearm.item.holder == null)
                    pullSound.Play();
            }
            else if (trigger != null && pulledPosition != null && idlePosition != null)
            {
                trigger.localPosition = idlePosition.localPosition;
                trigger.localRotation = idlePosition.localRotation;
                if (firearm != null && resetSound != null && firearm.item.holder == null)
                    resetSound.Play();
            }
        }

        private void Update()
        {
            if (firearm == null && attachment?.attachmentPoint?.parentFirearm != null)
            {
                firearm = attachment.attachmentPoint.parentFirearm;
                firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            }
            
            if (!triggerEnabled)
                return;
            
            if (firearm != null && firearm.setUpForHandPose)
            {
                foreach (Handle h in firearm.AllTriggerHandles().Where(h => h != null))
                {
                    if (h.handlers.Count > 0)
                    {
                        float weight;
                        if (PlayerControl.GetHand(h.handlers[0].side).usePressed)
                        {
                            weight = 1f;
                            lastTriggerPull = Time.time;
                        }
                        else if (Time.time - lastTriggerPull <= FirearmsSettings.triggerDisciplineTime) weight = onTriggerWeight;
                        else weight = 0f;

                        h.handlers[0].poser.SetTargetWeight(weight);
                    }
                }
            }
        }
    }
}
