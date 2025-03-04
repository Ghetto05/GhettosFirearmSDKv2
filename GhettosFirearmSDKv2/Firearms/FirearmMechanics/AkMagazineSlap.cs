using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AkMagazineSlap : MonoBehaviour
{
    public FirearmBase firearm;
    public List<Collider> triggers;

    public void Start()
    {
        firearm.OnCollisionEvent += Firearm_OnCollisionEvent;
    }

    public void Firearm_OnCollisionEvent(Collision collision)
    {
        if (collision.collider.GetComponentInParent<Magazine>() is { } mag)
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