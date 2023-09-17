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
            if (!spawning && holder.HasSlotFree())
            {
                spawning = true;
                try
                {
                    GunLockerSaveData.allPrebuilts[Random.Range(0, GunLockerSaveData.allPrebuilts.Count)].SpawnAsync(item =>
                    {
                        holder.Snap(item);
                        spawning = false;
                    });
                }
                catch (System.Exception)
                {
                    spawning = false;
                }
            }
        }
    }
}
