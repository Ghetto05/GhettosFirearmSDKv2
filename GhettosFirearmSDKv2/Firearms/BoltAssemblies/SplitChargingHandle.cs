using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class SplitChargingHandle : MonoBehaviour
    {
        public Transform idlePosition;
        public Transform reference;
        public List<Transform> chargingHandles;
        public List<Handle> handles;

        private void FixedUpdate()
        {
            if (!handles.SelectMany(x => x.handlers).Any())
                return;
            var chosenHandle = handles.First(x => x.handlers.Any());
            foreach (var t in chargingHandles)
            {
                t.SetParent(handles.IndexOf(chosenHandle) == chargingHandles.IndexOf(t) ? reference : idlePosition);
                t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
    }
}
