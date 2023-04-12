using IngameDebugConsole;
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
        public static SettingsWrapper values;

        #region Initialization
        public override void ScriptEnable()
        {
            local = this;

            string oldPath = FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, "GhettosFirearmSDKv2Saves");
            if (File.Exists(oldPath)) File.Delete(oldPath);

            string settingsPath = FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName + "\\firearm.settings");
            if (File.Exists(settingsPath))
            {
                values = JsonConvert.DeserializeObject<SettingsWrapper>(File.ReadAllText(settingsPath), Catalog.jsonSerializerSettings);
            }
            else
            {
                values = new SettingsWrapper();
                CreateSaveFolder();
                OverrideJson();
            }

            Debug.Log("------------> Loaded FirearmSDKv2 settings!");
            Debug.Log($"-----------------> Incapitate: {values.incapitateOnTorsoShot}");
            Debug.Log($"-----------------> Infinite ammo: {values.infiniteAmmo}");
            Debug.Log($"-----------------> Caliber checks: {values.doCaliberChecks}");
            Debug.Log($"-----------------> Magazine type checks: {values.doMagazineTypeChecks}");
            Debug.Log($"-----------------> Damage multiplier: {values.damageMultiplier}");
            Debug.Log($"-----------------> Long press time: {values.longPressTime}");

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
            if (Level.current.data.id.Equals("Home")) SpawnGunLocker();

            //First person only renderer
            NVGOnlyRenderer ren = Player.local.head.cam.gameObject.AddComponent<NVGOnlyRenderer>();
            ren.renderCamera = Player.local.head.cam;
            ren.renderType = NVGOnlyRenderer.Types.FirstPerson;
        }
        #endregion Initialization

        #region Settings

        private void OverrideJson()
        {
            CreateSaveFolder();
            File.WriteAllText(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName + "\\firearm.settings"), JsonConvert.SerializeObject(values, Catalog.jsonSerializerSettings));
        }

        public void SendUpdate()
        {
            OverrideJson();
            OnValueChangedEvent?.Invoke();
        }

        public class SettingsWrapper
        {
            public bool incapitateOnTorsoShot = false;
            public bool infiniteAmmo = false;
            public bool doCaliberChecks = true;
            public bool doMagazineTypeChecks = true;
            public float damageMultiplier = 1f;
            public bool magazinesHaveNoCollision = true;
            public float scopeX1MagnificationFOV = 28.5f;
            public float cartridgeEjectionTorque = 1f;
            public float cartridgeEjectionForceRandomizationDevision = 3f;
            public float hudScale = 1f;
            public float cartridgeDespawnTime = 0f;
            public float firingSoundDeviation = 0.2f;
            public float longPressTime = 0.5f;
            public float revolverTriggerDeadzone = 0f;
        }

        public delegate void OnValueChangedDelegate();
        public static event OnValueChangedDelegate OnValueChangedEvent;
        #endregion Settings

        #region Gun Locker
        private void SpawnGunLocker()
        {
            Level.current.StartCoroutine(DelayedLockerSpawn());
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
                    Debug.LogWarning((object)("Unable to instantiate gun locker!"));
                    Addressables.ReleaseInstance(handle);
                }
            });
        }
        #endregion Gun Locker

        #region General
        public static readonly string saveFolderName = "!GhettosFirearmSDKv2Saves";

        public static void CreateSaveFolder()
        {
            if (File.Exists(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName + "\\Saves")))
            {
                File.Move(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName + "\\Saves"), FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName + "\\Saves"));
            }
            else
            {
                Directory.CreateDirectory(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName + "\\Saves"));
                ModManager.ModData manifest = new ModManager.ModData
                {
                    Name = "GhettosFirearmSDKv2Saves",
                    Author = "Ghetto05",
                    ModVersion = "1.0",
                    Description = "Saves for your guns.",
                    GameVersion = ThunderRoadSettings.current.game.minModVersion
                };
                File.WriteAllText(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, saveFolderName + "\\manifest.json"), JsonConvert.SerializeObject(manifest, Catalog.jsonSerializerSettings));
            }
        }
        #endregion General
    }
}
