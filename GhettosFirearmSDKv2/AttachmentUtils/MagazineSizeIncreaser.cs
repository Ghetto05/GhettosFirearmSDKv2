using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class MagazineSizeIncreaser : MonoBehaviour
    {
        public Magazine magazine;
        public bool useDeltaInsteadOfFixed;
        public int targetSize;
        public int deltaSize;
        private int previousSize;

        public void Apply()
        {
            if (magazine == null) return;
            previousSize = magazine.maximumCapacity;
            if (useDeltaInsteadOfFixed)
            {
                magazine.maximumCapacity += deltaSize;
            }
            else
            {
                magazine.maximumCapacity = targetSize;
            }
        }

        public void Revert()
        {
            if (magazine == null) return;
            magazine.maximumCapacity = previousSize;
        }
    }
}
