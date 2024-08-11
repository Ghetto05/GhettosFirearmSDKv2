using System.Linq;
using GhettosFirearmSDKv2.Chemicals;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Initialization : ThunderScript
    {
        public override void ScriptEnable()
        {
            #region Initial message

            var version = ModManager.TryGetModData(GetType().Assembly, out var data) ? data.ModVersion : "?";

            var initialMessage = "\n\n" +
                                 "----> Loaded FirearmSDKv2!\n" +
                                 $"----> Version: {version}\n" +
                                 "----> \n" +
                                 "----> Mod versions check:\n" +
                                 UpdateChecker.CheckForUpdates() +
                                 "\n\n";

            Debug.Log(initialMessage);

            #endregion

            #region HandPose check
            
            var incompleteData = Catalog.GetDataList<HandPoseData>().Where(x => !x.poses.Any(y => y.creatureName.Equals("HumanMale")) || !x.poses.Any(y => y.creatureName.Equals("HumanFemale"))).ToList();
            if (incompleteData.Count > 0)
            {
                var dataMessage = "\n";
                dataMessage += "INCOMPLETE HANDLE DATA FOUND! The following hand poses lack a creature:";
                foreach (var pose in incompleteData)
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
                creature.gameObject.AddComponent<NpcChemicalsModule>();

                var id = "NoneFound";
                if (creature.data.prefabAddress.Equals("Bas.Creature.HumanMale")) id = "Ghetto05.FirearmSDKv2.ThermalBody.Male";
                else if (creature.data.prefabAddress.Equals("Bas.Creature.HumanFemale")) id = "Ghetto05.FirearmSDKv2.ThermalBody.Female";
                else if (creature.data.prefabAddress.Equals("Bas.Creature.Chicken")) id = "Ghetto05.FirearmSDKv2.ThermalBody.Chicken";

                if (!id.Equals("NoneFound"))
                {
                    Catalog.InstantiateAsync(id, creature.transform.position, creature.transform.rotation, creature.transform, body =>
                    {
                        var tb = body.GetComponent<ThermalBody>();
                        tb.ApplyTo(creature);
                    }, "Thermal Imaging Spawner");
                }
            }
        }

        private void EventManager_OnPlayerSpawned()
        {
            //Chemicals (Player)
            Player.local.gameObject.AddComponent<PlayerEffectsAndChemicalsModule>();

            //Penetration Levels
            foreach (var rppd in Catalog.GetDataList<RequiredPenetrationPowerData>())
            {
                rppd.Init();
            }

            //Gun locker
            if (Level.current.data.id.Equals("Home"))
                HomeAdjustments.local.SpawnHomeItems();

            //First person only renderer
            var ren = Player.local.head.cam.gameObject.AddComponent<NvgOnlyRenderer>();
            ren.renderCamera = Player.local.head.cam;
            ren.renderType = NvgOnlyRenderer.Types.FirstPerson;
        }
    }
}