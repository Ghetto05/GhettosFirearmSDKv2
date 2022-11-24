using System.Collections;
using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;

namespace GhettosFirearmSDKv2
{
    public class AmmoModule : ItemModule
    {
        public string category;
        public string caliber;
        public string variant;
        public string description;
        public bool hidden = false;

        public static List<string> AllCategories()
        {
            List<string> list = new List<string>();
            foreach (ItemData data in Catalog.GetDataList<ItemData>())
            {
                if (data.HasModule<AmmoModule>())
                {
                    AmmoModule module = data.GetModule<AmmoModule>();
                    if (!list.Contains(module.category)) list.Add(module.category);
                }
            }
            return list;
        }

        public static List<string> AllCalibersOfCategory(string wantedCategory)
        {
            List<string> list = new List<string>();
            foreach (ItemData data in Catalog.GetDataList<ItemData>())
            {
                if (data.HasModule<AmmoModule>())
                {
                    AmmoModule module = data.GetModule<AmmoModule>();
                    if (module.category.Remove(0, 5).Equals(wantedCategory) && !list.Contains(module.caliber)) list.Add(module.caliber);
                }
            }
            return list;
        }

        public static List<string> AllVariantsOfCaliber(string wantedCaliber)
        {
            List<string> list = new List<string>();
            foreach (ItemData data in Catalog.GetDataList<ItemData>())
            {
                if (data.HasModule<AmmoModule>())
                {
                    AmmoModule module = data.GetModule<AmmoModule>();
                    if (!module.hidden && module.caliber.Remove(0, 5).Equals(wantedCaliber) && !list.Contains(module.variant)) list.Add(module.variant);
                    else if (module.caliber.Remove(0, 5).Equals(wantedCaliber)) Debug.Log($"Duplicate cartridge variant found! Category: {module.category}, Caliber: {module.caliber}, Variant: {module.variant}");
                }
            }
            return list;
        }

        public static string GetCartridgeItemId(string wantedCategory, string wantedCaliber, string wantedVariant)
        {
            foreach (ItemData data in Catalog.GetDataList<ItemData>())
            {
                if (data.HasModule<AmmoModule>())
                {
                    AmmoModule module = data.GetModule<AmmoModule>();
                    if (module.category.Remove(0, 5).Equals(wantedCategory) && module.caliber.Remove(0, 5).Equals(wantedCaliber) && module.variant.Remove(0, 5).Equals(wantedVariant)) return data.id;
                }
            }
            return string.Empty;
        }

        public static string GetCaliberCategory(string wantedCaliber)
        {
            foreach (ItemData data in Catalog.GetDataList<ItemData>())
            {
                if (data.HasModule<AmmoModule>())
                {
                    AmmoModule module = data.GetModule<AmmoModule>();
                    if (module.caliber.Remove(0, 5).Equals(wantedCaliber)) return module.category;
                }
            }
            return string.Empty;
        }

        public static string GetDescription(string itemId)
        {
            return Catalog.GetData<ItemData>(itemId).GetModule<AmmoModule>().description;
        }
    }
}