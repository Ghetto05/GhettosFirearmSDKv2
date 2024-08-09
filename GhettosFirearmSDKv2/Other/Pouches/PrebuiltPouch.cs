using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class PrebuiltPouch : MonoBehaviour
    {
        public Item pouchItem;
        public Holder holder;
        private bool spawning = false;

        private void Start()
        {
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
        }

        private void Holder_UnSnapped(Item item)
        {
            item.Hide(false);
        }

        private void Holder_Snapped(Item item)
        {
            item.Hide(true);
        }

        private void Update()
        {
            if (!spawning && holder.items.Count > 0 && (holder.items[0] == null || holder.items[0].holder == null))
            {
                holder.items.Clear();
                holder.currentQuantity = 0;
            }

            if (!spawning && holder.HasSlotFree())
            {
                spawning = true;
                try
                {
                    ItemData data = GunLockerSaveData.allPrebuilts[Random.Range(0, GunLockerSaveData.allPrebuilts.Count)];
                    data.SpawnAsync(item =>
                    {
                        item.physicBody.rigidBody.isKinematic = true;
                        StartCoroutine(DelayedSnap(item));
                    }, holder.transform.position);
                }
                catch (System.Exception)
                {
                    spawning = false;
                }
            }
        }

        private IEnumerator DelayedSnap(Item item)
        {
            yield return new WaitForSeconds(Settings.invokeTime + 0.06f);
            if (item != null) holder.Snap(item);
            spawning = false;
        }
    }
}
