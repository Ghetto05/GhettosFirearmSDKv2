using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class NvgAdjuster : MonoBehaviour
    {
        private static List<NvgAdjuster> _all;
        public static List<NvgAdjuster> All
        {
            get
            {
                if (_all == null)
                    _all = new List<NvgAdjuster>();
                return _all;
            }
            set
            {
                _all = value;
            }
        }

        public static void UpdateAllOffsets()
        {
            foreach (var nvg in All)
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
            if (upwardAxis != null) upwardAxis.localPosition = Offset(Settings.NvgUpwardOffset);
            if (forwardAxis != null) forwardAxis.localPosition = Offset(Settings.NvgForwardOffset);
            if (sidewaysAxisLeft != null) sidewaysAxisLeft.localPosition = Offset(Settings.NvgSidewaysOffset);
            if (sidewaysAxisRight != null) sidewaysAxisRight.localPosition = Offset(Settings.NvgSidewaysOffset);
            if (foldAxis != null)
            {
                if (Settings.FoldNVGs)
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
