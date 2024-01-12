using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PrebuiltEquipmentSpawner : ItemModule
    {
        public string prebuiltId;

        private Holder _equipmentSlot;
        
        public override void OnItemLoaded(Item item)
        {
            if (item.holder != null)
                _equipmentSlot = item.holder;
            else
                item.OnSnapEvent += OnSnap;
        }

        private void OnSnap(Holder holder)
        {
            item.OnSnapEvent -= OnSnap;
            _equipmentSlot = holder;
            
            _equipmentSlot.UnSnap(item, true, false);
            Catalog.GetData<ItemData>(prebuiltId).SpawnAsync(gun =>
            {
                _equipmentSlot.Snap(gun, true, false);
            }, _equipmentSlot.transform.position + Vector3.up * 20, null, null, false);
            
            item.Despawn();
        }
    }
}