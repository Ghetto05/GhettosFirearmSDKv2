using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class GunLockerSaveData : CustomData
{
    public string DisplayName;
    public string ItemId;
    public string Category;
    public List<ContentCustomData> DataList;

    public static List<ItemData> allPrebuilts = new();

    public static List<string> GetAllCategories()
    {
        var list = new List<string>();
        foreach (var data in Catalog.GetDataList<GunLockerSaveData>())
        {
            if (!list.Contains(data.Category) && !data.Category.Equals("Prebuilts")) list.Add(data.Category);
        }
        return list;
    }

    public void GenerateItem()
    {
        if (Category.Equals("Prebuilts"))
        {
            try
            {
                Settings.CreateSaveFolder();
                var itemData = Catalog.GetData<ItemData>(ItemId).CloneJson();
                itemData.modules = itemData.modules.CloneJson();
                itemData.id = id;
                itemData.description = "Prebuilt version of the " + itemData.displayName + ".";
                itemData.displayName = DisplayName;
                itemData.category = "Firearm Prebuilts";
                itemData.iconAddress = id + ".Icon";
                var loader = new PrebuiltLoader
                             {
                                 OriginalId = ItemId,
                                 PrebuiltId = id,
                                 itemData = itemData
                             };
                itemData.modules.Add(loader);
                Catalog.LoadJson(itemData, JsonConvert.SerializeObject(itemData, Catalog.jsonSerializerSettings), Settings.GetSaveFolderPath() + "\\Saves\\" + id + ".json", "!GhettosFirearmSDKv2_Saves");
                allPrebuilts.Add(itemData);
            }
            catch (Exception)
            {
                Debug.Log($"Couldn't load prebuilt {DisplayName}!");
            }
        }
        base.OnCatalogRefresh();
    }
}