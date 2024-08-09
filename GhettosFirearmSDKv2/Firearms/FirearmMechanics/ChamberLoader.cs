using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class ChamberLoader : MonoBehaviour, ICaliberGettable
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
            if ((lockingCondition == null || lockingCondition.IsUnlocked()) && Util.CheckForCollisionWithThisCollider(collision, loadCollider) && collision.collider.GetComponentInParent<Cartridge>() is { } c && Util.AllowLoadCartridge(c, caliber))
            {
                if (boltToBeLoaded.LoadChamber(c))
                {
                    Util.PlayRandomAudioSource(insertSounds);
                }
            }
        }

        public string GetCaliber()
        {
            return caliber;
        }

        public Transform GetTransform()
        {
            return transform;
        }
    }
}
