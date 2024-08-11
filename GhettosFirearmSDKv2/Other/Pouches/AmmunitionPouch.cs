using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AmmunitionPouch : MonoBehaviour
    {
        public Holder holder;
        public Item pouchItem;
        public PouchSaveData SavedData;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            pouchItem.OnHeldActionEvent += PouchItem_OnHeldActionEvent;
            pouchItem.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;

            if (pouchItem.TryGetCustomData(out SavedData))
            {
                SpawnSavedItem();
            }
            else
            {
                SavedData = new PouchSaveData();
                pouchItem.AddCustomData(SavedData);
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
            if (string.IsNullOrEmpty(SavedData.ItemID)) SaveItem();
            Util.IgnoreCollision(gameObject, item.gameObject, true);
        }

        public void SaveItem()
        {
            pouchItem.RemoveCustomData<PouchSaveData>();
            SavedData = new PouchSaveData();
            if (holder.items.Count < 1)
            {
                return;
            }
            SavedData.ItemID = holder.items[0].data.id;
            SavedData.DataList = holder.items[0].contentCustomData.CloneJson();
            pouchItem.AddCustomData(SavedData);
        }

        public void SpawnSavedItem()
        {
            if (SavedData == null || string.IsNullOrEmpty(SavedData.ItemID)) return;
            Util.SpawnItem(SavedData.ItemID, "Ammunition Pouch", newItem =>
            {
                if (newItem.TryGetComponent(out Magazine mag))
                {
                    mag.OnLoadFinished += Mag_onLoadFinished;
                }
                else holder.Snap(newItem);
            }, transform.position, transform.rotation, null, true, SavedData.DataList.CloneJson());
        }

        private void Mag_onLoadFinished(Magazine mag)
        {
            holder.Snap(mag.item);
        }

        public void Reset()
        {
            SavedData = new PouchSaveData();
            holder.UnSnapOne();
            pouchItem.RemoveCustomData<PouchSaveData>();
            pouchItem.AddCustomData(SavedData);
        }

        private void UpdateAllLightVolumeReceivers(LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
        {
            foreach (var lvr in GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != pouchItem.lightVolumeReceiver))
            {
                Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
            }
        }
    }
}