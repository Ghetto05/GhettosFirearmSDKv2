using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class LootTableOverride : CustomData
    {
        public string targetTableId;
        public Drop[] drops;

        public void ApplyToTable()
        {
            if (Catalog.GetData<LootTable>(targetTableId, false) is { } table)
            {
                var tableDrops = new List<LootTable.Drop>();
                foreach (var drop in drops)
                {
                    var tableDrop = new LootTable.Drop();
                    tableDrop.reference = drop.reference;
                    tableDrop.referenceID = drop.referenceID;
                    tableDrop.probabilityWeight = drop.probabilityWeight;
                    tableDrops.Add(tableDrop);
                }

                //table.drops = tableDrops; //ToDo
                //table.CalculateWeight(); //ToDo
            }
        }

        public class Drop
        {
            public string referenceID;
            public LootTable.Drop.Reference reference;
            public float probabilityWeight;
        }
    }
}