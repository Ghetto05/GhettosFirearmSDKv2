using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Bolt assemblies/Barrel tilt-lock")]
    public class BarrelLocker : MonoBehaviour
    {
        public BoltBase bolt;
        public Transform barrel;
        public Transform lockedParent;
        public Transform openedParent;
        public BoltBase.BoltState lockedState;

        private void FixedUpdate()
        {
            if (bolt.state == lockedState)
            {
                barrel.SetParent(lockedParent);
            }
            else
            {
                barrel.SetParent(openedParent);
            }
            barrel.localPosition = Vector3.zero;
            barrel.localEulerAngles = Vector3.zero;
        }
    }
}