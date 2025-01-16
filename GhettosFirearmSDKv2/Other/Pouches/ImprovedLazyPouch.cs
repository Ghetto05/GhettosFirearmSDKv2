using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class ImprovedLazyPouch : MonoBehaviour
    {
        public static event SavedAmmoItemChanged SavedAmmoItemChangedEvent;
        public delegate void SavedAmmoItemChanged(FirearmBase firearm);

        public static void InvokeAmmoItemChanged(FirearmBase firearm)
        {
            SavedAmmoItemChangedEvent?.Invoke(firearm);
        }

        public Holder holder;
        public Item pouchItem;

        private FirearmBase _lastFrameHeldFirearm;
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
            SavedAmmoItemChangedEvent += OnSavedAmmoItemChangedEvent;
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
            SavedAmmoItemChangedEvent -= OnSavedAmmoItemChangedEvent;
            pouchItem.lightVolumeReceiver.onVolumeChangeEvent -= UpdateAllLightVolumeReceivers;
            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized || !Player.local?.creature?.ragdoll)
                return;

            var held = GetHeldFirearm();
            var last = _lastFrameHeldFirearm;
            _lastFrameHeldFirearm = held;

            if (held)
                _lastHeldFirearm = held;

            if (!held || held.GetAmmoItem() == null)
                return;

            if (held != last)
            {
                _nextUnsnapIsClear = true;
                var i = holder.UnSnapOne();
                i?.Despawn();
                SpawnItem();
            }
        }

        private FirearmBase GetHeldFirearm()
        {
            var heldHandleDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.grabbedHandle;
            var heldHandleNonDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.otherHand.grabbedHandle;
            
            FirearmBase firearm;

            if (!heldHandleDominant && !heldHandleNonDominant)
                return null;
            
            FirearmBase heldDominant = heldHandleDominant?.GetComponentInParent<Firearm>();
            FirearmBase heldAttachmentDominant = heldHandleDominant?.GetComponentInParent<AttachmentFirearm>();
            FirearmBase heldOffhand = heldHandleNonDominant?.GetComponentInParent<Firearm>();
            FirearmBase heldAttachmentOffhand = heldHandleNonDominant?.GetComponentInParent<AttachmentFirearm>();
            
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
            Util.SpawnItem(_lastHeldFirearm?.GetAmmoItem()?.ItemID, "Improved Lazy Pouch", i =>
            {
                i.physicBody.isKinematic = true;
                holder.Snap(i, true);
            }, transform.position - Vector3.down * 5, null, null, true, _lastHeldFirearm?.GetAmmoItem()?.CustomData?.CloneJson().ToList());
            _nextUnsnapIsClear = false;
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
            if (!Util.CompareSubstitutedIDs(item.data.id, _lastHeldFirearm?.GetAmmoItem()?.ItemID))
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

        private void OnSavedAmmoItemChangedEvent(FirearmBase firearm)
        {
            if (_lastFrameHeldFirearm != firearm)
                return;
            _lastFrameHeldFirearm = null;
        }
    }
}