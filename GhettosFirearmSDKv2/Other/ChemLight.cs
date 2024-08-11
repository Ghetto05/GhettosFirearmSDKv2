using ThunderRoad;
using UnityEngine;

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

        private bool _triggered;
        private float _triggerTime;
        private bool _appliedConstant;
        private bool _burntOut;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            item.OnHeldActionEvent += (_, _, action) =>
            {
                if (action == Interactable.Action.UseStart)
                    Trigger();
            };
        }

        public void Trigger()
        {
            if (_triggered) return;
            _triggered = true;
            _triggerTime = Time.time;
            item.DisallowDespawn = true;
            Util.PlayRandomAudioSource(triggerSounds);
        }

        private void Update()
        {
            if (_burntOut || !_triggered)
                return;
            
            var timePassed = Time.time - _triggerTime;
            var timeRemaining = _triggerTime + burnTime - Time.time;

            if (timePassed > burnTime)
            {
                ApplyLightLevel(0);
                _burntOut = true;
                item.DisallowDespawn = false;
            }

            if (timePassed <= lightUpTime)
                ApplyLightLevel(timePassed / lightUpTime);
            else if (timeRemaining <= lightDownTime)
                ApplyLightLevel(timeRemaining / lightDownTime);
            else if (!_appliedConstant)
            {
                _appliedConstant = true;
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
                r.material.SetColor(EmissionColor, color * strength * Mathf.Clamp01(level));
            }

            foreach (var l in lights)
            {
                l.intensity = Mathf.Lerp(0, lightIntensity, Mathf.Clamp01(level));
            }
        }
    }
}
