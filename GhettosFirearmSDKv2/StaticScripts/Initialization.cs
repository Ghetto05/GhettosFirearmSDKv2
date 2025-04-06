using System.Linq;
using GhettosFirearmSDKv2.Chemicals;
using GhettosFirearmSDKv2.Other.LockSpell;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

// ReSharper disable once UnusedType.Global - Instantinated by game code
public class Initialization : ThunderScript
{
    public override void ScriptEnable()
    {
        SendStartupMessage();
        ExecuteHandposeValidation();

        foreach (var glsd in Catalog.GetDataList<GunLockerSaveData>())
        {
            glsd.GenerateItem();
        }

        EventManager.OnPlayerPrefabSpawned += OnPlayerPrefabSpawned;
        EventManager.onPossess += OnPossess;
        EventManager.onCreatureSpawn += OnCreatureSpawn;
        Item.OnItemSpawn += OnItemSpawn;
    }

    public override void ScriptDisable()
    {
        EventManager.OnPlayerPrefabSpawned -= OnPlayerPrefabSpawned;
        EventManager.onPossess -= OnPossess;
        EventManager.onCreatureSpawn -= OnCreatureSpawn;
        Item.OnItemSpawn -= OnItemSpawn;
    }

    private static void OnPossess(Creature creature, EventTime eventTime)
    {
        LockSpell.ToggleEquip();
    }

    private static void OnItemSpawn(Item item)
    {
        if (!item.spawnPoint)
        {
            item.spawnPoint = item.transform;
        }
    }

    private static void OnCreatureSpawn(Creature creature)
    {
        //Chemicals (NPC)
        if (creature.isPlayer)
        {
            return;
        }

        creature.gameObject.AddComponent<NpcChemicalsModule>();

        var id = creature.data.prefabAddress switch
        {
            "Bas.Creature.HumanMale" => "Ghetto05.FirearmSDKv2.ThermalBody.Male",
            "Bas.Creature.HumanFemale" => "Ghetto05.FirearmSDKv2.ThermalBody.Female",
            "Bas.Creature.Chicken" => "Ghetto05.FirearmSDKv2.ThermalBody.Chicken",
            _ => "NoneFound"
        };

        if (!id.Equals("NoneFound"))
        {
            Catalog.InstantiateAsync(id, creature.transform.position, creature.transform.rotation, creature.transform, body =>
            {
                var tb = body.GetComponent<ThermalBody>();
                tb.ApplyTo(creature);
            }, "Thermal Imaging Spawner");
        }
    }

    private static void OnPlayerPrefabSpawned()
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
        {
            HomeAdjustments.local.SpawnHomeItems();
        }

        //First person only renderer
        var ren = Player.local.head.cam.gameObject.AddComponent<NvgOnlyRenderer>();
        ren.renderCamera = Player.local.head.cam;
        ren.renderType = NvgOnlyRenderer.Types.FirstPerson;
    }

    private void SendStartupMessage()
    {
        var version = ModManager.TryGetModData(GetType().Assembly, out var data) ? data.ModVersion : "?";

        var initialMessage = "\n\n" +
                             "----> Loaded FirearmSDKv2!\n" +
                             $"----> Version: {version}\n" +
                             "----> \n" +
                             "----> Mod versions check:\n" +
                             UpdateChecker.CheckForUpdates() +
                             "\n\n";

        Debug.Log(initialMessage);
    }

    private static void ExecuteHandposeValidation()
    {
        var incompleteData = Catalog.GetDataList<HandPoseData>().Where(x =>
            !x.poses.Any(y => y.creatureName.Equals("HumanMale")) ||
            !x.poses.Any(y => y.creatureName.Equals("HumanFemale"))).ToList();
        if (incompleteData.Count > 0)
        {
            var dataMessage = "\n";
            dataMessage += "INCOMPLETE HANDLE DATA FOUND! The following hand poses lack a creature:";
            foreach (var pose in incompleteData)
            {
                if (!pose.poses.Any(x => x.creatureName.Equals("HumanMale")))
                {
                    dataMessage += $"\n   ID: {pose.id} - HumanMale";
                }
                if (!pose.poses.Any(x => x.creatureName.Equals("HumanFemale")))
                {
                    dataMessage += $"\n   ID: {pose.id} - HumanFemale";
                }
            }

            Debug.LogWarning(dataMessage);
        }
    }
}