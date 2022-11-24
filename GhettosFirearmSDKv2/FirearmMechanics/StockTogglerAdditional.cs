using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class StockTogglerAdditional : MonoBehaviour
    {
        public StockToggler parent;
        public AudioSource toggleSound;
        public Transform pivot;
        public Transform[] positions;

        void Awake()
        {
            parent.OnToggleEvent += ApplyPosition;
        }

        public void ApplyPosition(int index, bool playSound = true)
        {
            if (toggleSound != null && playSound) toggleSound.Play();
            pivot.localPosition = positions[index].localPosition;
            pivot.localEulerAngles = positions[index].localEulerAngles;
        }
    }
}