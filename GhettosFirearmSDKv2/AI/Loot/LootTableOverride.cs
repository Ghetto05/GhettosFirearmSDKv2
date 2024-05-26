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
            if (Catalog.GetData<LootTable>(targetTableId, false) is LootTable table)
            {
                List<LootTable.Drop> tableDrops = new List<LootTable.Drop>();
                foreach (Drop drop in drops)
                {
                    LootTable.Drop tableDrop = new LootTable.Drop();
                    tableDrop.reference = drop.reference;
                    tableDrop.referenceID = drop.referenceID;
                    tableDrop.probabilityWeight = drop.probabilityWeight;
                    tableDrops.Add(tableDrop);
                }

                table.drops = tableDrops;
                table.CalculateWeight();
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