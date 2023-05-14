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
            if (contents == null || contents.Length == 0)
            {
                magazine.loadable = true;
                return;
            }
            ApplyToMagazineRecurve(contents.Length - 1, magazine, contents.CloneJson());
        }

        private void ApplyToMagazineRecurve(int index, Magazine mag, string[] con)
        {
            if (index < 0)
            {
                mag.loadable = true;
                mag.InvokeLoadFinished();
                return;
            }
            try
            {
                Catalog.GetData<ItemData>(con[index]).SpawnAsync(cartridge =>
                {
                    mag.InsertRound(cartridge.GetComponent<Cartridge>(), true, true, false);
                    ApplyToMagazineRecurve(index - 1, mag, con);
                }, mag.transform.position + Vector3.up * 3, null, null, false);
            }
            catch (Exception)
            {
                Debug.Log("Error mag: " + mag);
                Debug.Log($"Contents: {con}");
                Debug.Log($"Index: {index}");
                Debug.Log($"Contents length: {con.Length}");
                Debug.Log($"Cartridge: {Catalog.GetData<ItemData>(con[index]).id}");
            }
        }

        public void GetContentsFromMagazine(Magazine magazine)
        {
            if (magazine == null || magazine.cartridges == null) return;
            contents = new string[magazine.cartridges.Count];
            for (int i = 0; i < magazine.cartridges.Count; i++)
            {
                Cartridge car = magazine.cartridges[i];
                contents[i] = car.item.itemId;
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
