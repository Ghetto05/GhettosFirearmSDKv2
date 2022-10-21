using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GhettosFirearmSDKv2.Explosives;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Explosives/Detonators/Time based")]
    public class TimedDetonator : MonoBehaviour
    {
        public Explosive explosive;
        public bool startAtAwake;
        public float delay;
        private float startTime;
        private bool armed = false;

        private void Awake()
        {
            if (startAtAwake) Arm();
        }

        public void Arm()
        {
            startTime = Time.time;
            armed = true;
        }

        private void Update()
        {
            if (armed && Time.time - delay >= startTime)
            {
                explosive.Detonate();
            }
        }
    }
}
