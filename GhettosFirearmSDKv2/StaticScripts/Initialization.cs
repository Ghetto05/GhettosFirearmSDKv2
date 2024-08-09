using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Initialization : ThunderScript
    {
        public override void ScriptEnable()
        {
            #region Initial message

            string version = ModManager.TryGetModData(GetType().Assembly, out ModManager.ModData data) ? data.ModVersion : "?";

            string initialMessage = $"\n\n" +
                             $"----> Loaded FirearmSDKv2!\n" +
                             $"----> Version: {version}\n" +
                             $"----> \n" +
                             $"----> Mod versions check:\n" +
                             $"{UpdateChecker.CheckForUpdates()}" +
                             $"\n\n";

            Debug.Log(initialMessage);

            #endregion

            #region HandPose check
            
            List<HandPoseData> incompleteData = Catalog.GetDataList<HandPoseData>().Where(x => !x.poses.Any(y => y.creatureName.Equals("HumanMale")) || !x.poses.Any(y => y.creatureName.Equals("HumanFemale"))).ToList();
            if (incompleteData.Count > 0)
            {
                string dataMessage = "\n";
                dataMessage += "INCOMPLETE HANDLE DATA FOUND! The following hand poses lack a creature:";
                foreach (HandPoseData pose in incompleteData)
                {
                    if (pose.poses.Any(x => x.creatureName.Equals("HumanMale")))
                        dataMessage += $"\n   ID: {pose.id} - HumanMale";
                    if (pose.poses.Any(x => x.creatureName.Equals("HumanFemale")))
                        dataMessage += $"\n   ID: {pose.id} - HumanFemale";
                }
                
                Debug.LogWarning(dataMessage);
            }
            
            #endregion
            
            foreach (var glsd in Catalog.GetDataList<GunLockerSaveData>())
            {
                glsd.GenerateItem();
            }
            foreach (var lto in Catalog.GetDataList<LootTableOverride>())
            {
                lto.ApplyToTable();
            }

            EventManager.OnPlayerPrefabSpawned += EventManager_OnPlayerSpawned;
            EventManager.onCreatureSpawn += EventManager_onCreatureSpawn;
            Item.OnItemSpawn += EventManager_onItemSpawn;
        }

        private void EventManager_onItemSpawn(Item item)
        {
            if (item.spawnPoint == null)
                item.spawnPoint = item.transform;
        }

        private void EventManager_onCreatureSpawn(Creature creature)
        {
            //Chemicals (NPC)
            if (!creature.isPlayer)
            {
                creature.gameObject.AddComponent<Chemicals.NPCChemicalsModule>();

                string id = "NoneFound";
                if (creature.data.prefabAddress.Equals("Bas.Creature.HumanMale")) id = "Ghetto05.FirearmSDKv2.ThermalBody.Male";
                else if (creature.data.prefabAddress.Equals("Bas.Creature.HumanFemale")) id = "Ghetto05.FirearmSDKv2.ThermalBody.Female";
                else if (creature.data.prefabAddress.Equals("Bas.Creature.Chicken")) id = "Ghetto05.FirearmSDKv2.ThermalBody.Chicken";

                if (!id.Equals("NoneFound"))
                {
                    Catalog.InstantiateAsync(id, creature.transform.position, creature.transform.rotation, creature.transform, body =>
                    {
                        ThermalBody tb = body.GetComponent<ThermalBody>();
                        tb.ApplyTo(creature);
                    }, "Thermal Imaging Spawner");
                }
            }
        }

        private void EventManager_OnPlayerSpawned()
        {
            //Chemicals (Player)
            Player.local.gameObject.AddComponent<Chemicals.PlayerEffectsAndChemicalsModule>();

            //Penetration Levels
            foreach (RequiredPenetrationPowerData rppd in Catalog.GetDataList<RequiredPenetrationPowerData>())
            {
                rppd.Init();
            }

            //Gun locker
            if (Level.current.data.id.Equals("Home"))
                HomeAdjustments.local.SpawnHomeItems();

            //First person only renderer
            NVGOnlyRenderer ren = Player.local.head.cam.gameObject.AddComponent<NVGOnlyRenderer>();
            ren.renderCamera = Player.local.head.cam;
            ren.renderType = NVGOnlyRenderer.Types.FirstPerson;
        }
    }
}