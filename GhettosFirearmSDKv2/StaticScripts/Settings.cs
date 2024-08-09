using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GhettosFirearmSDKv2
{
    public class Settings : ThunderScript
    {
        #region Settings
        
        #region Static
        
        public static float scopeX1MagnificationFOV = 20f; //45f; //60f; //13f; //28.5f
        public static float cartridgeEjectionTorque = 1f;
        public static float cartridgeEjectionForceRandomizationDevision = 3f;
        public static float firingSoundDeviation = 0.2f;
        public static float invokeTime = 0.3f;
        public static float boltPointTreshold = 0.004f;
        public static float aiFirearmSpread = 0f;
        
        #endregion Static

        #region General
        
        //[ModOption(name = "HUD scale", tooltip = "Scales HUDs.", saveValue = true)]
        public static float hudScale = 1f;

        [ModOptionOrder(1)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Cartridge despawn time", tooltip = "Despawns spent casings after set time. Disabled if set to 0.", saveValue = true)]
        public static float cartridgeDespawnTime = 0f;

        [ModOptionOrder(2)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Firearm despawn time", tooltip = "Despawns any dropped firearms after set time. Disabled if set to 0. Note: Firearms will never despawn up until 10 seconds after having spawned in.", saveValue = true, defaultValueIndex = 8, valueSourceName = nameof(FirearmsSettingsValues.firearmDespawnTimeValues), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float firearmDespawnTime = 0f;

        [ModOptionOrder(3)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Long press (safety switch) time", tooltip = "Defines the amount of time you need to hold alternate use to switch fire modes.", saveValue = true, defaultValueIndex = 5)]
        public static float longPressTime = 0.5f;

        [ModOptionOrder(4)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Revolver trigger dead zone", tooltip = "Lowers the trigger threshold for revolvers. Helpful if your revolver does not fire double action.", saveValue = true)]
        public static float revolverTriggerDeadzone = 0f;
        
        [ModOptionOrder(5)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Progressive trigger dead zone", tooltip = "Lowers the fire mode switch threshold for progressive triggers. Helpful if firearms with progressive trigger do not fire in full auto.", saveValue = true)]
        public static float progressiveTriggerDeadZone = 0f;

        [ModOptionOrder(6)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Trigger discipline timer", tooltip = "Defines the amount of time after which the index finger will move off the trigger after last pressing it.", saveValue = true, defaultValueIndex = 4, valueSourceName = nameof(FirearmsSettingsValues.triggerDisciplineTimers), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float triggerDisciplineTime = 3f;

        [ModOptionOrder(7)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Do blunt damage", tooltip = "If enabled, bullets will deal blunt damage rather than pierce damage. Intended for things like headbreaker. Has no impact on damage.", defaultValueIndex = 0, saveValue = true)]
        public static bool bulletsAreBlunt = false;

        [ModOptionOrder(8)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Play gas mask sounds", tooltip = "If enabled, wearing a gas mask will play a breathing sound", defaultValueIndex = 1, saveValue = true)]
        public static bool playGasMaskSound = false;

        [ModOptionOrder(9)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Explosions dismember", tooltip = "If enabled, any ragdoll part in that is caught in the center of an explosion will be ripped off.", defaultValueIndex = 0, saveValue = true)]
        public static bool explosionsDismember = false;

        [ModOptionOrder(10)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Break actions only eject fired rounds", tooltip = "If enabled, break actions will only eject fired shells. Unfired ones can be ejected with the release button.", defaultValueIndex = 0, saveValue = true)]
        public static bool breakActionsEjectOnlyFired = false;

        [ModOptionOrder(11)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Spawn workbench and locker", tooltip = "If enabled, the locker and workbench will spawn in the home", defaultValueIndex = 1, saveValue = true)]
        public static bool spawnWorkbenchAndLocker
        {
            get => _spawnWorkbenchAndLocker;
            set
            {
                _spawnWorkbenchAndLocker = value;
                if (_spawnWorkbenchAndLocker)
                    HomeAdjustments.local.SpawnWorkbenchAndLocker();
                else if (HomeAdjustments.local.workbenchAndLocker != null)
                    Object.Destroy(HomeAdjustments.local.workbenchAndLocker);
            }
        }
        private static bool _spawnWorkbenchAndLocker;

        [ModOptionOrder(12)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Hide update notifications", tooltip = "Hide popup notifications for updates.", defaultValueIndex = 0, saveValue = true)]
        public static bool hideUpdateNotifications = false;

        [ModOptionOrder(13)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Spawn Axon Body 3", tooltip = "")]
        public static bool spawnAxonBodyThree
        {
            get { return true; }
            set
            {
                if (axonBodyThree != null)
                {
                    Object.Destroy(axonBodyThree);
                }
                else if (Player.local != null && Player.local.creature != null)
                {
                    Catalog.InstantiateAsync("Ghetto05.FirearmFrameworkV2.AxonBodyThree", Player.local.transform.position, Player.local.transform.rotation, Player.local.transform, axon =>
                    {
                        axonBodyThree = axon;
                        axon.transform.SetParent(Player.currentCreature.ragdoll.bones?.FirstOrDefault(cb => cb.mesh.gameObject.name.Equals("Spine1_Mesh"))?.mesh);
                        axon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    }, "AxonBodyThree");
                }
            }
        }
        private static GameObject axonBodyThree;

        #endregion

        #region Clothing
        
        #region NVG Offsets

        private static float _nvgForwardOffset;
        [ModOptionOrder(1)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "NVG Forward Offset", tooltip = "Offsets all NVGs forwards.", saveValue = true, defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.possibleNvgOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
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
        [ModOption(name = "NVG Upward Offset", tooltip = "Offsets all NVGs upwards.", saveValue = true, defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.possibleNvgOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
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
        [ModOption(name = "NVG Sideways Offset", tooltip = "Offsets all NVGs sideways.", saveValue = true, defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.possibleNvgOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
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

        #region Cheats
        
        [ModOptionOrder(1)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Incapacitate hit creatures", tooltip = "If enabled, shooting a creature in the torso will prevent them from standing up. May be mistaken for the creature dying.", defaultValueIndex = 3, saveValue = true, valueSourceName = nameof(FirearmsSettingsValues.incapacitateOnTorsoShotTimers), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float incapacitateOnTorsoShot;

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

        [ModOptionOrder(6)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "No recoil", tooltip = "Disables all recoil.", saveValue = true, defaultValueIndex = 0)]
        public static bool noRecoil = false;
        
        #endregion Cheats

        #region Gore

        [ModOptionOrder(1)]
        [ModOptionCategory("Gore", 15)]
        [ModOption(name = "Disable gore", tooltip = "If enabled, there will be no blood effects or dismemberment.", defaultValueIndex = 0, saveValue = true)]
        public static bool disableGore = false;

        [ModOptionOrder(2)]
        [ModOptionCategory("Gore", 15)]
        [ModOption(name = "Disable blood splatters", tooltip = "If enabled, there will be no blood effects on walls from the bullets penetrating.", defaultValueIndex = 0, saveValue = true)]
        public static bool disableBloodSpatters = false;

        [ModOptionOrder(3)]
        [ModOptionCategory("Gore", 15)]
        [ModOption(name = "Blood splatter life time multiplier", tooltip = "Allows you to change how long blood splatters stay.", defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.zeroToOneModifier), valueSourceType = typeof(FirearmsSettingsValues), saveValue = true)]
        public static float bloodSplatterLifetimeMultiplier;

        [ModOptionOrder(4)]
        [ModOptionCategory("Gore", 15)]
        [ModOption(name = "Blood splatter size multiplier", tooltip = "Allows you to change how large blood splatters are.", defaultValueIndex = 20, valueSourceName = nameof(FirearmsSettingsValues.zeroToFiveModifier), valueSourceType = typeof(FirearmsSettingsValues), saveValue = true)]
        public static float bloodSplatterSizeMultiplier;

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
                if (_spawnLiam)
                    HomeAdjustments.local.SpawnLiam();
                else if (HomeAdjustments.local.liam != null)
                    Object.Destroy(HomeAdjustments.local.liam);
            }
        }
        private static bool _spawnLiam;

        [ModOptionOrder(3)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Save guns as prebuilts", tooltip = "Only for development. Saves any gun in the locker with the prebuilt setup.", defaultValueIndex = 0, saveValue = true)]
        public static bool saveAsPrebuilt;
        
        #endregion Debug
        
        #endregion Settings

        #region Save folder

        public static readonly string SaveFolderName = "!GhettosFirearmSDKv2Saves";
        
        public static string GetSaveFolderPath()
        {
            return FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, SaveFolderName);
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
        #endregion
    }
}
