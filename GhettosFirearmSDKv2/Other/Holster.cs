using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Holster : MonoBehaviour
    {
        private HolsterSaveData _data;
        
        public Holder holder;
        public Item item;

        public void Start()
        {
            holder.Snapped += HolderOnSnapped;
            holder.UnSnapped += HolderOnUnSnapped;
            item.OnDespawnEvent += ItemOnOnDespawnEvent;
            
            if (!item.TryGetCustomData(out _data))
            {
                _data = new HolsterSaveData();
                _data.itemIDs = new Dictionary<string, string>();
                _data.dataLists = new Dictionary<string, List<ContentCustomData>>();
                item.AddCustomData(_data);
            }

            if (_data.itemIDs.TryGetValue(holder.name, out string id))
            {
                Util.SpawnItem(id, $"[Holster {item.itemId} - Holder {holder.name}, Item {id}", i =>
                {
                    holder.Snap(i);
                }, holder.transform.position, null, null, true, _data.dataLists[holder.name]);
            }
        }

        private void ItemOnOnDespawnEvent(EventTime eventtime)
        {
            if (eventtime == EventTime.OnStart)
            {
                holder.Snapped -= HolderOnSnapped;
                holder.UnSnapped -= HolderOnUnSnapped;
                item.OnDespawnEvent -= ItemOnOnDespawnEvent;
            }
        }

        private void HolderOnUnSnapped(Item item1)
        {
            _data.itemIDs.Remove(holder.name);
            _data.dataLists.Remove(holder.name);
        }

        private void HolderOnSnapped(Item item1)
        {
            _data.itemIDs.Add(holder.name, item1.data.id);
            _data.dataLists.Add(holder.name, item1.contentCustomData);
        }
    }
}