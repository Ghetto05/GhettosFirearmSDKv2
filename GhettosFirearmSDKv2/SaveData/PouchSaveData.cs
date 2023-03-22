using ThunderRoad;
using System.Collections.Generic;

namespace GhettosFirearmSDKv2
{
    public class PouchSaveData : ContentCustomData
    {
        public string itemID;
        public MagazineSaveData savedMagazineData;
        public List<ContentCustomData> dataList;
    }
}
