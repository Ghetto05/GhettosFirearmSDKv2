using System;
using UnityEngine;
using ThunderRoad;
using GhettosFirearmSDKv2.SaveData;
using System.Collections.Generic;

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
    }
}
