using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class MagazineSizeIncreaser : MonoBehaviour
    {
        public Attachment attachment;
        public bool useDeltaInsteadOfFixed;
        public int targetSize;
        public int deltaSize;
        private int _previousSize;
        private Magazine _magazine;

        private void Awake()
        {
            if (attachment.initialized) Attachment_OnDelayedAttachEvent();
            else attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            attachment.OnDetachEvent += Attachment_OnDetachEvent;
        }

        private void Attachment_OnDetachEvent(bool despawnDetach)
        {
            if (_magazine != null) Revert();
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            if (attachment.attachmentPoint.ConnectedManager is FirearmBase { magazineWell.currentMagazine: { } mag })
            {
                _magazine = mag;
                Apply();
            }
        }

        public void Apply()
        {
            if (_magazine == null) return;
            _previousSize = _magazine.maximumCapacity;
            if (useDeltaInsteadOfFixed)
            {
                _magazine.maximumCapacity += deltaSize;
            }
            else
            {
                _magazine.maximumCapacity = targetSize;
            }
        }

        public void Revert()
        {
            if (_magazine == null) return;
            _magazine.maximumCapacity = _previousSize;
            //foreach (Cartridge c in magazine.cartridges)
            //{
            //    if (magazine.cartridges.IndexOf(c) >= previousSize && c.item != null) c.item.Despawn();
            //}
            while (_magazine.cartridges.Count > _magazine.maximumCapacity)
            {
                var c = _magazine.ConsumeRound();
                c.item.Despawn();
            }
        }
    }
}
