using System;
using UnityEngine;
using ThunderRoad;
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

        public static List<ItemData> allPrebuilts = new List<ItemData>();

        public static List<string> GetAllCategories()
        {
            List<string> list = new List<string>();
            foreach (GunLockerSaveData data in Catalog.GetDataList<GunLockerSaveData>())
            {
                if (!list.Contains(data.category) && !data.category.Equals("Prebuilts")) list.Add(data.category);
            }
            return list;
        }

        public void GenerateItem()
        {
            if (category.Equals("Prebuilts"))
            {
                try
                {
                    Settings.CreateSaveFolder();
                    ItemData itemData = Catalog.GetData<ItemData>(itemId).CloneJson();
                    itemData.modules = itemData.modules.CloneJson();
                    itemData.id = id;
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
                    Catalog.LoadJson(itemData, JsonConvert.SerializeObject(itemData, Catalog.jsonSerializerSettings), Settings.GetSaveFolderPath() + "\\Saves\\" + id + ".json", "!GhettosFirearmSDKv2_Saves");
                    allPrebuilts.Add(itemData);
                }
                catch (Exception)
                {
                    Debug.Log($"Couldn't load prebuilt {displayName}!");
                }
            }
            base.OnCatalogRefresh();
        }
    }
}
