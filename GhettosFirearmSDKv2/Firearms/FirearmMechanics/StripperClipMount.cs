using System;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class StripperClipMount : MonoBehaviour
    {
        public Transform mountPoint;
        public MagazineWell connectedWell;
        public BoltBase connectedBolt;
        public Collider mountCollider;
        public string clipType;
        public StripperClip loadedClip;

        private Firearm firearm;

        public void Start()
        {
            Invoke("InvokedStart", FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            firearm = GetComponentInParent<Firearm>();

            firearm.OnCollisionEvent += Firearm_OnCollisionEvent;
            firearm.item.OnDespawnEvent += Item_OnDespawnEvent;
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            if (loadedClip)
            {
                foreach (Cartridge c in loadedClip.cartridges)
                {
                    if (c && c.item) c.item.Despawn();
                }
                if (loadedClip.item) loadedClip.item.Despawn();
            }
        }

        private void Firearm_OnCollisionEvent(Collision collision)
        {

        }
    }
}
