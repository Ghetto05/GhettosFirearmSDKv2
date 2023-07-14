using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class ChamberLoader : MonoBehaviour
    {
        public string caliber;
        public Collider loadCollider;
        public BoltBase boltToBeLoaded;
        public Lock lockingCondition;
        public FirearmBase firearm;
        public AudioSource[] insertSounds;

        private void Start()
        {
            firearm.OnCollisionEvent += OnCollisionEnter;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if ((lockingCondition == null || lockingCondition.IsUnlocked()) && Util.CheckForCollisionWithThisCollider(collision, loadCollider) && collision.collider.GetComponentInParent<Cartridge>() is Cartridge c && Util.AllowLoadCatridge(c, caliber))
            {
                if (boltToBeLoaded.LoadChamber(c))
                {
                    Util.PlayRandomAudioSource(insertSounds);
                }
            }
        }
    }
}
