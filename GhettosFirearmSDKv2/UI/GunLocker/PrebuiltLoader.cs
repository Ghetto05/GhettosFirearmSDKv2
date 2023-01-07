using UnityEngine;
using ThunderRoad;
using GhettosFirearmSDKv2.SaveData;

namespace GhettosFirearmSDKv2
{
    public class PrebuiltLoader : ItemModule
    {
        public string prebuiltId;
        public string originalId;

        public override void OnItemLoaded(Item item)
        {
            if (item.contentCustomData == null || item.contentCustomData.Count == 0) item.contentCustomData = Catalog.GetData<GunLockerSaveData>(prebuiltId).dataList.CloneJson();
            base.OnItemLoaded(item);
        }
    }
}