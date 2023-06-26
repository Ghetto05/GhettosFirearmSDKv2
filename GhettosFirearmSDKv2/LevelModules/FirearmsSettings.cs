﻿using IngameDebugConsole;
using Newtonsoft.Json;
using System.Collections;
using System.IO;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class FirearmsSettings : ThunderScript
    {
        public static FirearmsSettings local;

        #region Initialization
        public override void ScriptEnable()
        {
            local = this;

            #region oldSaveLoader

            Debug.Log("------------> Loaded FirearmSDKv2 settings!");
            Debug.Log($"-----------------> Incapitate: {incapitateOnTorsoShot}");
            Debug.Log($"-----------------> Infinite ammo: {infiniteAmmo}");
            Debug.Log($"-----------------> Caliber checks: {doCaliberChecks}");
            Debug.Log($"-----------------> Magazine type checks: {doMagazineTypeChecks}");
            Debug.Log($"-----------------> Damage multiplier: {damageMultiplier}");
            Debug.Log($"-----------------> Long press time: {longPressTime}");
            #endregion oldSaveLoader

            EventManager.OnPlayerPrefabSpawned += EventManager_OnPlayerSpawned;
            EventManager.onCreatureSpawn += EventManager_onCreatureSpawn;
            EventManager.onItemSpawn += EventManager_onItemSpawn;
        }

        private void EventManager_onItemSpawn(Item item)
        {
            if (item.spawnPoint == null) item.spawnPoint = item.transform;
        }

        private void EventManager_onCreatureSpawn(Creature creature)
        {
            //Chemicals (NPC)
            if (!creature.isPlayer) creature.gameObject.AddComponent<Chemicals.NPCChemicalsModule>();
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
            if (Level.current.data.id.Equals("Home")) SpawnHomeItems();

            //First person only renderer
            NVGOnlyRenderer ren = Player.local.head.cam.gameObject.AddComponent<NVGOnlyRenderer>();
            ren.renderCamera = Player.local.head.cam;
            ren.renderType = NVGOnlyRenderer.Types.FirstPerson;
        }
        #endregion Initialization

        #region Settings
        #region Values
        #region Static
        public static bool magazinesHaveNoCollision = true;
        public static float scopeX1MagnificationFOV = 13f; //28.5f
        public static float cartridgeEjectionTorque = 1f;
        public static float cartridgeEjectionForceRandomizationDevision = 3f;
        public static float firingSoundDeviation = 0.2f;
        public static float invokeTime = 0.1f;
        #endregion Static

        #region PureSettings
        //[ModOption(name = "HUD scale", tooltip = "Scales HUDs.", saveValue = true)]
        public static float hudScale = 1f;

        [ModOptionOrder(1)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Cartridge despawn time", tooltip = "Despawns spent casings after set time. Disabled if set to 0.", saveValue = true)]
        public static float cartridgeDespawnTime = 0f;

        [ModOptionOrder(2)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Long press (safety switch) time", tooltip = "Defines the amount of time you need to hold alternate use to switch fire modes.", saveValue = true, defaultValueIndex = 5)]
        public static float longPressTime = 0.5f;

        [ModOptionOrder(3)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Revolver trigger deadzone", tooltip = "Lowers the trigger threshold for revolvers. Helpful if your revolver does not fire double action.", saveValue = true)]
        public static float revolverTriggerDeadzone = 0f;

        [ModOptionOrder(4)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Trigger discipline timer", tooltip = "Defines the amount of time after which the index finger will move off the trigger after last pressing it.", saveValue = true, defaultValueIndex = 4, valueSourceName = nameof(tdt))]
        public static float triggerDisciplineTime = 3f;

        [ModOptionOrder(5)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Do blunt damage", tooltip = "If enabled, bullets will deal blunt damage rather than pierce damage. Intended for things like headbreaker. Has no impact on damage.", defaultValueIndex = 0, saveValue = true)]
        public static bool bulletsAreBlunt = false;
        public static ModOptionFloat[] tdt =
        {
            new ModOptionFloat("0.1", 0.1f),
            new ModOptionFloat("0.5", 0.5f),
            new ModOptionFloat("1", 1),
            new ModOptionFloat("2", 2),
            new ModOptionFloat("3", 3),
            new ModOptionFloat("5", 5),
            new ModOptionFloat("7.5", 7.5f),
            new ModOptionFloat("10", 10),
            new ModOptionFloat("30", 30),
            new ModOptionFloat("Never", 999999)
        };

        #endregion PureSettings

        #region Debug
        [ModOptionOrder(1)]
        [ModOptionCategory("Debug", 3)]
        [ModOption(name = "Display debug messages", tooltip = "Only for debugging use.", defaultValueIndex = 0, saveValue = true)]
        public static bool debugMode;

        [ModOptionOrder(2)]
        [ModOptionCategory("Debug", 3)]
        [ModOption(name = "Spawn Liam", tooltip = "Spawn the blue guy. Requires map reload to take effect.", defaultValueIndex = 0, saveValue = true)]
        public static bool spawnLiam;
        #endregion Debug

        #region Cheats
        [ModOptionOrder(1)]
        [ModOptionCategory("Cheats", 2)]
        [ModOption(name = "Incapitate hit creatures", tooltip = "If enabled, shooting a creature in the torso will prevent them from standing up. May be mistaken for the creature dying.", defaultValueIndex = 3, saveValue = true, valueSourceName = nameof(iots))]
        public static float incapitateOnTorsoShot;
        public static ModOptionFloat[] iots =
        {
            new ModOptionFloat("Disabled", 0.0f),
            new ModOptionFloat("10 Seconds", 10),
            new ModOptionFloat("20 Seconds", 20),
            new ModOptionFloat("30 Seconds", 30),
            new ModOptionFloat("40 Seconds", 40),
            new ModOptionFloat("50 Seconds", 50),
            new ModOptionFloat("1 Minute", 60),
            new ModOptionFloat("2 Minutes", 120),
            new ModOptionFloat("3 Minutes", 180),
            new ModOptionFloat("4 Minutes", 240),
            new ModOptionFloat("5 Minutes", 300),
            new ModOptionFloat("10 Minutes", 600),
            new ModOptionFloat("Permanent", -1f),
            new ModOptionFloat("1 Day", 1440),
            new ModOptionFloat("1 Week", 10080),
            new ModOptionFloat("1 Year", 525600),
            new ModOptionFloat("1 Decade", 5256000),
            new ModOptionFloat("1 Century", 52560000),
            new ModOptionFloat("1 Millenia", 525600000),
            new ModOptionFloat("Till Battle State Games adds the Colt M16A4 to EFT", Mathf.Infinity),
            new ModOptionFloat("Till our LORD AND SAVIOUR, JESUS CHIRST arrives", 3610558080)
        };

        [ModOptionOrder(2)]
        [ModOptionCategory("Cheats", 2)]
        [ModOption(name = "Infinite ammo", tooltip = "If enabled, magazines will refill as they are used and chamber loaders will not use up the loaded cartridge.", defaultValueIndex = 0, saveValue = true)]
        public static bool infiniteAmmo = false;

        [ModOptionOrder(3)]
        [ModOptionCategory("Cheats", 2)]
        [ModOption(name = "Caliber checks", tooltip = "If disabled, any magazine or chamber can be loaded with any caliber.", defaultValueIndex = 1, saveValue = true)]
        public static bool doCaliberChecks = true;

        [ModOptionOrder(4)]
        [ModOptionCategory("Cheats", 2)]
        [ModOption(name = "Magazine type checks", tooltip = "If disabled, magazine can be loaded into any firearm.", defaultValueIndex = 1, saveValue = true)]
        public static bool doMagazineTypeChecks = true;

        [ModOptionOrder(5)]
        [ModOptionCategory("Cheats", 2)]
        [ModOption(name = "Damage multiplier", tooltip = "Multiplies the damage done by projectiles.", saveValue = true, defaultValueIndex = 10)]
        public static float damageMultiplier = 1f;
        #endregion Cheats
        #endregion Values

        #region Other
        private void OverrideJson()
        {
            //CreateSaveFolder();
            //File.WriteAllText(GetSaveFolderPath() + "\\firearm.settings", JsonConvert.SerializeObject(values, Catalog.jsonSerializerSettings));
        }

        public void SendUpdate()
        {
            //OverrideJson();
            //OnValueChangedEvent?.Invoke();
        }

        //public class SettingsWrapper
        //{
        //    public bool incapitateOnTorsoShot = false;
        //    public bool infiniteAmmo = false;
        //    public bool doCaliberChecks = true;
        //    public bool doMagazineTypeChecks = true;
        //    public float damageMultiplier = 1f;
        //    public bool magazinesHaveNoCollision = true;
        //    public float scopeX1MagnificationFOV = 28.5f;
        //    public float cartridgeEjectionTorque = 1f;
        //    public float cartridgeEjectionForceRandomizationDevision = 3f;
        //    public float hudScale = 1f;
        //    public float cartridgeDespawnTime = 0f;
        //    public float firingSoundDeviation = 0.2f;
        //    public float longPressTime = 0.5f;
        //    public float revolverTriggerDeadzone = 0f;
        //}

        public delegate void OnValueChangedDelegate();
        public static event OnValueChangedDelegate OnValueChangedEvent;
        #endregion Other
        #endregion Settings

        #region Gun Locker
        private void SpawnHomeItems()
        {
            Level.current.StartCoroutine(DelayedLockerSpawn());
            if (spawnLiam) Level.current.StartCoroutine(DelayedRigEditorSpawn());
        }

        private IEnumerator DelayedLockerSpawn()
        {
            yield return new WaitForSeconds(3f);
            Vector3 position = new Vector3(41.3f, 2.5f, -43.0f);
            Vector3 rotation = new Vector3(0, 120, 0);
            Addressables.InstantiateAsync("Ghetto05.FirearmFrameworkV2.Locker", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += (System.Action<AsyncOperationHandle<GameObject>>)(handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning(("Unable to instantiate gun locker!"));
                    Addressables.ReleaseInstance(handle);
                }
            });
        }

        private IEnumerator DelayedRigEditorSpawn()
        {
            yield return new WaitForSeconds(3f);
            Vector3 position = new Vector3(44.03f, 2.5f, -44.37f);
            Vector3 rotation = new Vector3(0, -36, 0);
            Debug.Log("Blue guy");
            Addressables.InstantiateAsync("Ghetto05.Firearms.Clothes.Rigs.Editor", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += (System.Action<AsyncOperationHandle<GameObject>>)(handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning(("Unable to instantiate rig editor!"));
                    Addressables.ReleaseInstance(handle);
                }
            });
        }
        #endregion Gun Locker

        #region General
        public static readonly string saveFolderName = "!GhettosFirearmSDKv2Saves";

        public static string GetSaveFolderPath()
        {
            return FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName);
        }

        public static void CreateSaveFolder()
        {
            Directory.CreateDirectory(GetSaveFolderPath() + "\\Saves");
            ModManager.ModData manifest = new ModManager.ModData
            {
                Name = "GhettosFirearmSDKv2Saves",
                Author = "Ghetto05",
                ModVersion = "1.0",
                Description = "Saves for your guns.",
                GameVersion = ThunderRoadSettings.current.game.minModVersion
            };
            File.WriteAllText(GetSaveFolderPath() + "\\manifest.json", JsonConvert.SerializeObject(manifest, Catalog.jsonSerializerSettings));
        }
        #endregion General
    }
}