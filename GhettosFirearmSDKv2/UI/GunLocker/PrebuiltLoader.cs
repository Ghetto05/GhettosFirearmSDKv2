using System.Linq;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class PrebuiltLoader : ItemModule
    {
        public string prebuiltId;
        public string originalId;
        public bool forced;

        public override void OnItemLoaded(Item item)
        {
            if (!item.TryGetCustomData(out FirearmSaveData fsd) || forced)
                item.OverrideCustomData(Catalog.GetData<GunLockerSaveData>(prebuiltId).dataList.CloneJson().Where(d => d.GetType() == typeof(FirearmSaveData)).ToList());
        }
    }
}