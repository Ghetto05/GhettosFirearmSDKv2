using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class MagazineSaveData : ContentCustomData
    {
        public string itemID;
        public string[] contents;

        public void ApplyToMagazine(Magazine magazine)
        {
            if (contents == null) return;
            ApplyToMagazineRecurve(contents.Length - 1, magazine);
        }

        private void ApplyToMagazineRecurve(int index, Magazine mag)
        {
            if (index < 0) return;
            Catalog.GetData<ItemData>(contents[index]).SpawnAsync(cartridge =>
            {
                mag.InsertRound(cartridge.GetComponent<Cartridge>(), true, true);
                ApplyToMagazineRecurve(index - 1, mag);
            }, mag.transform.position + Vector3.up * 3, null, null, false);
        }

        public void GetContentsFromMagazine(Magazine magazine)
        {
            if (magazine == null || magazine.cartridges == null) return;
            contents = new string[magazine.cartridges.Count];
            foreach (Cartridge car in magazine.cartridges)
            {
                contents[magazine.cartridges.IndexOf(car)] = car.item.itemId;
            }
        }

        public void CloneTo(MagazineSaveData data)
        {
            data.itemID = itemID;
            data.contents = new string[contents.Length];
            contents.CopyTo(data.contents, 0);
        }

        public void Clear()
        {
            itemID = null;
            contents = null;
        }
    }
}
