using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class ItemMetaData : ItemModule
    {
        public enum ItemTypes
        {
            Firearm,
            PrebuiltFirearm,
            Cartridge,
            Magazine,
            Melee,
            Clothing,
            Tool,
            Grenade
        }

        public enum FirearmActions
        {
            ClosedBolt,
            OpenBolt,
            PumpAction,
            BoltAction,
            BreakAction,
            ChamberLoader,
            Revolver,
            Minigun,
            LeverAction,
            Spoon,
            Pin,
            PullCord,
            SafetyCap,
            TimedDetonator,
            ImpactDetonator
        }

        public enum Eras
        {
            Victorian, //1800 - 1900
            WildWest, //1900 - 1914
            TheGreatWar, //1914 - 1918
            Interwar, //1918 - 1938
            WorldWarTwo, //1938 - 1945
            EarlyColdWar, //1945 - 1970
            LateColdWar, //1970 - 1991
            WarOnTerror //1991 - 2024
        }

        public ItemTypes Type;
        public string Category;
        public FirearmActions[] Actions;
        public FirearmBase.FireModes[] FireModes;
        public string[] FireRates;
        public CaliberCapacityData[] CaliberCapacityDatas;
        public string[] MagazineTypes;

        public string[] CountryOfOrigin;
        public Eras[] Era;
        public string[] YearOfIntroduction;
        public string[] Manufacturer;
        public string[] Designer;

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
            public int Capactiy;
        }
    }
}