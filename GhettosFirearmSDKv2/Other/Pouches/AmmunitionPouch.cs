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

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            pouchItem.OnHeldActionEvent += PouchItem_OnHeldActionEvent;

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

        private void PouchItem_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart) Reset();
        }

        private void Holder_UnSnapped(Item item)
        {
            item.Hide(false);
            SpawnSavedItem();
            Util.IgnoreCollision(gameObject, item.gameObject, false);
        }

        private void Holder_Snapped(Item item)
        {
            item.Hide(true);
            if (item.GetComponent<Firearm>()) holder.UnSnap(item);
            if (string.IsNullOrEmpty(savedData.itemID)) SaveItem();
            Util.IgnoreCollision(gameObject, item.gameObject, true);
        }

        public void SaveItem()
        {
            pouchItem.RemoveCustomData<PouchSaveData>();
            savedData = new PouchSaveData();
            if (holder.items.Count < 1)
            {
                return;
            }
            savedData.itemID = holder.items[0].data.id;
            savedData.dataList = holder.items[0].contentCustomData.CloneJson();
            pouchItem.AddCustomData(savedData);
        }

        public void SpawnSavedItem()
        {
            if (savedData == null || string.IsNullOrEmpty(savedData.itemID)) return;
            Catalog.GetData<ItemData>(savedData.itemID)?.SpawnAsync(newItem =>
            {
                if (newItem.TryGetComponent(out Magazine mag))
                {
                    mag.onLoadFinished += Mag_onLoadFinished;
                }
                else holder.Snap(newItem);
            }, transform.position, transform.rotation, null, true, savedData.dataList.CloneJson());
        }

        private void Mag_onLoadFinished(Magazine mag)
        {
            holder.Snap(mag.item);
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