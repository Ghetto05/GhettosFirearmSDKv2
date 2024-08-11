using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2
{
    public class ChemLight : MonoBehaviour
    {
        public float burnTime = 300f;
        public float lightUpTime = 10f;
        public float lightDownTime = 30f;

        public Item item;
        public MeshRenderer[] renderers;
        public Color color;
        public float strength;
        public AudioSource[] triggerSounds;
        public float lightIntensity;
        public Light[] lights;

        private bool triggered;
        private float triggerTime;
        private bool appliedConstant;
        private bool burntOut;

        private void Start()
        {
            item.OnHeldActionEvent += (hand, handle, action) =>
            {
                if (action == Interactable.Action.UseStart)
                    Trigger();
            };
        }

        public void Trigger()
        {
            if (triggered) return;
            triggered = true;
            triggerTime = Time.time;
            item.DisallowDespawn = true;
            Util.PlayRandomAudioSource(triggerSounds);
        }

        private void Update()
        {
            if (burntOut || !triggered)
                return;
            
            var timePassed = Time.time - triggerTime;
            var timeRemaining = triggerTime + burnTime - Time.time;

            if (timePassed > burnTime)
            {
                ApplyLightLevel(0);
                burntOut = true;
                item.DisallowDespawn = false;
            }

            if (timePassed <= lightUpTime)
                ApplyLightLevel(timePassed / lightUpTime);
            else if (timeRemaining <= lightDownTime)
                ApplyLightLevel(timeRemaining / lightDownTime);
            else if (!appliedConstant)
            {
                appliedConstant = true;
                ApplyLightLevel(1f);
            }

            foreach (var l in lights)
            {
                l.transform.position = transform.position + Vector3.up * 0.07f;
            }
        }

        private void ApplyLightLevel(float level)
        {
            foreach (var r in renderers)
            {
                r.material.SetColor("_EmissionColor", color * strength * Mathf.Clamp01(level));
            }

            foreach (var l in lights)
            {
                l.intensity = Mathf.Lerp(0, lightIntensity, Mathf.Clamp01(level));
            }
        }
    }
}
