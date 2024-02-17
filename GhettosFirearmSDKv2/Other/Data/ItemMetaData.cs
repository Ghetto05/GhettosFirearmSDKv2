using System;
using ThunderRoad;
using ThunderRoad.Reveal;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class ItemMetaData
    {
        public enum ItemType
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
        public enum Actions
        {
            ClosedBolt,
            OpenBolt,
            PumpAction,
            BoltAction,
            BreakAction,
            ChamberLoader,
            Revolver,
            Minigun
        }
        public enum Eras
        {
            Victorian, //1800 - 1900
            WildWest, //1900 - 1914
            TheGreatWar, //1914 - 1918
            Interwar, //1918 - 1938
            WorldWarII, //1938 - 1945
            EarlyColdWar, //1945 - 1970
            LateColdWar, //1970 - 1991
            WarOnTerror //1991 - 2024
        }

        public ItemType itemType;
        public string category;
        public string description;
        public Actions action;
        public FirearmBase.FireModes[] fireModes;
        public int[] fireRates;
        public string[] calibers;
        public string[] magazineTypes;
        public int capacity;
        public string countryOfOrigin;
        public Eras era;
        public int yearOfIntroduction;
    }
}