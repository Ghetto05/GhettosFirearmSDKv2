using GhettosFirearmSDKv2.Explosives;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Explosives/Detonators/Time based")]
    public class TimedDetonator : MonoBehaviour
    {
        public Explosive explosive;
        public bool startAtAwake;
        public float delay;
        private float _startTime;
        private bool _armed;

        private void Awake()
        {
            if (startAtAwake) Arm();
        }

        public void Arm()
        {
            _startTime = Time.time;
            _armed = true;
        }

        private void Update()
        {
            if (_armed && Time.time - delay >= _startTime)
            {
                explosive.Detonate();
            }
        }
    }
}
