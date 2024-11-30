using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThunderRoad;
using UnityEngine;

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
            [EnumDescription("Stripper clip")]
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
            ImpactDetonator
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
        private bool _categorySet;
        public string Category;
        public FirearmActions[] Actions = [];
        public FirearmBase.FireModes[] FireModes = [];
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
    "$type": "GhettosFirearmSDKv2.ItemMetaData, Assembly-CSharp",
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

        private void AddInfo(string name, IEnumerable<string> data, StringBuilder builder)
        {
            builder.AppendLine($"{name}: {string.Join(", ", data)}");
        }

        private void GenerateFirearmDescription()
        {
            var builder = new StringBuilder();
            
            AddInfo("Actions", Actions.Select(x => x.GetName()), builder);
            AddInfo("Fire modes", FireModes.Select(x => x.GetName()), builder);
            AddInfo("Fire rates", FireRates, builder);
            AddInfo("Calibers", Calibers, builder);
            AddInfo("Magazine types", MagazineTypes, builder);
            AddInfo("Manufacturers", Manufacturer, builder);
            AddInfo("Country of origin", CountryOfOrigin, builder);
            AddInfo("Eras", Era.Select(x => x.GetName()), builder);
            AddInfo("Year of introduction", YearOfIntroduction, builder);
            AddInfo("Designer", Designer, builder);
            
            itemData.description = builder.ToString();
        }

        private void GenerateMagazineDescription()
        {
            var builder = new StringBuilder();
            
            AddInfo("Type", MagazineTypes, builder);
            AddInfo("Calibers/capacities", CaliberCapacityDatas.Select(x => $"{x.Caliber} ({x.Capacity} rounds)"), builder);
            
            itemData.description = builder.ToString();
        }
    }
}