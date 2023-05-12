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
        public static float scopeX1MagnificationFOV = 28.5f;
        public static float cartridgeEjectionTorque = 1f;
        public static float cartridgeEjectionForceRandomizationDevision = 3f;
        public static float firingSoundDeviation = 0.2f;
        #endregion Static

        #region PureSettings
        //[ModOption(name = "HUD scale", tooltip = "Scales HUDs.", category = "Settings", categoryOrder = 4, saveValue = true)]
        public static float hudScale = 1f;
        [ModOption(name = "Cartridge despawn time", tooltip = "Despawns spent casings after set time. Disabled if set to 0.", category = "Settings", categoryOrder = 3, saveValue = true)]
        public static float cartridgeDespawnTime = 0f;
        [ModOption(name = "Long press (safety switch) time", tooltip = "Defines the amount of time you need to hold alternate use to switch fire modes.", category = "Settings", categoryOrder = 1, saveValue = true, defaultValueIndex = 5)]
        public static float longPressTime = 0.5f;
        [ModOption(name = "Revolver trigger deadzone", tooltip = "Lowers the trigger threshold for revolvers. Helpful if your revolver does not fire.", category = "Settings", categoryOrder = 2, saveValue = true)]
        public static float revolverTriggerDeadzone = 0f;
        #endregion PureSettings

        #region Debug
        [ModOption(name = "Display debug messages", tooltip = "Only for debugging use.", category = "Debug", defaultValueIndex = 0, categoryOrder = 9, saveValue = true)]
        public static bool debugMode;
        [ModOption(name = "Spawn Liam", tooltip = "Spawn the blue guy. Requires map reload to take effect.", category = "Debug", defaultValueIndex = 0, categoryOrder = 10, saveValue = true)]
        public static bool spawnLiam;
        #endregion Debug

        #region Cheats
        [ModOption(name = "Incapitate hit creatures", tooltip = "If enabled, shooting a creature in the torso will prevent them from standing up. May be mistaken for the creature dying.", category = "Cheats", categoryOrder = 6, defaultValueIndex = 0, saveValue = true)]
        public static bool incapitateOnTorsoShot = false;
        [ModOption(name = "Infinite ammo", tooltip = "If enabled, magazines will refill as they are used and chamber loaders will not use up the loaded cartridge.", category = "Cheats", categoryOrder = 4, defaultValueIndex = 0, saveValue = true)]
        public static bool infiniteAmmo = false;
        [ModOption(name = "Caliber checks", tooltip = "If disabled, any magazine or chamber can be loaded with any caliber.", category = "Cheats", categoryOrder = 7, defaultValueIndex = 1, saveValue = true)]
        public static bool doCaliberChecks = true;
        [ModOption(name = "Magazine type checks", tooltip = "If disabled, magazine can be loaded into any firearm.", category = "Cheats", categoryOrder = 8, defaultValueIndex = 1, saveValue = true)]
        public static bool doMagazineTypeChecks = true;
        [ModOption(name = "Damage multiplier", tooltip = "Multiplies the damage done by projectiles.", category = "Cheats", categoryOrder = 5, saveValue = true, defaultValueIndex = 10)]
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
