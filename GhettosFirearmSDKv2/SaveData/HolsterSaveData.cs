using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class HolsterSaveData : ContentCustomData
    {
        public Dictionary<string, string> itemIDs;
        public Dictionary<string, List<ContentCustomData>> dataLists;
    }
}
