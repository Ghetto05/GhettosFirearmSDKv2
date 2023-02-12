using System.Text;
using System.Linq;
using UnityEngine;
using ThunderRoad;
using System.Collections;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using IngameDebugConsole;

namespace GhettosFirearmSDKv2
{
    public class Settings_LevelModule : LevelModule
    {
        public static Settings_LevelModule local;
        public static Score_LevelModule localScore;

        #region Initialization
        public override IEnumerator OnLoadCoroutine()
        {
            local = this;

            string settingsPath = FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, "GhettosFirearmSDKv2Saves\\settings.json");
            if (File.Exists(settingsPath))
            {
                SettingsWrapper wrap = JsonConvert.DeserializeObject<SettingsWrapper>(File.ReadAllText(settingsPath), Catalog.jsonSerializerSettings);
                incapitateOnTorsoShot = wrap.incapitateOnTorsoShot;
                infiniteAmmo = wrap.infiniteAmmo;
                doCaliberChecks = wrap.doCaliberChecks;
                doMagazineTypeChecks = wrap.doMagazineTypeChecks;
                damageMultiplier = wrap.damageMultiplier;
                magazinesHaveNoCollision = wrap.magazinesHaveNoCollision;
                scopeX1MagnificationFOV = wrap.scopeX1MagnificationFOV;
            }

            Debug.Log("------------> Loaded FirearmSDKv2 settings!");
            Debug.Log($"-----------------> Incapitate: {incapitateOnTorsoShot}");
            Debug.Log($"-----------------> Infinite ammo: {infiniteAmmo}");
            Debug.Log($"-----------------> Caliber checks: {doCaliberChecks}");
            Debug.Log($"-----------------> Magazine type checks: {doMagazineTypeChecks}");
            Debug.Log($"-----------------> Damage multiplier: {damageMultiplier}");
            Debug.Log($"-----------------> Disable magazine collisions: {magazinesHaveNoCollision}");

            EventManager.OnPlayerSpawned += EventManager_OnPlayerSpawned;
            EventManager.onCreatureSpawn += EventManager_onCreatureSpawn;
            DebugLogConsole.AddCommand("SaveFirearmSettings", "Overrides the current Level_Master.json file", SendUpdate);
            return base.OnLoadCoroutine();
        }

        IEnumerator LoadScoreModule()
        {
            Score_LevelModule score = new Score_LevelModule();
            localScore = score;
            level.mode.modules.Add(score);
            score.level = level;
            yield return score.OnLoadCoroutine();
        }

        private void EventManager_onCreatureSpawn(Creature creature)
        {
            //Chemicals (NPC)
            if (!creature.isPlayer) creature.gameObject.AddComponent<Chemicals.NPCChemicalsModule>();

            //Score
            if (localScore == null)
            {
                GameManager.local.StartCoroutine(LoadScoreModule());
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
            if (Level.current.data.id.Equals("Home")) SpawnGunLocker();
        }
        #endregion Initialization

        #region Settings
        #region Values
        public bool incapitateOnTorsoShot;
        public bool infiniteAmmo;
        public bool doCaliberChecks;
        public bool doMagazineTypeChecks;
        public float damageMultiplier;
        public bool magazinesHaveNoCollision = true;
        public float scopeX1MagnificationFOV = 28.5f;

        public float cartridgeEjectionTorque = 1f;
        public float cartridgeEjectionForceRandomizationDevision = 3f;
        #endregion Values

        private void OverrideJson(string content)
        {
            CreateSaveFolder();
            SettingsWrapper wrap = new SettingsWrapper(incapitateOnTorsoShot, infiniteAmmo, doCaliberChecks, doMagazineTypeChecks, damageMultiplier, magazinesHaveNoCollision, scopeX1MagnificationFOV, cartridgeEjectionTorque, cartridgeEjectionForceRandomizationDevision);
            File.WriteAllText(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, "GhettosFirearmSDKv2Saves\\settings.json"), JsonConvert.SerializeObject(wrap, Catalog.jsonSerializerSettings));
        }

        public void SendUpdate()
        {
            string s = JsonConvert.SerializeObject(this, Catalog.jsonSerializerSettings);
            OverrideJson(s);
            OnValueChangedEvent?.Invoke();
        }

        public class SettingsWrapper
        {
            public SettingsWrapper(bool iots, bool ia, bool cc, bool mtc, float dm, bool mhnc, float sx1mfov, float cet, float cefrd)
            {
                incapitateOnTorsoShot = iots;
                infiniteAmmo = ia;
                doCaliberChecks = cc;
                doMagazineTypeChecks = mtc;
                damageMultiplier = dm;
                magazinesHaveNoCollision = mhnc;
                scopeX1MagnificationFOV = sx1mfov;
                cartridgeEjectionTorque = cet;
                cartridgeEjectionForceRandomizationDevision = cefrd;
            }

            public bool incapitateOnTorsoShot;
            public bool infiniteAmmo;
            public bool doCaliberChecks;
            public bool doMagazineTypeChecks;
            public float damageMultiplier;
            public bool magazinesHaveNoCollision;
            public float scopeX1MagnificationFOV;
            public float cartridgeEjectionTorque;
            public float cartridgeEjectionForceRandomizationDevision;
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
            Addressables.InstantiateAsync("GunLocker_Ghetto05_FirearmSDKv2", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += (System.Action<AsyncOperationHandle<GameObject>>)(handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning((object)("Unable to instantiate gun locker!"));
                    Addressables.ReleaseInstance(handle);
                }
            });
        }

        public static void CreateSaveFolder()
        {
            Directory.CreateDirectory(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, "GhettosFirearmSDKv2Saves\\Saves"));
            ModData manifest = new ModData
            {
                Name = "GhettosFirearmSDKv2Saves",
                Author = "Ghetto05",
                ModVersion = "1.0",
                Description = "Saves for your guns.",
                GameVersion = GameSettings.instance.minModVersion
            };
            File.WriteAllText(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, "GhettosFirearmSDKv2Saves\\manifest.json"), JsonConvert.SerializeObject(manifest, Catalog.jsonSerializerSettings));
        }
        #endregion Gun Locker
    }
}
