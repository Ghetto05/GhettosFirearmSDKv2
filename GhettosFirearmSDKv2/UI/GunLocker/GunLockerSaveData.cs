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
                FirearmsSettings.CreateSaveFolder();
                ItemData itemData = (ItemData)Catalog.GetData<ItemData>(itemId).Clone();
                itemData.modules = itemData.modules.CloneJson();
                itemData.id = "Item" + id;
                itemData.description = "Prebuilt version of the " + itemData.displayName + ".";
                itemData.displayName = displayName;
                itemData.category = "Firearm Prebuilts";
                //itemData.iconAddress = id + ".Icon";
                PrebuiltLoader loader = new PrebuiltLoader
                {
                    originalId = itemId,
                    prebuiltId = id,
                    itemData = itemData
                };
                itemData.modules.Add(loader);
                Catalog.LoadJson(itemData, JsonConvert.SerializeObject(itemData, Catalog.jsonSerializerSettings), FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, FirearmsSettings.saveFolderName + "\\Saves\\" + "Item" + id + ".json"), "!GhettosFirearmSDKv2_Saves");
            }
            base.OnCatalogRefresh();
        }
    }
}
