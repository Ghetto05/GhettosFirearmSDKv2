using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class RecoilSpring : MonoBehaviour
    {
        public Transform springRoot;
        public BoltBase bolt;
        public Vector3 targetScale;

        private void FixedUpdate()
        {
            springRoot.localScale = Vector3.Lerp(Vector3.one, targetScale, bolt.cyclePercentage);
        }
    }
}
