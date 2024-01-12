using System.Linq;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class PrebuiltLoader : ItemModule
    {
        public string prebuiltId;
        public string originalId;

        public override void OnItemLoaded(Item item)
        {
            if (!item.TryGetCustomData(out FirearmSaveData fsd))
                item.AddCustomData(Catalog.GetData<GunLockerSaveData>(prebuiltId).dataList.Where(d => d.GetType() == typeof(FirearmSaveData)).FirstOrDefault()?.CloneJson());
            base.OnItemLoaded(item);
        }
    }
}