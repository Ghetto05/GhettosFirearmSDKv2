using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class Shootable : MonoBehaviour
    {
        public ProjectileData.PenetrationLevels requiredPenetrationLevel;
        public bool onlyOnce;
        public UnityEvent onShotEvent;
        private bool shot = false;

        public void Shoot(ProjectileData.PenetrationLevels penetrationLevel)
        {
            if (penetrationLevel >= requiredPenetrationLevel && (!onlyOnce || !shot))
            {
                shot = true;
                onShotEvent.Invoke();
            }
        }
    }
}
