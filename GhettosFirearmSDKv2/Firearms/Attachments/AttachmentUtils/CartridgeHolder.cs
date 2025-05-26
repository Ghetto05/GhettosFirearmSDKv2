using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class CartridgeHolder : MonoBehaviour
{
    public IAttachmentManager ConnectedManager;
    public Attachment attachment;
    public AttachmentManager attachmentManager;
    public Firearm firearm;

    public int slot;
    public string caliber;
    public Collider mountCollider;
    public ChamberLoader chamberLoader;

    public List<AudioSource> roundInsertSounds;
    public List<AudioSource> roundEjectSounds;

    public Cartridge loadedCartridge;

    private void Start()
    {
        if (firearm)
        {
            ConnectedManager = firearm;
        }
        if (attachmentManager)
        {
            ConnectedManager = attachmentManager;
        }

        Invoke(nameof(InvokedStart), Settings.invokeTime);
        Invoke(nameof(UpdateCartridgePositions), Settings.invokeTime * 5);
    }

    public void InvokedStart()
    {
        if (ConnectedManager is not null)
        {
            ConnectedManager.OnCollision += Collision;
            ConnectedManager.Item.OnGrabEvent += Firearm_OnGrabEvent;
        }
        else if (attachment)
        {
            attachment.attachmentPoint.ConnectedManager.OnCollision += Collision;
            attachment.attachmentPoint.ConnectedManager.Item.OnGrabEvent += Firearm_OnGrabEvent;
        }

        SaveNodeValueCartridgeData save = null;
        if (attachment && attachment.Node.TryGetValue("CartridgeHolder" + slot, out SaveNodeValueCartridgeData value))
        {
            save = value;
        }
        else if (ConnectedManager is not null && ConnectedManager.SaveData.FirearmNode.TryGetValue("CartridgeHolder" + slot, out SaveNodeValueCartridgeData value2))
        {
            save = value2;
        }

        if (save is not null)
        {
            Util.SpawnItem(save.Value.ItemId, $"[Cartridge holder - Firearm: {ConnectedManager?.Item?.itemId ?? "--"} Attachment: {attachment?.Data.id ?? "--"} Slot: {slot}]", cartridge =>
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
        if (loaded)
        {
            Util.PlayRandomAudioSource(roundEjectSounds);
            loaded.ToggleCollision(true);
            if (ConnectedManager is not null)
            {
                Util.DelayIgnoreCollision(ConnectedManager.Transform.gameObject, loaded.gameObject, false, 1f, loaded.item);
            }
            if (attachment)
            {
                Util.DelayIgnoreCollision(attachment.gameObject, loaded.gameObject, false, 1f, loaded.item);
            }
            loaded.loaded = false;
            loaded.GetComponent<Rigidbody>().isKinematic = false;
            loaded.item.DisallowDespawn = false;
            loaded.transform.parent = null;
            loaded.item.OnGrabEvent -= Item_OnGrabEvent;
            loadedCartridge = null;

            if (attachment)
            {
                attachment.Node.RemoveValue("CartridgeHolder" + slot);
            }
            else if (ConnectedManager is not null)
            {
                ConnectedManager.SaveData.FirearmNode.RemoveValue("CartridgeHolder" + slot);
            }
        }
        UpdateCartridgePositions();
        return loaded;
    }

    public void InsertRound(Cartridge c, bool silent)
    {
        if (!loadedCartridge && Util.AllowLoadCartridge(c, caliber) && !c.loaded)
        {
            c.item.DisallowDespawn = true;
            c.loaded = true;
            c.ToggleCollision(false);
            loadedCartridge = c;
            c.UngrabAll();
            if (ConnectedManager is not null)
            {
                Util.IgnoreCollision(c.gameObject, ConnectedManager.Transform.gameObject, true);
            }
            if (attachment)
            {
                Util.IgnoreCollision(c.gameObject, attachment.gameObject, true);
            }
            if (!silent)
            {
                Util.PlayRandomAudioSource(roundInsertSounds);
            }
            c.GetComponent<Rigidbody>().isKinematic = true;
            c.transform.parent = transform;
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Vector3.zero;

            c.item.OnGrabEvent += Item_OnGrabEvent;

            FirearmSaveData.AttachmentTreeNode target = null;
            if (attachment)
            {
                target = attachment.Node;
            }
            else if (ConnectedManager is not null)
            {
                target = ConnectedManager.SaveData.FirearmNode;
            }

            if (target is not null)
            {
                target.GetOrAddValue("CartridgeHolder" + slot, new SaveNodeValueCartridgeData()).Value = new CartridgeSaveData(c.item.itemId, c.Fired, c.Failed);
            }
        }
        UpdateCartridgePositions();
    }

    private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
    {
        var c = EjectRound();
        var success = chamberLoader?.TryLoad(c) ?? false;
        if (success && ConnectedManager is FirearmBase { bolt: BoltSemiautomatic { caught: true } bolt })
        {
            bolt.TryRelease();
        }
    }

    private void UpdateCartridgePositions()
    {
        if (loadedCartridge && loadedCartridge.transform)
        {
            loadedCartridge.transform.parent = transform;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Vector3.zero;
        }
    }
}