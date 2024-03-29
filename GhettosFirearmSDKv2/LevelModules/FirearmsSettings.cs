﻿using System;
using IngameDebugConsole;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            #region Initial message

            string version = ModManager.TryGetModData(GetType().Assembly, out ModManager.ModData data) ? data.ModVersion : "?";

            string initialMessage = $"\n\n" +
                             $"----> Loaded FirearmSDKv2!\n" +
                             $"----> Version: {version}" +
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
            
            foreach (GunLockerSaveData glsd in Catalog.GetDataList<GunLockerSaveData>())
            {
                glsd.GenerateItem();
            }
            foreach (LootTableOverride lto in Catalog.GetDataList<LootTableOverride>())
            {
                lto.ApplyToTable();
            }

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
        public static float scopeX1MagnificationFOV = 20f; //45f; //60f; //13f; //28.5f
        public static float cartridgeEjectionTorque = 1f;
        public static float cartridgeEjectionForceRandomizationDevision = 3f;
        public static float firingSoundDeviation = 0.2f;
        public static float invokeTime = 0.3f;
        public static float boltPointTreshold = 0.004f;
        public static float aiFirearmSpread = 0f;
        
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
        [ModOption(name = "Firearm despawn time", tooltip = "Despawns any dropped firearms after set time. Disabled if set to 0. Note: Firearms will never despawn up until 10 seconds after having spawned in.", saveValue = true, defaultValueIndex = 8, valueSourceName = nameof(firearmDespawnTimeValues))]
        public static float firearmDespawnTime = 0f;
        public static ModOptionFloat[] firearmDespawnTimeValues =
        {
            new ModOptionFloat("Disabled", 0.0f),
            new ModOptionFloat("0.01 Seconds", 0.01f),
            new ModOptionFloat("5 Seconds", 5),
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
            new ModOptionFloat("10 Minutes", 600)
        };

        [ModOptionOrder(3)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Long press (safety switch) time", tooltip = "Defines the amount of time you need to hold alternate use to switch fire modes.", saveValue = true, defaultValueIndex = 5)]
        public static float longPressTime = 0.5f;

        [ModOptionOrder(4)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Revolver trigger deadzone", tooltip = "Lowers the trigger threshold for revolvers. Helpful if your revolver does not fire double action.", saveValue = true)]
        public static float revolverTriggerDeadzone = 0f;

        [ModOptionOrder(5)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Trigger discipline timer", tooltip = "Defines the amount of time after which the index finger will move off the trigger after last pressing it.", saveValue = true, defaultValueIndex = 4, valueSourceName = nameof(tdt))]
        public static float triggerDisciplineTime = 3f;
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

        [ModOptionOrder(6)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Do blunt damage", tooltip = "If enabled, bullets will deal blunt damage rather than pierce damage. Intended for things like headbreaker. Has no impact on damage.", defaultValueIndex = 0, saveValue = true)]
        public static bool bulletsAreBlunt = false;

        [ModOptionOrder(7)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Play gas mask sounds", tooltip = "If enabled, wearing a gas mask will play a breathing sound", defaultValueIndex = 1, saveValue = true)]
        public static bool playGasMaskSound = false;

        [ModOptionOrder(8)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Explosions dismember", tooltip = "If enabled, any ragdoll part in that is caught in the center of an explosion will be ripped off.", defaultValueIndex = 0, saveValue = true)]
        public static bool explosionsDismember = false;

        [ModOptionOrder(9)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Disable gore", tooltip = "If enabled, there will be no blood effects or dismemberment.", defaultValueIndex = 0, saveValue = true)]
        public static bool disableGore = false;

        #endregion PureSettings

        #region Clothing
        
        #region NVG Offsets

        private static float _nvgForwardOffset;
        [ModOptionOrder(1)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "NVG Forward Offset", tooltip = "Offsets all NVGs forwards.", saveValue = true, defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.possibleNVGOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float NvgForwardOffset
        {
            get { return _nvgForwardOffset; }
            set 
            { 
                _nvgForwardOffset = value; 
                NVGAdjuster.UpdateAllOffsets();
            }
        }
        
        private static float _nvgUpwardOffset;
        [ModOptionOrder(2)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "NVG Upward Offset", tooltip = "Offsets all NVGs upwards.", saveValue = true, defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.possibleNVGOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float NvgUpwardOffset
        {
            get { return _nvgUpwardOffset; }
            set 
            { 
                _nvgUpwardOffset = value; 
                NVGAdjuster.UpdateAllOffsets();
            }
        }
        
        private static float _nvgSidewaysOffset;
        [ModOptionOrder(3)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "NVG Sideways Offset", tooltip = "Offsets all NVGs sideways.", saveValue = true, defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.possibleNVGOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float NvgSidewaysOffset
        {
            get { return _nvgSidewaysOffset; }
            set 
            { 
                _nvgSidewaysOffset = value; 
                NVGAdjuster.UpdateAllOffsets();
            }
        }
        
        private static bool _foldNvgs;
        [ModOptionOrder(4)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "Fold NVGs", tooltip = "Folds all NVGs upwards.", saveValue = true)]
        public static bool FoldNvgs
        {
            get { return _foldNvgs; }
            set
            {
                _foldNvgs = value;
                NVGAdjuster.UpdateAllOffsets();
            }
        }

        #endregion
        
        #endregion
        
        #region Debug
        [ModOptionOrder(1)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Display debug messages", tooltip = "Only for debugging use.", defaultValueIndex = 0, saveValue = true)]
        public static bool debugMode;

        [ModOptionOrder(2)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Spawn Liam", tooltip = "Spawn the blue guy.", defaultValueIndex = 0, saveValue = true)]
        public static bool spawnLiam
        {
            get => _spawnLiam;
            set
            {
                _spawnLiam = value;
                if (_spawnLiam) SpawnLiam();
                else if (liam != null) GameObject.Destroy(liam);
            }
        }
        private static bool _spawnLiam;
        private static GameObject liam;

        [ModOptionOrder(3)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Save guns as prebuilts", tooltip = "Only for development. Saves any gun in the locker with the prebuilt setup.", defaultValueIndex = 0, saveValue = true)]
        public static bool saveAsPrebuilt;
        #endregion Debug

        #region Cheats
        [ModOptionOrder(1)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Incapacitate hit creatures", tooltip = "If enabled, shooting a creature in the torso will prevent them from standing up. May be mistaken for the creature dying.", defaultValueIndex = 3, saveValue = true, valueSourceName = nameof(iots))]
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
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Infinite ammo", tooltip = "If enabled, magazines will refill as they are used and chamber loaders will not use up the loaded cartridge.", defaultValueIndex = 0, saveValue = true)]
        public static bool infiniteAmmo = false;

        [ModOptionOrder(3)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Caliber checks", tooltip = "If disabled, any magazine or chamber can be loaded with any caliber.", defaultValueIndex = 1, saveValue = true)]
        public static bool doCaliberChecks = true;

        [ModOptionOrder(4)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Magazine type checks", tooltip = "If disabled, magazine can be loaded into any firearm.", defaultValueIndex = 1, saveValue = true)]
        public static bool doMagazineTypeChecks = true;

        [ModOptionOrder(5)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Damage multiplier", tooltip = "Multiplies the damage done by projectiles.", saveValue = true, defaultValueIndex = 10)]
        public static float damageMultiplier = 1f;
        #endregion Cheats
        #endregion Values
        #endregion Settings

        #region Gun Locker / Liam
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
            Addressables.InstantiateAsync("Ghetto05.FirearmFrameworkV2.Locker", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning(("Unable to instantiate gun locker!"));
                    Addressables.ReleaseInstance(handle);
                }
            };
        }

        private IEnumerator DelayedRigEditorSpawn()
        {
            yield return new WaitForSeconds(3f);
            SpawnLiam();
        }

        private static void SpawnLiam()
        {
            if (liam != null || !Level.current.data.id.Equals("Home")) return;
            Vector3 position = new Vector3(44.03f, 2.5f, -44.37f);
            Vector3 rotation = new Vector3(0, -36, 0);
            Addressables.InstantiateAsync("Ghetto05.Firearms.Clothes.Rigs.Editor", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning(("Unable to instantiate rig editor!"));
                    Addressables.ReleaseInstance(handle);
                }
                liam = handle.Result;
            };
        }
        #endregion

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
