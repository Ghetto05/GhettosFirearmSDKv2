using System.Collections;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class FirearmCleaner : ThunderScript
    {
        public override void ScriptUpdate()
        {
            if (FirearmsSettings.firearmDespawnTime > 0 && (Time.time - Player.local?.creature?.spawnTime > 30))
            {
                foreach (Firearm firearm in Firearm.all.ToArray())
                {
                    if (firearm.item != null && Time.time - firearm.item.spawnTime > 10f && !firearm.item.physicBody.rigidBody.isKinematic && !firearm.item.handlers.Any() && !firearm.item.tkHandlers.Any() && !firearm.item.isGripped && firearm.item.holder == null && Time.time - firearm.item.lastInteractionTime > FirearmsSettings.firearmDespawnTime)
                    {
                        firearm.item.Despawn();
                    }
                }
            }
        }
    }
}