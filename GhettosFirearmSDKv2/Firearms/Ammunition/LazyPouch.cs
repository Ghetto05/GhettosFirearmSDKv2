using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class LazyPouch : MonoBehaviour
    {
        public Holder holder;
        public Item pouchItem;
        public List<Item> spawnedItems;
        public List<string> containedItems;
        public Item lastHeldItem;
        bool restock = true;
        bool setup = false;

        private void Start()
        {
            spawnedItems = new List<Item>();
            containedItems = new List<string>();

            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;

            holder.data.maxQuantity = 9999999;

            for (int i = 0; i < 100; i++)
            {
                holder.slots.Add(holder.transform);
            }

            setup = true;
        }

        private void Update()
        {
            if (!setup || Player.local == null || Player.local.creature == null || Player.local.creature.ragdoll == null) return;

            foreach (Item i in holder.items.ToArray())
            {
                if (!spawnedItems.Contains(i)) holder.UnSnap(i, true);
            }

            Handle h = Player.local.GetHand(Handle.dominantHand).ragdollHand.grabbedHandle;
            if (h != null && h.item is Item item)
            {
                if (lastHeldItem != item && item.TryGetComponent(out Firearm f))
                {
                    if (!containedItems.Contains(f.defaultAmmoItem) && !f.defaultAmmoItem.IsNullOrEmptyOrWhitespace())
                    {
                        containedItems.Add(f.defaultAmmoItem);
                        Catalog.GetData<ItemData>(f.defaultAmmoItem).SpawnAsync(newItem =>
                        {
                            item.disallowDespawn = true;
                            StartCoroutine(DelayedSnap(newItem));
                            spawnedItems.Add(newItem);
                        });
                    }
                    else if (!f.defaultAmmoItem.IsNullOrEmptyOrWhitespace())
                    {
                        GetById(f.defaultAmmoItem);
                    }
                }
                lastHeldItem = item;
            }
        }

        public void GetById(string id)
        {
            bool searching = true;
            foreach (Item i in spawnedItems)
            {
                if (searching && i.data.id.Equals(id))
                {
                    searching = false;
                    holder.items.Remove(i);
                    holder.items.Add(i);
                }
            }
        }

        private void Holder_UnSnapped(Item item)
        {
            if (!setup) return;
            if (!restock) return;
            item.disallowDespawn = false;
            Util.IgnoreCollision(gameObject, item.gameObject, false);
            spawnedItems.Remove(item);
            Catalog.GetData<ItemData>(item.data.id).SpawnAsync(newItem =>
            {
                item.disallowDespawn = true;
                StartCoroutine(DelayedSnap(newItem));
                spawnedItems.Add(newItem);
            });
        }

        private void Holder_Snapped(Item item)
        {
            if (!setup) return;
            Util.IgnoreCollision(gameObject, item.gameObject, true);
        }

        IEnumerator DelayedSnap(Item item)
        {
            yield return new WaitForSeconds(0.05f);
            holder.Snap(item, true);
        }
    }
}