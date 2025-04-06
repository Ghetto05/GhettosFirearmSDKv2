using System.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class PrebuiltLoader : ItemModule
{
    public string PrebuiltId;
    public string OriginalId;
    public bool Forced;

    public override void OnItemLoaded(Item loadedItem)
    {
        if (!loadedItem.TryGetCustomData(out FirearmSaveData _) || Forced)
        {
            loadedItem.OverrideCustomData(Catalog.GetData<GunLockerSaveData>(PrebuiltId).DataList.CloneJson().ToList());
        }
    }
}