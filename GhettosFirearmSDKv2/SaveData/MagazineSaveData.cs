using System;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class MagazineSaveData : ContentCustomData
    {
        public string ItemID;
        public string[] Contents;

        public void ApplyToMagazine(Magazine magazine)
        {
            if (Contents == null || Contents.Length == 0)
            {
                magazine.loadable = true;
                magazine.InvokeLoadFinished();
                return;
            }
            ApplyToMagazineRecurve(Contents.Length - 1, magazine, Contents.CloneJson());
        }
        
        public void ApplyToMagazine(StripperClip clip)
        {
            if (Contents == null || Contents.Length == 0)
            {
                clip.loadable = true;
                return;
            }
            ApplyToClipRecurve(Contents.Length - 1, clip, Contents.CloneJson());
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
                Util.SpawnItem(con[index], "Magazine save data",cartridge =>
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
        
        private void ApplyToClipRecurve(int index, StripperClip clip, string[] con)
        {
            if (index < 0)
            {
                clip.loadable = true;
                return;
            }
            try
            {
                Util.SpawnItem(con[index], "Clip save data", cartridge =>
                {
                    clip.InsertRound(cartridge.GetComponent<Cartridge>(), true, true, false);
                    ApplyToClipRecurve(index - 1, clip, con);
                }, clip.transform.position + Vector3.up * 3, null, null, false);
            }
            catch (Exception)
            {
                Debug.Log("Error clip: " + clip);
                Debug.Log($"Contents: {con}");
                Debug.Log($"Index: {index}");
                Debug.Log($"Contents length: {con.Length}");
                Debug.Log($"Cartridge: {Catalog.GetData<ItemData>(con[index]).id}");
            }
        }

        public void GetContentsFromMagazine(Magazine magazine)
        {
            if (magazine == null || magazine.cartridges == null) return;
            Contents = new string[magazine.cartridges.Count];
            for (var i = 0; i < magazine.cartridges.Count; i++)
            {
                var car = magazine.cartridges[i];
                Contents[i] = car.item.itemId;
            }
        }

        public void GetContentsFromClip(StripperClip clip)
        {
            if (clip == null || clip.loadedCartridges == null)
                return;
            Contents = new string[clip.loadedCartridges.Count];
            for (var i = 0; i < clip.loadedCartridges.Count; i++)
            {
                var car = clip.loadedCartridges[i];
                Contents[i] = car.item.itemId;
            }
        }

        public void CloneTo(MagazineSaveData data)
        {
            data.ItemID = ItemID;
            data.Contents = new string[Contents.Length];
            Contents.CopyTo(data.Contents, 0);
        }

        public void Clear()
        {
            ItemID = null;
            Contents = null;
        }
    }
}
