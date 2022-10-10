using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class Projectileyfier : ItemModule
    {
        Cartridge car;
        public string caliber;
        public float recoil;
        public float force;

        public override void OnItemLoaded(Item item)
        {
            item.StartCoroutine(delayed(item));
            base.OnItemLoaded(item);
        }

        IEnumerator delayed(Item item)
        {
            yield return new WaitForSeconds(0.5f);
            car = item.gameObject.AddComponent<Cartridge>();
            car.caliber = caliber;
            car.data = item.gameObject.AddComponent<ProjectileData>();
            car.data.accuracyMultiplier = 1f;
            car.data.recoil = recoil;
            car.data.projectileItemId = item.itemId;
            car.data.projectileForce = force;
        }
    }
}
