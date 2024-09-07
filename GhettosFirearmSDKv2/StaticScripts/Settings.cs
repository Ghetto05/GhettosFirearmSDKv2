using System.IO;
using Newtonsoft.Json;
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
        public static float cartridgeEjectionTorque = 10f;
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
        [ModOption(name = "Firearm despawn time", tooltip = "Despawns any dropped firearms after set time. Disabled if set to 0. Note: Firearms will never despawn up until 10 seconds after having spawned in.", saveValue = true, valueSourceName = nameof(FirearmsSettingsValues.firearmDespawnTimeValues), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float firearmDespawnTime = 60f;

        [ModOptionOrder(3)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Long press (safety switch) time", tooltip = "Defines the amount of time you need to hold alternate use to switch fire modes.", saveValue = true)]
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
        [ModOption(name = "Trigger discipline timer", tooltip = "Defines the amount of time after which the index finger will move off the trigger after last pressing it.", saveValue = true, valueSourceName = nameof(FirearmsSettingsValues.triggerDisciplineTimers), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float triggerDisciplineTime = 3f;

        [ModOptionOrder(7)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Do blunt damage", tooltip = "If enabled, bullets will deal blunt damage rather than pierce damage. Intended for things like headbreaker. Has no impact on damage.", saveValue = true)]
        public static bool bulletsAreBlunt = false;

        [ModOptionOrder(8)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Play gas mask sounds", tooltip = "If enabled, wearing a gas mask will play a breathing sound",  saveValue = true)]
        public static bool playGasMaskSound = true;

        [ModOptionOrder(9)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Explosions dismember", tooltip = "If enabled, any ragdoll part in that is caught in the center of an explosion will be ripped off.", saveValue = true)]
        public static bool explosionsDismember = false;

        [ModOptionOrder(10)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Break actions only eject fired rounds", tooltip = "If enabled, break actions will only eject fired shells. Unfired ones can be ejected with the release button.", saveValue = true)]
        public static bool breakActionsEjectOnlyFired = false;

        [ModOptionOrder(11)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Spawn workbench and locker", tooltip = "If enabled, the locker and workbench will spawn in the home", saveValue = true)]
        public static bool SpawnWorkbenchAndLocker
        {
            get { return _spawnWorkbenchAndLocker; }
            set
            {
                _spawnWorkbenchAndLocker = value;
                if (_spawnWorkbenchAndLocker)
                    HomeAdjustments.local.SpawnWorkbenchAndLocker();
                else if (HomeAdjustments.local.WorkbenchAndLocker != null)
                    Object.Destroy(HomeAdjustments.local.WorkbenchAndLocker);
            }
        }

        private static bool _spawnWorkbenchAndLocker = true;

        [ModOptionOrder(12)]
        [ModOptionCategory("Settings", 1)]
        [ModOption(name = "Hide update notifications", tooltip = "Hide popup notifications for updates.", saveValue = true)]
        public static bool hideUpdateNotifications = false;

        #endregion

        #region Clothing
        
        #region NVG Offsets

        private static float _nvgForwardOffset;
        [ModOptionOrder(1)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "NVG Forward Offset", tooltip = "Offsets all NVGs forwards.", saveValue = true, valueSourceName = nameof(FirearmsSettingsValues.possibleNvgOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float NvgForwardOffset
        {
            get { return _nvgForwardOffset; }
            set 
            { 
                _nvgForwardOffset = value; 
                NvgAdjuster.UpdateAllOffsets();
            }
        }
        
        private static float _nvgUpwardOffset;
        [ModOptionOrder(2)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "NVG Upward Offset", tooltip = "Offsets all NVGs upwards.", saveValue = true, valueSourceName = nameof(FirearmsSettingsValues.possibleNvgOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float NvgUpwardOffset
        {
            get { return _nvgUpwardOffset; }
            set 
            { 
                _nvgUpwardOffset = value; 
                NvgAdjuster.UpdateAllOffsets();
            }
        }
        
        private static float _nvgSidewaysOffset;
        [ModOptionOrder(3)]
        [ModOptionCategory("NVG Offsets", 5)]
        [ModOption(name = "NVG Sideways Offset", tooltip = "Offsets all NVGs sideways.", saveValue = true, valueSourceName = nameof(FirearmsSettingsValues.possibleNvgOffsets), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float NvgSidewaysOffset
        {
            get { return _nvgSidewaysOffset; }
            set 
            { 
                _nvgSidewaysOffset = value; 
                NvgAdjuster.UpdateAllOffsets();
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
                NvgAdjuster.UpdateAllOffsets();
            }
        }

        #endregion
        
        #endregion

        #region Cheats
        
        [ModOptionOrder(1)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Incapacitate hit creatures", tooltip = "If enabled, shooting a creature in the torso will prevent them from standing up. May be mistaken for the creature dying.", saveValue = true, valueSourceName = nameof(FirearmsSettingsValues.incapacitateOnTorsoShotTimers), valueSourceType = typeof(FirearmsSettingsValues))]
        public static float incapacitateOnTorsoShot = 30;

        [ModOptionOrder(2)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Infinite ammo", tooltip = "If enabled, magazines will refill as they are used and chamber loaders will not use up the loaded cartridge.", saveValue = true)]
        public static bool infiniteAmmo = false;

        [ModOptionOrder(3)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Caliber checks", tooltip = "If disabled, any magazine or chamber can be loaded with any caliber.", saveValue = true)]
        public static bool doCaliberChecks = true;

        [ModOptionOrder(4)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Magazine type checks", tooltip = "If disabled, magazine can be loaded into any firearm.", saveValue = true)]
        public static bool doMagazineTypeChecks = true;

        [ModOptionOrder(5)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "Damage multiplier", tooltip = "Multiplies the damage done by projectiles.", saveValue = true)]
        public static float damageMultiplier = 1f;

        [ModOptionOrder(6)]
        [ModOptionCategory("Cheats", 10)]
        [ModOption(name = "No recoil", tooltip = "Disables all recoil.", saveValue = true)]
        public static bool noRecoil = false;
        
        #endregion Cheats

        #region Gore

        [ModOptionOrder(1)]
        [ModOptionCategory("Gore", 15)]
        [ModOption(name = "Disable gore", tooltip = "If enabled, there will be no blood effects or dismemberment.", saveValue = true)]
        public static bool disableGore = false;

        [ModOptionOrder(2)]
        [ModOptionCategory("Gore", 15)]
        [ModOption(name = "Disable blood splatters", tooltip = "If enabled, there will be no blood effects on walls from the bullets penetrating.", saveValue = true)]
        public static bool disableBloodSpatters = false;

        [ModOptionOrder(3)]
        [ModOptionCategory("Gore", 15)]
        [ModOptionFloatValues(0, 1, 0.5f)]
        [ModOption(name = "Blood splatter life time multiplier", tooltip = "Allows you to change how long blood splatters stay.", saveValue = true)]
        public static float bloodSplatterLifetimeMultiplier = 1f;

        [ModOptionOrder(4)]
        [ModOptionCategory("Gore", 15)]
        [ModOptionFloatValues(0, 5, 0.5f)]
        [ModOption(name = "Blood splatter size multiplier", tooltip = "Allows you to change how large blood splatters are.", saveValue = true)]
        public static float bloodSplatterSizeMultiplier = 1f;

        #endregion
        
        #region Debug
        
        [ModOptionOrder(1)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Display debug messages", tooltip = "Only for debugging use.", saveValue = true)]
        public static bool debugMode = false;

        [ModOptionOrder(2)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Spawn Liam", tooltip = "Spawn the blue guy.", saveValue = true)]
        public static bool SpawnLiam
        {
            get { return _spawnLiam; }
            set
            {
                _spawnLiam = value;
                if (_spawnLiam)
                    HomeAdjustments.local.SpawnLiam();
                else if (HomeAdjustments.local.Liam != null)
                    Object.Destroy(HomeAdjustments.local.Liam);
            }
        }

        private static bool _spawnLiam;

        [ModOptionOrder(3)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Save guns as prebuilts", tooltip = "Only for development. Saves any gun in the locker with the prebuilt setup.", saveValue = true)]
        public static bool saveAsPrebuilt = false;
        
        [ModOptionOrder(41)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Spawn Collider Layer Check Tool", tooltip = "Only for development.", saveValue = false, valueSourceName = nameof(FirearmsSettingsValues.spawnBooleanButton), valueSourceType = typeof(FirearmsSettingsValues))]
        public static bool SpawnColliderLayerChecker
        {
            get { return false; }
            set
            {
                _ = value;
                if (!Player.local || !Player.local.locomotion.allowMove)
                    return;
                Catalog.GetData<ItemData>("Ghetto05.FirearmSDKv2.ColliderLayerChecker").SpawnAsync(_ => { }, Player.local.transform.position + Vector3.up * 2);
            }
        }
        
        [ModOptionOrder(42)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Spawn Rail Type Check Tool", tooltip = "Only for development.", saveValue = false, valueSourceName = nameof(FirearmsSettingsValues.spawnBooleanButton), valueSourceType = typeof(FirearmsSettingsValues))]
        public static bool SpawnRailTypeChecker
        {
            get { return false; }
            set
            {
                _ = value;
                if (!Player.local || !Player.local.locomotion.allowMove)
                    return;
                Catalog.GetData<ItemData>("Ghetto05.FirearmSDKv2.RailTester").SpawnAsync(_ => { }, Player.local.transform.position + Vector3.up * 2);
            }
        }
        
        [ModOptionOrder(42)]
        [ModOptionCategory("Debug", 30)]
        [ModOption(name = "Spawn Attachment Validation Tool", tooltip = "Only for development.", saveValue = false, valueSourceName = nameof(FirearmsSettingsValues.spawnBooleanButton), valueSourceType = typeof(FirearmsSettingsValues))]
        public static bool SpawnAttachmentValidator
        {
            get { return false; }
            set
            {
                _ = value;
                if (!Player.local || !Player.local.locomotion.allowMove)
                    return;
                Catalog.GetData<ItemData>("Ghetto05.FirearmSDKv2.AttachmentValidator").SpawnAsync(_ => { }, Player.local.transform.position + Vector3.up * 2);
            }
        }
        
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
            var manifest = new ModManager.ModData
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
