﻿using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class StockTogglerAdditional : MonoBehaviour
    {
        public StockToggler parent;
        public AudioSource toggleSound;
        public Transform pivot;
        public Transform[] positions;

        private void Start()
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