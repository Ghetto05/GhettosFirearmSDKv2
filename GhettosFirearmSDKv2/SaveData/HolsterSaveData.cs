using System.Collections.Generic;
using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class HolsterSaveData : ContentCustomData
{
    public Dictionary<string, string> ItemIDs;
    public Dictionary<string, List<ContentCustomData>> DataLists;
}