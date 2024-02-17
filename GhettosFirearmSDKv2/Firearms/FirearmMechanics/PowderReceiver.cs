using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PowderReceiver : MonoBehaviour
    {
        public int grainCapacity;
        public int currentAmount;
        public int minimum;
        public bool blocked;
        public Collider loadCollider;
        public Transform fillRoot;
        public Transform emptyPosition;
        public Transform filledPosition;
        public Transform fillPosition;

        public bool Sufficient()
        {
            return currentAmount >= minimum;
        }

        private void FixedUpdate()
        {
            if (fillRoot != null)
                fillRoot.localScale = new Vector3(1, 1, (float)currentAmount / (float)minimum);
            if (fillPosition != null)
                fillPosition.position = Vector3.Lerp(emptyPosition.position, filledPosition.position, (float)currentAmount / (float)minimum);
        }
    }
}
