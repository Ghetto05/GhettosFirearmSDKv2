using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class ItemMetaData : ItemModule
    {
        public enum ItemTypes
        {
            Firearm,
            [EnumDescription("Prebuilt firearm")]
            PrebuiltFirearm,
            Cartridge,
            Magazine,
            Melee,
            Clothing,
            Tool,
            Grenade,
            [EnumDescription("Stripper clip")]
            StripperClip
        }

        public enum DataFireModes
        {
            Safe,
            Semi,
            Burst,
            Auto,
            [EnumDescription("Multi shot")]
            MultiShot
        }

        public enum FirearmActions
        {
            [EnumDescription("Closed bolt")]
            ClosedBolt,
            [EnumDescription("Open bolt")]
            OpenBolt,
            [EnumDescription("Pump action")]
            PumpAction,
            [EnumDescription("Bolt action")]
            BoltAction,
            [EnumDescription("Break action")]
            BreakAction,
            [EnumDescription("Chamber loader")]
            ChamberLoader,
            Revolver,
            Minigun,
            [EnumDescription("Lever action")]
            LeverAction,
            Spoon,
            Pin,
            [EnumDescription("Pull cord")]
            PullCord,
            [EnumDescription("Safety cap")]
            SafetyCap,
            [EnumDescription("Timed detonator")]
            TimedDetonator,
            [EnumDescription("Impact detonator")]
            ImpactDetonator,
            [EnumDescription("Firearm/launcher mount")]
            LauncherMount
        }

        public enum FirearmClasses
        {
            [EnumDescription("Miscellaneous")]
            Misc,
            Derringer,
            Pistol,
            Revolver,
            [EnumDescription("Submachine gun")]
            SMG,
            [EnumDescription("Personal defense weapon")]
            PDW,
            Shotgun,
            Carbine,
            [EnumDescription("Assault rifle")]
            AssaultRifle,
            [EnumDescription("Battle rifle")]
            BattleRifle,
            [EnumDescription("Designated marksmen rifle")]
            DMR,
            Rifle,
            [EnumDescription("Anti-material rifle")]
            AntiMaterialRifle,
            [EnumDescription("Machine gun")]
            MachineGun,
            Ordnance,
            [EnumDescription("Flint lock")]
            FlintLock
        }

        public enum Eras
        {
            Victorian, //1800 - 1900
            [EnumDescription("Wild West")]
            WildWest, //1900 - 1914
            [EnumDescription("The Great War")]
            TheGreatWar, //1914 - 1918
            Interwar, //1918 - 1938
            [EnumDescription("World War II")]
            WorldWarTwo, //1938 - 1945
            [EnumDescription("Early Cold War")]
            EarlyColdWar, //1945 - 1970
            [EnumDescription("Late Cold War")]
            LateColdWar, //1970 - 1991
            [EnumDescription("War On Terror")]
            WarOnTerror //1991 - 2024
        }

        public ItemTypes Types;
        public FirearmClasses[] FirearmClass;
        private bool _categorySet;
        public string Category;
        public FirearmActions[] Actions = [];
        public DataFireModes[] FireModes = [];
        public string[] FireRates = [];
        public string[] Calibers = [];
        public CaliberCapacityData[] CaliberCapacityDatas = [];
        public string[] MagazineTypes = [];

        public string[] CountryOfOrigin = [];
        public Eras[] Era = [];
        public string[] YearOfIntroduction = [];
        public string[] Manufacturer = [];
        public string[] Designer = [];

/*
 
{
    "$type": "GhettosFirearmSDKv2.ItemMetaData, GhettosFirearmSDKv2",
    "Types": "Firearm",
    "Category": "Some Category",
    "Actions": [ "ClosedBolt" ],
    "FireModes": [ "Safe" ],
    "FireRates": [ "Rate1", "Rate2" ],
    "CaliberCapacityDatas": [ {"Caliber": "5.56x45mm","Capactiy": 30} ],
    "MagazineTypes": [ "STANAG" ],
    "CountryOfOrigin": [ "USA" ],
    "Era": [ "Victorian" ],
    "YearOfIntroduction": [ "1890" ],
    "Manufacturer": [ "Colt" ],
    "Designer": [ "Some Designer" ]
}
*/

/* for cartridges
{
    "$type": "GhettosFirearmSDKv2.ItemMetaData, GhettosFirearmSDKv2",
    "Types": "Cartridge",
    "CountryOfOrigin": [ "xxxxxxxx" ],
    "YearOfIntroduction": [ "xxxxxxx" ],
    "Designer": [ "xxxxxxxxx" ]
}
*/

        public class CaliberCapacityData
        {
            public string Caliber;
            public int Capacity;
        }

        public override void OnItemDataRefresh(ItemData data)
        {
            base.OnItemDataRefresh(data);

            if (!_categorySet)
            {
                Category = data.category;
                _categorySet = true;
            }
            
            try
            {
                switch (Types)
                {
                    case ItemTypes.Firearm:
                        GenerateFirearmDescription();
                        break;

                    case ItemTypes.PrebuiltFirearm:
                        break;

                    case ItemTypes.Cartridge:
                        GenerateCartridgeDescription();
                        break;

                    case ItemTypes.StripperClip:
                    case ItemTypes.Magazine:
                        GenerateMagazineDescription();
                        break;

                    case ItemTypes.Melee:
                        break;

                    case ItemTypes.Clothing:
                        break;

                    case ItemTypes.Tool:
                        break;

                    case ItemTypes.Grenade:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                Debug.LogError($"Failed to generate meta data description for {data.id}!");
                throw;
            }
        }

        private void GenerateFirearmDescription()
        {
            var builder = new StringBuilder();
            
            Util.AddInfoToBuilder("Firearm class", FirearmClass?.Select(x => x.GetName()), builder);
            Util.AddInfoToBuilder("Actions", Actions?.Select(x => x.GetName()), builder);
            Util.AddInfoToBuilder("Fire modes", FireModes?.Select(x => x.GetName()), builder);
            Util.AddInfoToBuilder("Fire rates", FireRates?.Select(x => x.Equals("0") ? "manual/single shot" : x), builder);
            Util.AddInfoToBuilder("Calibers", Calibers, builder);
            Util.AddInfoToBuilder("Magazine types", MagazineTypes, builder);
            Util.AddInfoToBuilder("Manufacturers", Manufacturer, builder);
            Util.AddInfoToBuilder("Country of origin", CountryOfOrigin, builder);
            Util.AddInfoToBuilder("Eras", Era?.Select(x => x.GetName()), builder);
            Util.AddInfoToBuilder("Year of introduction", YearOfIntroduction, builder);
            Util.AddInfoToBuilder("Designer", Designer, builder);
            
            itemData.description = builder.ToString();
        }

        private void GenerateMagazineDescription()
        {
            var builder = new StringBuilder();
            
            Util.AddInfoToBuilder("Type", MagazineTypes, builder);
            Util.AddInfoToBuilder("Calibers/capacities", CaliberCapacityDatas.Select(x => $"{x.Caliber} ({x.Capacity} rounds)"), builder);
            
            itemData.description = builder.ToString();
        }

        private void GenerateCartridgeDescription()
        {
            var builder = new StringBuilder();
            
            Util.AddInfoToBuilder("Country of origin", CountryOfOrigin, builder);
            Util.AddInfoToBuilder("Year of introduction", YearOfIntroduction, builder);
            Util.AddInfoToBuilder("Designer", Designer, builder);
            builder.AppendLine();

            itemData.description = builder.ToString();
            
            GetProjectileDataString();
        }

        private void GetProjectileDataString()
        {
            try
            {
                Addressables.LoadAssetAsync<GameObject>(itemData.prefabAddress).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (handle.Result.GetComponent<ProjectileData>() is { } data)
                            itemData.description += $"\n{data}";
                        else
                            Debug.LogWarning($"No projectile data component found on root object of {itemData.id}! Please make sure it is added to the root, not a child object!");

                        Addressables.Release(handle);
                    }
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't load prefab for {itemData.id}!\n{e}");
            }
        }
    }
}