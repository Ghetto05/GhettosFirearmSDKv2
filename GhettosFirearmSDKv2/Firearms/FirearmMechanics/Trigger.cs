using System.Linq;
using ThunderRoad;
using UnityEngine;

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
        public float lastTriggerPull;

        public bool triggerEnabled = true;

        [Space]
        public bool fireModeSelectionMode;
        public float secondModePullWeight = 0.9f;
        public Transform secondModePulledPosition;
        public FirearmBase.FireModes firstMode;
        public FirearmBase.FireModes secondMode;
        private bool onSecondMode;
        [Space]
        public FiremodeSelector selector;
        public int[] allowedIndexesForSecondMode;

        private const float SecondModeTriggerPull = 0.9f;

        void Start()
        {
            if (firearm != null) 
                firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (!triggerEnabled)
                return;
            
            if (isPulled)
            {
                if (firearm != null && pullSound != null && firearm.item.holder == null)
                    pullSound.Play();
            }
            else
            {
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

            float highestPull = 0f;
            if (firearm != null)
            {
                foreach (Handle h in firearm.AllTriggerHandles().Where(h => h != null))
                {
                    if (h.handlers.Count > 0)
                    {
                        if (PlayerControl.GetHand(h.handlers[0].side).useAxis > highestPull)
                            highestPull = PlayerControl.GetHand(h.handlers[0].side).useAxis;
                        
                        float weight;
                        if (PlayerControl.GetHand(h.handlers[0].side).usePressed)
                        {
                            weight = onSecondMode ? secondModePullWeight : 1f;
                            lastTriggerPull = Time.time;
                        }
                        else if (Time.time - lastTriggerPull <= Settings.triggerDisciplineTime)
                            weight = onTriggerWeight;
                        else
                            weight = 0f;

                        if (firearm.setUpForHandPose)
                            h.handlers[0].poser.SetTargetWeight(weight);
                    }
                }
            }
            
            UpdateTriggerPosition(highestPull);
        }

        private void UpdateTriggerPosition(float pull)
        {
            Transform target = GetTarget(pull);
            
            if (trigger == null || pulledPosition == null || idlePosition == null)
                return;
            
            trigger.SetPositionAndRotation(target.position, target.rotation);
        }

        private Transform GetTarget(float pull)
        {
            if (fireModeSelectionMode && (selector == null || allowedIndexesForSecondMode.Contains(selector.currentIndex)))
            {
                float actual = pull + Settings.progressiveTriggerDeadZone;
                if (actual > SecondModeTriggerPull)
                {
                    onSecondMode = true;
                    firearm.fireMode = secondMode;
                    return secondModePulledPosition;
                }

                firearm.fireMode = firstMode;
                onSecondMode = false;
                return firearm.triggerState ? pulledPosition : idlePosition;
            }
            else
            {
                return firearm.triggerState ? pulledPosition : idlePosition;
            }
        }
    }
}
