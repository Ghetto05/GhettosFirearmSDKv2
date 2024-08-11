using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class CartridgeHolder : MonoBehaviour
    {
        public Firearm firearm;
        public Attachment attachment;

        public int slot;
        public string caliber;
        public Collider mountCollider;

        public List<AudioSource> roundInsertSounds;
        public List<AudioSource> roundEjectSounds;

        public Cartridge loadedCartridge;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
            Invoke(nameof(UpdateCartridgePositions), Settings.invokeTime * 5);
        }

        public void InvokedStart()
        {
            if (firearm != null)
            {
                firearm.OnCollisionEvent += OnCollisionEvent;
                firearm.item.OnGrabEvent += Firearm_OnGrabEvent;
            }
            else if (attachment != null)
            {
                attachment.attachmentPoint.parentFirearm.OnCollisionEvent += OnCollisionEvent;
                attachment.attachmentPoint.parentFirearm.item.OnGrabEvent += Firearm_OnGrabEvent;
            }

            var id = "";
            if (attachment != null && attachment.Node.TryGetValue("CartridgeHolder" + slot, out SaveNodeValueString value))
            {
                id = value.Value;
            }
            else if (firearm != null && firearm.SaveData.FirearmNode.TryGetValue("CartridgeHolder" + slot, out SaveNodeValueString value2))
            {
                id = value2.Value;
            }

            if (!id.Equals(""))
            {
                Util.SpawnItem(id, $"[Cartridge holder - Firearm: {firearm?.item?.itemId ?? "--"} Attachment: {attachment?.Data.id ?? "--"} Slot: {slot}]", cartridge =>
                {
                    InsertRound(cartridge.GetComponent<Cartridge>(), true);
                }, transform.position + Vector3.up * 3);
            }
        }

        private void Firearm_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            UpdateCartridgePositions();
        }

        private void OnCollisionEvent(Collision collision)
        {
            if (collision.collider.GetComponentInParent<Cartridge>() is { } car && Util.CheckForCollisionWithThisCollider(collision, mountCollider))
            {
                InsertRound(car, false);
            }
        }

        public Cartridge EjectRound()
        {
            if (loadedCartridge != null)
            {
                Util.PlayRandomAudioSource(roundEjectSounds);
                loadedCartridge.ToggleCollision(true);
                if (firearm != null)
                    Util.DelayIgnoreCollision(firearm.gameObject, loadedCartridge.gameObject, false, 1f, loadedCartridge.item);
                if (attachment != null)
                    Util.DelayIgnoreCollision(attachment.gameObject, loadedCartridge.gameObject, false, 1f, loadedCartridge.item);
                loadedCartridge.loaded = false;
                loadedCartridge.GetComponent<Rigidbody>().isKinematic = false;
                loadedCartridge.item.DisallowDespawn = false;
                loadedCartridge.transform.parent = null;
                loadedCartridge.item.OnGrabEvent -= Item_OnGrabEvent;
                loadedCartridge = null;

                if (attachment != null) attachment.Node.RemoveValue("CartridgeHolder" + slot);
                else if (firearm != null) firearm.SaveData.FirearmNode.RemoveValue("CartridgeHolder" + slot);
            }
            UpdateCartridgePositions();
            return loadedCartridge;
        }

        public void InsertRound(Cartridge c, bool silent)
        {
            if (loadedCartridge == null && Util.AllowLoadCartridge(c, caliber) && !c.loaded)
            {
                c.item.DisallowDespawn = true;
                c.loaded = true;
                c.ToggleCollision(false);
                loadedCartridge = c;
                c.UngrabAll();
                if (firearm != null)
                    Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                if (attachment != null)
                    Util.IgnoreCollision(c.gameObject, attachment.gameObject, true);
                if (!silent) Util.PlayRandomAudioSource(roundInsertSounds);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = transform;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;

                c.item.OnGrabEvent += Item_OnGrabEvent;

                if (attachment != null) attachment.Node.GetOrAddValue("CartridgeHolder" + slot, new SaveNodeValueString()).Value = c.item.itemId;
                else if (firearm != null) firearm.SaveData.FirearmNode.GetOrAddValue("CartridgeHolder" + slot, new SaveNodeValueString()).Value = c.item.itemId;
            }
            UpdateCartridgePositions();
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            EjectRound();
        }

        private void UpdateCartridgePositions()
        {
            if (loadedCartridge != null && loadedCartridge.transform != null)
            {
                loadedCartridge.transform.parent = transform;
                loadedCartridge.transform.localPosition = Vector3.zero;
                loadedCartridge.transform.localEulerAngles = Vector3.zero;
            }
        }
    }
}
