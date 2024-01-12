using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class HolderItemSpawner : MonoBehaviour
    {
        public string itemId;
        public Holder holder;
        
        private void Start()
        {
            if (!itemId.IsNullOrEmptyOrWhitespace() && Catalog.GetData<ItemData>(itemId) is ItemData data)
            {
                data.SpawnAsync(item =>
                {
                    holder.Snap(item);
                }, holder.transform.position, holder.transform.rotation);
            }
        }
    }
}
