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
            foreach (Firearm firearm in Firearm.all.ToArray())
            {
                if (firearm.item != null && !firearm.item.handlers.Any() && !firearm.item.tkHandlers.Any() && firearm.item.holder == null && Time.time - firearm.item.lastInteractionTime > FirearmsSettings.firearmDespawnTime)
                {
                    firearm.item.Despawn();
                }
            }
        }
    }
}