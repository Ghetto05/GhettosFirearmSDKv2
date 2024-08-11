using System;
using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;

namespace GhettosFirearmSDKv2
{
    public class Projectilifier : ItemModule
    {
        public string caliber;
        public float recoil;
        public float velocity;

        public Cartridge cartridge;

        public override void OnItemLoaded(Item item)
        {
            cartridge = item.gameObject.AddComponent<Cartridge>();
            cartridge.item = item;
            cartridge.destroyOnFire = true;
            cartridge.colliders = new List<Collider>();
            foreach (var c in item.GetComponentsInChildren<Collider>())
            {
                cartridge.colliders.Add(c);
            }
            cartridge.caliber = caliber;
            cartridge.data = new ProjectileData();
            cartridge.data.isHitscan = false;
            cartridge.data.projectileItemId = item.data.id;
            cartridge.data.recoil = recoil;
            cartridge.data.muzzleVelocity = velocity;
        }
    }
}
