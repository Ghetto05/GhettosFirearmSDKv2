using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class Projectilifier : ItemModule
{
    public string Caliber;
    public float Recoil;
    public float Velocity;

    public Cartridge Cartridge;

    public override void OnItemLoaded(Item loadedItem)
    {
        Cartridge = loadedItem.gameObject.AddComponent<Cartridge>();
        Cartridge.item = loadedItem;
        Cartridge.destroyOnFire = true;
        Cartridge.colliders = new List<Collider>();
        foreach (var c in loadedItem.GetComponentsInChildren<Collider>())
        {
            Cartridge.colliders.Add(c);
        }
        Cartridge.caliber = Caliber;
        Cartridge.data = loadedItem.gameObject.AddComponent<ProjectileData>();
        Cartridge.data.isHitscan = false;
        Cartridge.data.projectileItemId = loadedItem.data.id;
        Cartridge.data.recoil = Recoil;
        Cartridge.data.muzzleVelocity = Velocity;
    }
}