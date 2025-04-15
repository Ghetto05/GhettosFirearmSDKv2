using System;
using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class CartridgeHolder : MonoBehaviour
{
    private IAttachmentManager _connectedManager;
    private IComponentParent _parent;
    public Attachment attachment;
    public GameObject attachmentManager;

    public int slot;
    public string caliber;
    public Collider mountCollider;
    public ChamberLoader chamberLoader;

    public List<AudioSource> roundInsertSounds;
    public List<AudioSource> roundEjectSounds;

    public Cartridge loadedCartridge;

    private void Start()
    {
        Util.GetParent(attachmentManager, attachment).GetInitialization(Init);
        Invoke(nameof(UpdateCartridgePositions), Settings.invokeTime * 5);
    }

    public void Init(IAttachmentManager manager, IComponentParent parent)
    {
        _connectedManager = manager;
        _parent = parent;
        manager.OnCollision += Collision;
        manager.Item.OnGrabEvent += Firearm_OnGrabEvent;

        SaveNodeValueCartridgeData save = null;
        if (parent.SaveNode.TryGetValue("CartridgeHolder" + slot, out SaveNodeValueCartridgeData value))
        {
            save = value;
        }

        if (save is not null)
        {
            Util.SpawnItem(save.Value.ItemId, $"[Cartridge holder - Firearm: {_connectedManager?.Item?.itemId ?? "--"} Attachment: {attachment?.Data.id ?? "--"} Slot: {slot}]", cartridge =>
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
            if (_connectedManager is not null)
            {
                Util.DelayIgnoreCollision(_connectedManager.Transform.gameObject, loaded.gameObject, false, 1f, loaded.item);
            }
            loaded.loaded = false;
            loaded.GetComponent<Rigidbody>().isKinematic = false;
            loaded.item.DisallowDespawn = false;
            loaded.transform.parent = null;
            loaded.item.OnGrabEvent -= OnGrab;
            loadedCartridge = null;

            attachment.Node.RemoveValue("CartridgeHolder" + slot);
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
            if (_connectedManager is not null)
            {
                Util.IgnoreCollision(c.gameObject, _connectedManager.Transform.gameObject, true);
            }
            if (!silent)
            {
                Util.PlayRandomAudioSource(roundInsertSounds);
            }
            c.GetComponent<Rigidbody>().isKinematic = true;
            c.transform.parent = transform;
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Vector3.zero;

            c.item.OnGrabEvent += OnGrab;

            _parent.SaveNode.GetOrAddValue("CartridgeHolder" + slot, new SaveNodeValueCartridgeData()).Value = new CartridgeSaveData(c.item.itemId, c.Fired);
        }
        UpdateCartridgePositions();
    }

    private void OnGrab(Handle handle, RagdollHand ragdollHand)
    {
        var c = EjectRound();
        var success = chamberLoader?.TryLoad(c) ?? false;
        if (success && _connectedManager is FirearmBase { bolt: BoltSemiautomatic { caught: true } bolt })
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