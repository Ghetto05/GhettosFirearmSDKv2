using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class AmmunitionPouch : MonoBehaviour
    {
        public Holder holder;
        public Item pouchItem;
        public PouchSaveData savedData;

        private void Awake()
        {
            StartCoroutine(delayedLoad());
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            pouchItem.OnHeldActionEvent += PouchItem_OnHeldActionEvent;
        }

        private void PouchItem_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart) Reset();
        }

        private void Holder_UnSnapped(Item item)
        {
            SpawnSavedItem();
        }

        private void Holder_Snapped(Item item)
        {
            if (string.IsNullOrEmpty(savedData.itemID)) SaveItem();
        }

        IEnumerator delayedLoad()
        {
            yield return new WaitForSeconds(0.5f);
            if (pouchItem.TryGetCustomData(out savedData))
            {
                SpawnSavedItem();
            }
            else
            {
                savedData = new PouchSaveData();
                pouchItem.AddCustomData(savedData);
            }
        }

        IEnumerator delayedSnap(Item item)
        {
            yield return new WaitForSeconds(0.05f);
            holder.Snap(item);
        }

        public void SaveItem()
        {
            pouchItem.RemoveCustomData<PouchSaveData>();
            savedData = new PouchSaveData();
            if (holder.items.Count < 1)
            {
                savedData.itemID = null;
                savedData.savedMagazineData = null;
                return;
            }
            savedData.itemID = holder.items[0].itemId;
            if (holder.items[0].TryGetComponent(out Magazine mag))
            {
                savedData.savedMagazineData = new MagazineSaveData();
                savedData.savedMagazineData.GetContentsFromMagazine(mag);
            }
            pouchItem.AddCustomData(savedData);
        }

        public void SpawnSavedItem()
        {
            if (savedData == null || string.IsNullOrEmpty(savedData.itemID)) return;
            Catalog.GetData<ItemData>(savedData.itemID).SpawnAsync(newItem =>
            {
                if (newItem.TryGetComponent(out Magazine mag) && savedData.savedMagazineData != null)
                {
                    MagazineSaveData magSave = new MagazineSaveData();
                    magSave.contents = savedData.savedMagazineData.contents;
                    newItem.AddCustomData(magSave);
                }
                StartCoroutine(delayedSnap(newItem));
            }, transform.position, transform.rotation);
        }

        public void Reset()
        {
            savedData = new PouchSaveData();
            holder.UnSnapOne();
            pouchItem.RemoveCustomData<PouchSaveData>();
            pouchItem.AddCustomData(savedData);
        }
    }
}