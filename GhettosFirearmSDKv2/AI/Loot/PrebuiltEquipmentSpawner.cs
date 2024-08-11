using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PrebuiltEquipmentSpawner : ItemModule
    {
        public string prebuiltId;

        private Holder _equipmentSlot;
        private Item _item;
        
        public override void OnItemLoaded(Item item)
        {
            _item = item;
            if (_item.holder != null)
                _equipmentSlot = _item.holder;
            else
                _item.OnSnapEvent += OnSnap;
            
            _item.StartCoroutine(Spawn());
        }

        private void OnSnap(Holder holder)
        {
            _item.OnSnapEvent -= OnSnap;
            _equipmentSlot = holder;
        }

        private IEnumerator Spawn()
        {
            yield return new WaitForSeconds(2f);

            var prebuilt = Catalog.GetData<GunLockerSaveData>(prebuiltId);
            var data = (ItemData)Catalog.GetData<ItemData>(prebuilt.itemId).Clone();
            var loader = (PrebuiltLoader)data.modules.FirstOrDefault(d => d.GetType() == typeof(PrebuiltLoader));
            if (loader != null)
                loader.forced = true;
            data.SpawnAsync(gun =>
            {
                Debug.Log("FUCK ME");
                //if (_equipmentSlot != null)
                    //_equipmentSlot.Snap(gun, true, false);
                _item.Despawn(2f);
            }, _item.transform.position + Vector3.up * 20, null, null, false, prebuilt.dataList.CloneJson());
            
            if (_equipmentSlot != null)
                _equipmentSlot.UnSnap(_item, true);
        }
    }
}