using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Firearm components/Chamber gravity ejector")]
    public class GravtiyEjector : MonoBehaviour
    {
        public BoltBase bolt;
        public Transform direction;
        public float maximumAngle;
        public List<Lock> locks;

        public void FixedUpdate()
        {
            if (Util.AllLocksUnlocked(locks) && CheckEjectionGravity(direction) && bolt.GetChamber() != null)
            {
                bolt.EjectRound();
            }
        }

        private bool CheckEjectionGravity(Transform t)
        {
            var angle = Vector3.Angle(t.forward, Vector3.down);
            return angle < maximumAngle;
        }
    }
}
