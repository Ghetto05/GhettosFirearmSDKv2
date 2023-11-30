using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class NVGAdjuster : MonoBehaviour
    {
        private static List<NVGAdjuster> _all;
        public static List<NVGAdjuster> All
        {
            get
            {
                if (_all == null)
                    _all = new List<NVGAdjuster>();
                return _all;
            }
            set
            {
                _all = value;
            }
        }

        public static void UpdateAllOffsets()
        {
            foreach (NVGAdjuster nvg in All)
            {
                nvg.UpdateOffsets();
            }
        }

        public Transform upwardAxis;
        public Transform forwardAxis;
        public Transform sidewaysAxisLeft;
        public Transform sidewaysAxisRight;
        public Transform foldAxis;
        public Transform idlePosition;
        public Transform foldedPosition;

        private void Start()
        {
            All.Add(this);
            UpdateOffsets();
        }

        public void UpdateOffsets()
        {
            if (upwardAxis != null) upwardAxis.localPosition = Offset(FirearmsSettings.NvgUpwardOffset);
            if (forwardAxis != null) forwardAxis.localPosition = Offset(FirearmsSettings.NvgForwardOffset);
            if (sidewaysAxisLeft != null) sidewaysAxisLeft.localPosition = Offset(FirearmsSettings.NvgSidewaysOffset);
            if (sidewaysAxisRight != null) sidewaysAxisRight.localPosition = Offset(FirearmsSettings.NvgSidewaysOffset);
            if (foldAxis != null)
            {
                if (FirearmsSettings.FoldNvgs)
                    foldAxis.SetLocalPositionAndRotation(foldedPosition.localPosition, foldedPosition.localRotation);
                else
                    foldAxis.SetLocalPositionAndRotation(idlePosition.localPosition, idlePosition.localRotation);
            }
        }

        private Vector3 Offset(float offset)
        {
            return new Vector3(0, 0, offset);
        }
    }
}
