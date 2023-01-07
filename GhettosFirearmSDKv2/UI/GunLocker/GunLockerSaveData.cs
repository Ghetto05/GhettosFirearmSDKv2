using System;
using UnityEngine;
using ThunderRoad;
using GhettosFirearmSDKv2.SaveData;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace GhettosFirearmSDKv2
{
    public class GunLockerSaveData : CustomData
    {
        public string displayName;
        public string itemId;
        public string category;
        public List<ContentCustomData> dataList;

        public static List<string> GetAllCategories()
        {
            List<string> list = new List<string>();
            foreach (GunLockerSaveData data in Catalog.GetDataList<GunLockerSaveData>())
            {
                if (!list.Contains(data.category) && !data.category.Equals("Prebuilts")) list.Add(data.category);
            }
            return list;
        }

        public override void OnCatalogRefresh()
        {
            if (category.Equals("Prebuilts"))
            {
                Settings_LevelModule.GenerateGunSaveFolders();
                ItemData itemData = (ItemData)Catalog.GetData<ItemData>(itemId).Clone();
                itemData.modules = itemData.modules.CloneJson();
                itemData.id = "Item" + id;
                itemData.description = "Prebuilt version of the " + itemData.displayName + ".";
                itemData.displayName = displayName;
                PrebuiltLoader loader = new PrebuiltLoader();
                loader.originalId = itemId;
                loader.prebuiltId = id;
                loader.itemData = itemData;
                itemData.modules.Add(loader);
                itemData.category = "Firearm Prebuilts";
                itemData.iconAddress = id + ".Icon";
                Catalog.LoadJson(itemData, JsonConvert.SerializeObject(itemData, Catalog.jsonSerializerSettings), FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, "GhettosFirearmSDKv2Saves\\Saves\\" + "Item" + id + ".json"), "GhettosFirearmSDKv2_Saves");
            }
            base.OnCatalogRefresh();
        }
    }
}
