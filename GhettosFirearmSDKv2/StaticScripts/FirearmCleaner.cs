using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

// ReSharper disable once UnusedType.Global - Instantinated by game code
public class FirearmCleaner : ThunderScript
{
    public override void ScriptUpdate()
    {
        if (Settings.firearmDespawnTime > 0 && Time.time - Player.local?.creature?.spawnTime > 30)
        {
            foreach (var firearmBase in FirearmBase.all.Where(x => x.GetType() == typeof(Firearm)).ToArray())
            {
                var firearm = (Firearm)firearmBase;
                if (firearm.item && Time.time - firearm.item.spawnTime > 10f && !firearm.item.physicBody.rigidBody.isKinematic && !firearm.item.handlers.Any() && !firearm.item.tkHandlers.Any() && !firearm.item.isGripped && !firearm.item.holder && Time.time - firearm.item.lastInteractionTime > Settings.firearmDespawnTime)
                {
                    firearm.item.Despawn();
                }
            }

            foreach (var magazine in Magazine.all.Where(m => !m.overrideItem && !m.overrideAttachment).ToArray())
            {
                if (magazine.item && Time.time - magazine.item.spawnTime > 10f && !magazine.item.physicBody.rigidBody.isKinematic && !magazine.item.handlers.Any() && !magazine.item.tkHandlers.Any() && !magazine.item.isGripped && !magazine.item.holder && Time.time - magazine.item.lastInteractionTime > Settings.firearmDespawnTime)
                {
                    magazine.item.Despawn();
                }
            }
        }
    }
}