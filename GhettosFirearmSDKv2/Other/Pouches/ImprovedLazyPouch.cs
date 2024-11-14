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

        private bool _initialized;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            pouchItem.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;

            holder.data.maxQuantity = 1000;

            for (var i = 0; i < 1000; i++)
            {
                holder.slots.Add(holder.transform);
            }

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
                return;
        }

        private FirearmBase GetHeldFirearm()
        {
            var heldDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.grabbedHandle;
            var heldNonDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.otherHand.grabbedHandle;
            
            FirearmBase firearm;
            
            if (heldDominant && heldDominant.GetComponentInParent<Firearm>() && !heldDominant.GetComponentInParent<AttachmentFirearm>())
            {
                firearm = heldDominant.GetComponentInParent<Firearm>();
            }
            else if (heldDominant && heldDominant.GetComponentInParent<AttachmentFirearm>())
            {
                firearm = heldDominant.GetComponentInParent<AttachmentFirearm>();
            }
            else if (heldNonDominant && heldNonDominant.GetComponentInParent<Firearm>() && !heldNonDominant.GetComponentInParent<AttachmentFirearm>())
            {
                firearm = heldNonDominant.GetComponentInParent<Firearm>();
            }
            else if (heldNonDominant && heldNonDominant.GetComponentInParent<AttachmentFirearm>())
            {
                firearm = heldNonDominant.GetComponentInParent<AttachmentFirearm>();
            }
            else
            {
                firearm = null;
            }
            
            return firearm;
        }

        public void GetById(string id)
        {
            
        }

        private void Holder_UnSnapped(Item item)
        {
            
        }

        private void Holder_Snapped(Item item)
        {
            
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