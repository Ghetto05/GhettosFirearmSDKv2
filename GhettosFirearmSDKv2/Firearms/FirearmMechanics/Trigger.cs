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
        private bool _onSecondMode;
        [Space]
        public FiremodeSelector selector;
        public int[] allowedIndexesForSecondMode;

        private const float SecondModeTriggerPull = 0.9f;

        private void Start()
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
                if (firearm && pullSound && !firearm.item.holder)
                    pullSound.Play();
            }
            else
            {
                if (firearm && resetSound && !firearm.item.holder)
                    resetSound.Play();
            }
        }

        private void Update()
        {
            if (firearm && attachment && attachment.attachmentPoint?.parentFirearm)
            {
                firearm = attachment.attachmentPoint.parentFirearm;
                firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            }
            
            if (!triggerEnabled)
                return;

            var highestPull = 0f;
            if (firearm)
            {
                foreach (var h in firearm.AllTriggerHandles().Where(h => h))
                {
                    if (h.handlers.Count > 0 && h.handlers[0].poser.hasTargetHandPose)
                    {
                        if (!firearm.HeldByAI())
                        {
                            if (PlayerControl.GetHand(h.handlers[0].side).useAxis > highestPull)
                                highestPull = PlayerControl.GetHand(h.handlers[0].side).useAxis;

                            float weight;
                            if (PlayerControl.GetHand(h.handlers[0].side).usePressed)
                            {
                                weight = _onSecondMode ? secondModePullWeight : 1f;
                                lastTriggerPull = Time.time;
                            }
                            else if (Time.time - lastTriggerPull <= Settings.triggerDisciplineTime)
                                weight = onTriggerWeight;
                            else
                                weight = 0f;

                            h.handlers[0].poser.SetTargetWeight(weight);
                        }
                        else
                        {
                            float weight;
                            if (firearm.triggerState)
                            {
                                weight = _onSecondMode ? secondModePullWeight : 1f;
                                lastTriggerPull = Time.time;
                            }
                            else if (Time.time - lastTriggerPull <= Settings.triggerDisciplineTime)
                                weight = onTriggerWeight;
                            else
                                weight = 0f;
                            
                            h.handlers[0].poser.SetTargetWeight(weight);
                        }
                    }
                }
            }
            
            UpdateTriggerPosition(highestPull);
        }

        private void UpdateTriggerPosition(float pull)
        {
            var target = GetTarget(pull);
            
            if (trigger == null || pulledPosition == null || idlePosition == null)
                return;
            
            trigger.SetPositionAndRotation(target.position, target.rotation);
        }

        private Transform GetTarget(float pull)
        {
            if (fireModeSelectionMode && (selector == null || allowedIndexesForSecondMode.Contains(selector.currentIndex)))
            {
                var actual = pull + Settings.progressiveTriggerDeadZone;
                if (actual > SecondModeTriggerPull)
                {
                    _onSecondMode = true;
                    firearm.fireMode = secondMode;
                    return secondModePulledPosition;
                }

                firearm.fireMode = firstMode;
                _onSecondMode = false;
                return firearm.triggerState ? pulledPosition : idlePosition;
            }

            return firearm.triggerState ? pulledPosition : idlePosition;
        }
    }
}
