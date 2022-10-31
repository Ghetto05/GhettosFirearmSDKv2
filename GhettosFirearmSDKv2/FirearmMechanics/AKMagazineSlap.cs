using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AKMagazineSlap : MonoBehaviour
    {
        public FirearmBase firearm;
        public List<Collider> triggers;

        public void Awake()
        {
            firearm.OnCollisionEvent += Firearm_OnCollisionEvent;
        }

        public void Firearm_OnCollisionEvent(Collision collision)
        {
            if (collision.collider.GetComponentInParent<Magazine>() is Magazine mag)
            {
                if (triggers.Contains(collision.contacts[0].thisCollider))
                {
                    if (mag != firearm.magazineWell.currentMagazine)
                    {
                        firearm.magazineWell.Eject(true);
                    }
                }
            }
        }
    }
}
