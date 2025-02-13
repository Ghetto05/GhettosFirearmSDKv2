using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2
{
    public class CartridgeHolder : MonoBehaviour
    {
        [FormerlySerializedAs("firearm"), SerializeField, SerializeReference]
        public IAttachmentManager manager;
        public Attachment attachment;

        public int slot;
        public string caliber;
        public Collider mountCollider;
        public ChamberLoader chamberLoader;

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
            if (manager != null)
            {
                manager.OnCollision += Collision;
                manager.Item.OnGrabEvent += Firearm_OnGrabEvent;
            }
            else if (attachment != null)
            {
                attachment.attachmentPoint.parentManager.OnCollision += Collision;
                attachment.attachmentPoint.parentManager.Item.OnGrabEvent += Firearm_OnGrabEvent;
            }

            SaveNodeValueCartridgeData save = null;
            if (attachment != null && attachment.Node.TryGetValue("CartridgeHolder" + slot, out SaveNodeValueCartridgeData value))
            {
                save = value;
            }
            else if (manager != null && manager.SaveData.FirearmNode.TryGetValue("CartridgeHolder" + slot, out SaveNodeValueCartridgeData value2))
            {
                save = value2;
            }

            if (save != null)
            {
                Util.SpawnItem(save.Value.ItemId, $"[Cartridge holder - Firearm: {manager?.Item?.itemId ?? "--"} Attachment: {attachment?.Data.id ?? "--"} Slot: {slot}]", cartridge =>
                {
                    var c = cartridge.GetComponent<Cartridge>();
                    save.Value.Apply(c);
                    InsertRound(c, true);
                }, transform.position + Vector3.up * 3);
            }
        }

        private void Firearm_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            UpdateCartridgePositions();
        }

        private void Collision(Collision collision)
        {
            if (collision.collider.GetComponentInParent<Cartridge>() is { } car && Util.CheckForCollisionWithThisCollider(collision, mountCollider))
            {
                InsertRound(car, false);
            }
        }

        public Cartridge EjectRound()
        {
            var loaded = loadedCartridge;
            if (loaded != null)
            {
                Util.PlayRandomAudioSource(roundEjectSounds);
                loaded.ToggleCollision(true);
                if (manager != null)
                    Util.DelayIgnoreCollision(manager.Transform.gameObject, loaded.gameObject, false, 1f, loaded.item);
                if (attachment != null)
                    Util.DelayIgnoreCollision(attachment.gameObject, loaded.gameObject, false, 1f, loaded.item);
                loaded.loaded = false;
                loaded.GetComponent<Rigidbody>().isKinematic = false;
                loaded.item.DisallowDespawn = false;
                loaded.transform.parent = null;
                loaded.item.OnGrabEvent -= Item_OnGrabEvent;
                loadedCartridge = null;

                if (attachment != null)
                    attachment.Node.RemoveValue("CartridgeHolder" + slot);
                else if (manager != null)
                    manager.SaveData.FirearmNode.RemoveValue("CartridgeHolder" + slot);
            }
            UpdateCartridgePositions();
            return loaded;
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
                if (manager != null)
                    Util.IgnoreCollision(c.gameObject, manager.Transform.gameObject, true);
                if (attachment != null)
                    Util.IgnoreCollision(c.gameObject, attachment.gameObject, true);
                if (!silent) Util.PlayRandomAudioSource(roundInsertSounds);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = transform;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;

                c.item.OnGrabEvent += Item_OnGrabEvent;

                FirearmSaveData.AttachmentTreeNode target = null;
                if (attachment != null)
                    target = attachment.Node;
                else if (manager != null)
                    target = manager.SaveData.FirearmNode;
                
                if (target != null)
                {
                    target.GetOrAddValue("CartridgeHolder" + slot, new SaveNodeValueCartridgeData()).Value = new CartridgeSaveData(c.item.itemId, c.Fired);
                }
            }
            UpdateCartridgePositions();
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            var c = EjectRound();
            var success = chamberLoader?.TryLoad(c) ?? false;
            if (success && manager is FirearmBase { bolt: BoltSemiautomatic { caught: true } bolt })
                bolt.TryRelease();
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
