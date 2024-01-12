using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class BeltFedCoverAxis : MonoBehaviour
    {
        public BeltFedCover beltFed;
        public bool foldOnFullyOpened;
        public Transform axis;
        public Transform idlePosition;
        public Transform foldedPosition;

        private void FixedUpdate()
        {
            BoltBase.BoltState openedState =
                !foldOnFullyOpened ? BoltBase.BoltState.Back : BoltBase.BoltState.Locked;
            if (beltFed.state == openedState)
                axis.SetLocalPositionAndRotation(foldedPosition.localPosition, foldedPosition.localRotation);
            else
                axis.SetLocalPositionAndRotation(idlePosition.localPosition, idlePosition.localRotation);
        }
    }
}
