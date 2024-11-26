using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class ImprovedLazyPouch : MonoBehaviour
    {
        public Holder holder;
        public Item pouchItem;

        private FirearmBase _lastHeldFirearm;

        private bool _initialized;
        private bool _nextUnsnapIsClear;

        private void Start()
        {
            pouchItem.OnDespawnEvent += PouchItemOnOnDespawnEvent;
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            pouchItem.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;

            holder.data.maxQuantity = 1;

            _initialized = true;
        }

        private void PouchItemOnOnDespawnEvent(EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
                return;

            holder.Snapped -= Holder_Snapped;
            holder.UnSnapped -= Holder_UnSnapped;
            pouchItem.lightVolumeReceiver.onVolumeChangeEvent -= UpdateAllLightVolumeReceivers;
            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            var held = GetHeldFirearm();
            var last = _lastHeldFirearm;
            _lastHeldFirearm = held;

            if (!held || held.SavedAmmoData == null)
                return;

            if (held != last && !held)
            {
                _nextUnsnapIsClear = true;
                var i = holder.UnSnapOne(true);
                i?.Despawn();
            }

            SpawnItem();
        }

        private FirearmBase GetHeldFirearm()
        {
            var heldHandleDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.grabbedHandle;
            var heldHandleNonDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.otherHand.grabbedHandle;
            
            FirearmBase firearm;

            if (!heldHandleDominant || heldHandleNonDominant)
                return null;
            
            FirearmBase heldDominant = heldHandleDominant.GetComponentInParent<Firearm>();
            FirearmBase heldAttachmentDominant = heldHandleDominant.GetComponentInParent<AttachmentFirearm>();
            FirearmBase heldOffhand = heldHandleNonDominant.GetComponentInParent<Firearm>();
            FirearmBase heldAttachmentOffhand = heldHandleNonDominant.GetComponentInParent<AttachmentFirearm>();
            
            if (heldHandleDominant && heldDominant && !heldAttachmentDominant)
            {
                firearm = heldDominant;
            }
            else if (heldHandleDominant && heldAttachmentDominant)
            {
                firearm = heldAttachmentDominant;
            }
            else if (heldHandleNonDominant && heldOffhand && !heldAttachmentOffhand)
            {
                firearm = heldOffhand;
            }
            else if (heldHandleNonDominant && heldAttachmentOffhand)
            {
                firearm = heldAttachmentOffhand;
            }
            else
            {
                firearm = null;
            }
            
            return firearm;
        }

        public void SpawnItem()
        {
            Util.SpawnItem(_lastHeldFirearm.SavedAmmoData.ID, "Improved Lazy Pouch", i =>
            {
                i.physicBody.isKinematic = true;
                holder.Snap(i);
            }, transform.position - Vector3.down * 5);
        }

        private void Holder_UnSnapped(Item item)
        {
            if (_nextUnsnapIsClear)
            {
                _nextUnsnapIsClear = false;
                return;
            }

            SpawnItem();
        }

        private void Holder_Snapped(Item item)
        {
            if (item.data.id != _lastHeldFirearm?.SavedAmmoData?.Value.ItemID)
            {
                holder.UnSnap(item, true);
                item.Despawn();
            }
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