﻿using System.Collections;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class FirearmCleaner : ThunderScript
    {
        public override void ScriptUpdate()
        {
            if (Settings.firearmDespawnTime > 0 && (Time.time - Player.local?.creature?.spawnTime > 30))
            {
                foreach (var firearmBase in FirearmBase.all.Where(x => x.GetType() == typeof(Firearm)).ToArray())
                {
                    var firearm = (Firearm)firearmBase;
                    if (firearm.item != null && Time.time - firearm.item.spawnTime > 10f && !firearm.item.physicBody.rigidBody.isKinematic && !firearm.item.handlers.Any() && !firearm.item.tkHandlers.Any() && !firearm.item.isGripped && firearm.item.holder == null && Time.time - firearm.item.lastInteractionTime > Settings.firearmDespawnTime)
                    {
                        firearm.item.Despawn();
                    }
                }

                foreach (Magazine magazine in Magazine.all.Where(m => m.overrideItem == null).ToArray())
                {
                    if (magazine.item != null && Time.time - magazine.item.spawnTime > 10f && !magazine.item.physicBody.rigidBody.isKinematic && !magazine.item.handlers.Any() && !magazine.item.tkHandlers.Any() && !magazine.item.isGripped && magazine.item.holder == null && Time.time - magazine.item.lastInteractionTime > Settings.firearmDespawnTime)
                        magazine.item.Despawn();
                }
            }
        }
    }
}