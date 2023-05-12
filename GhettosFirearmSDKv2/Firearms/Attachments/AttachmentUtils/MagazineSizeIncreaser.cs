using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class MagazineSizeIncreaser : MonoBehaviour
    {
        public Attachment attachment;
        public bool useDeltaInsteadOfFixed;
        public int targetSize;
        public int deltaSize;
        private int previousSize;
        private Magazine magazine;

        private void Awake()
        {
            attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            if (magazine != null) Revert();
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            if (attachment.attachmentPoint.parentFirearm.magazineWell is MagazineWell well && well.currentMagazine is Magazine mag)
            {
                magazine = mag;
                Apply();
            }
        }

        public void Apply()
        {
            if (magazine == null) return;
            previousSize = magazine.maximumCapacity;
            if (useDeltaInsteadOfFixed)
            {
                magazine.maximumCapacity += deltaSize;
            }
            else
            {
                magazine.maximumCapacity = targetSize;
            }
        }

        public void Revert()
        {
            if (magazine == null) return;
            magazine.maximumCapacity = previousSize;
            foreach (Cartridge c in magazine.cartridges)
            {
                if (magazine.cartridges.IndexOf(c) >= previousSize) c.item.Despawn();
            }
        }
    }
}
