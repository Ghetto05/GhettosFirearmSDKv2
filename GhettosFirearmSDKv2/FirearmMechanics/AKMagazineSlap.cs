using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.FirearmMechanics
{
    public class AKMagazineSlap : MonoBehaviour
    {
        public Firearm firearm;
        public List<Collider> triggers;

        private void Awake()
        {
            firearm.OnCollisionEvent += Firearm_OnCollisionEvent;
        }

        private void Firearm_OnCollisionEvent(Collision collision)
        {
            if (triggers.Contains(collision.contacts[0].thisCollider) && collision.gameObject.GetComponent<Magazine>() is Magazine mag)
            {
                if (mag != firearm.magazineWell.currentMagazine)
                {
                    firearm.magazineWell.Eject();
                }
            }
        }
    }
}
