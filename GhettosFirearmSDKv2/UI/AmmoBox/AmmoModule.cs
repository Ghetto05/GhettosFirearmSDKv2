using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AmmoModule : ItemModule
{
    public string Category;
    public string Caliber;
    public string Variant;
    public string Description;
    public bool Hidden = false;

    public static List<string> AllCategories()
    {
        var list = new List<string>();
        foreach (var data in Catalog.GetDataList<ItemData>())
        {
            if (data.HasModule<AmmoModule>())
            {
                var module = data.GetModule<AmmoModule>();
                if (!list.Contains(module.Category))
                {
                    list.Add(module.Category);
                }
            }
        }
        return list;
    }

    public static List<string> AllCalibersOfCategory(string wantedCategory)
    {
        var list = new List<string>();
        foreach (var data in Catalog.GetDataList<ItemData>())
        {
            if (data.HasModule<AmmoModule>())
            {
                var module = data.GetModule<AmmoModule>();
                if (module.Category.Equals(wantedCategory) && !list.Contains(module.Caliber))
                {
                    list.Add(module.Caliber);
                }
            }
        }
        return list;
    }

    public static List<string> AllVariantsOfCaliber(string wantedCaliber)
    {
        var list = new List<string>();
        foreach (var data in Catalog.GetDataList<ItemData>())
        {
            if (data.HasModule<AmmoModule>())
            {
                var module = data.GetModule<AmmoModule>();
                if (!module.Hidden && module.Caliber.Equals(wantedCaliber) && !list.Contains(module.Variant))
                {
                    list.Add(module.Variant);
                }
                else if (module.Caliber.Equals(wantedCaliber))
                {
                    Debug.Log($"Duplicate cartridge variant found! Category: {module.Category}, Caliber: {module.Caliber}, Variant: {module.Variant}");
                }
            }
        }
        return list;
    }

    public static string GetCartridgeItemId(string wantedCategory, string wantedCaliber, string wantedVariant)
    {
        foreach (var data in Catalog.GetDataList<ItemData>())
        {
            if (data.HasModule<AmmoModule>())
            {
                var module = data.GetModule<AmmoModule>();
                if (module.Category.Equals(wantedCategory) && module.Caliber.Equals(wantedCaliber) && module.Variant.Remove(0, 5).Equals(wantedVariant))
                {
                    return data.id;
                }
            }
        }
        return string.Empty;
    }

    public static string GetCaliberCategory(string wantedCaliber)
    {
        foreach (var data in Catalog.GetDataList<ItemData>())
        {
            if (data.HasModule<AmmoModule>())
            {
                var module = data.GetModule<AmmoModule>();
                if (module.Caliber.Equals(wantedCaliber))
                {
                    return module.Category;
                }
            }
        }
        return string.Empty;
    }
}