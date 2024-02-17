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
        public Handle lastHeldHandle;
        bool setup = false;
        bool nextUnsnapIsCleaning = false;
        FirearmBase lastFirearm;
        bool spawning = false;

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            spawnedItems = new List<Item>();
            containedItems = new List<string>();

            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            pouchItem.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;

            holder.data.maxQuantity = 9999999;

            for (int i = 0; i < 1000; i++)
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
                if (!spawnedItems.Contains(i))
                {
                    nextUnsnapIsCleaning = true;
                    holder.UnSnap(i, true);
                }
            }

            Handle h = Player.local.GetHand(Handle.dominantHand).ragdollHand.grabbedHandle;
            if (h != null && h.item is Item item)
            {
                if (lastHeldHandle != h)
                {
                    if (h.GetComponentInParent<AttachmentFirearm>() != null)
                    {
                        lastFirearm = h.GetComponentInParent<AttachmentFirearm>();
                    }
                    else
                    {
                        lastFirearm = item.GetComponent<Firearm>();
                    }

                    if (lastFirearm != null && lastFirearm.defaultAmmoItem.IsNullOrEmptyOrWhitespace() && item.GetComponentInChildren<AttachmentFirearm>() is AttachmentFirearm ff) lastFirearm = ff;
                }

                if (lastFirearm != null)
                {
                    if (!containedItems.Contains(lastFirearm.defaultAmmoItem) && !lastFirearm.defaultAmmoItem.IsNullOrEmptyOrWhitespace())
                    {
                        containedItems.Add(lastFirearm.defaultAmmoItem);
                        spawning = true;
                        Catalog.GetData<ItemData>(lastFirearm.defaultAmmoItem).SpawnAsync(newItem =>
                        {
                            item.disallowDespawn = true;
                            StartCoroutine(DelayedSnap(newItem));
                            spawnedItems.Add(newItem);
                        });
                    }
                    else if (!lastFirearm.defaultAmmoItem.IsNullOrEmptyOrWhitespace() && !spawning)
                    {
                        GetById(lastFirearm.defaultAmmoItem);
                    }
                }
                lastHeldHandle = h;
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
            item.Hide(false);
            if (nextUnsnapIsCleaning)
            {
                nextUnsnapIsCleaning = false;
                return;
            }
            if (!setup) return;
            item.disallowDespawn = false;
            Util.IgnoreCollision(gameObject, item.gameObject, false);
            spawnedItems.Remove(item);
            if (item.TryGetComponent(out Firearm f)) return;
            spawning = true;
            Catalog.GetData<ItemData>(item.data.id).SpawnAsync(newItem =>
            {
                item.disallowDespawn = true;
                StartCoroutine(DelayedSnap(newItem));
                spawnedItems.Add(newItem);
            });
        }

        private void Holder_Snapped(Item item)
        {
            item.Hide(true);
            if (!setup) return;
            Util.IgnoreCollision(gameObject, item.gameObject, true);
        }

        IEnumerator DelayedSnap(Item item)
        {
            yield return new WaitForSeconds(0.05f);
            holder.Snap(item, true);
            spawning = false;
        }

        private void UpdateAllLightVolumeReceivers(LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
        {
            foreach (LightVolumeReceiver lvr in GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != pouchItem.lightVolumeReceiver))
            {
                Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
            }
        }
    }
}