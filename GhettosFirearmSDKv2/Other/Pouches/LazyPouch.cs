using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class LazyPouch : MonoBehaviour
{
    public Holder holder;
    public Item pouchItem;
    public List<Item> spawnedItems;
    public List<string> containedItems;
    public Handle lastHeldHandle;
    private bool _setup;
    private bool _nextUnsnapIsCleaning;
    private FirearmBase _lastFirearm;
    private bool _spawning;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        spawnedItems = new List<Item>();
        containedItems = new List<string>();

        holder.Snapped += Holder_Snapped;
        holder.UnSnapped += Holder_UnSnapped;
        pouchItem.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;

        holder.data.maxQuantity = 9999999;

        for (var i = 0; i < 1000; i++)
        {
            holder.slots.Add(holder.transform);
        }

        _setup = true;
    }

    private void Update()
    {
        if (!_setup || !Player.local?.creature?.ragdoll)
        {
            return;
        }

        foreach (var i in holder.items.ToArray())
        {
            if (!spawnedItems.Contains(i))
            {
                _nextUnsnapIsCleaning = true;
                holder.UnSnap(i, true);
            }
        }

        GetHeldFirearm();

        if (_lastFirearm)
        {
            if (!containedItems.Contains(_lastFirearm.defaultAmmoItem) && !_lastFirearm.defaultAmmoItem.IsNullOrEmptyOrWhitespace())
            {
                containedItems.Add(_lastFirearm.defaultAmmoItem);
                _spawning = true;
                Util.SpawnItem(_lastFirearm.defaultAmmoItem, "LazyPouch", newItem =>
                {
                    newItem.DisallowDespawn = true;
                    StartCoroutine(DelayedSnap(newItem));
                    spawnedItems.Add(newItem);
                });
            }
            else if (!_lastFirearm.defaultAmmoItem.IsNullOrEmptyOrWhitespace() && !_spawning)
            {
                GetById(_lastFirearm.defaultAmmoItem);
            }
        }
    }

    private void GetHeldFirearm()
    {
        var h = Player.local.GetHand(Handle.dominantHand).ragdollHand.grabbedHandle;
        if (h && h.item is { } item)
        {
            FirearmBase firearm;
            if (lastHeldHandle != h)
            {
                if (h.GetComponentInParent<AttachmentFirearm>())
                {
                    firearm = h.GetComponentInParent<AttachmentFirearm>();
                }
                else
                {
                    firearm = item.GetComponent<Firearm>();
                }

                if (firearm && firearm.defaultAmmoItem.IsNullOrEmptyOrWhitespace() && item.GetComponentInChildren<AttachmentFirearm>() is { } ff)
                {
                    firearm = ff;
                }

                _lastFirearm = firearm;
            }

            lastHeldHandle = h;
        }
    }

    public void GetById(string id)
    {
        var searching = true;
        var requestedId = Util.GetSubstituteId(id, "Lazy pouch");
        foreach (var i in spawnedItems)
        {
            if (searching && i.data.id.Equals(requestedId))
            {
                searching = false;
                holder.items.Remove(i);
                holder.items.Add(i);
            }
        }
    }

    private void Holder_UnSnapped(Item item)
    {
        item.Hide(false);
        if (_nextUnsnapIsCleaning)
        {
            _nextUnsnapIsCleaning = false;
            return;
        }
        if (!_setup)
        {
            return;
        }
        item.DisallowDespawn = false;
        Util.IgnoreCollision(gameObject, item.gameObject, false);
        spawnedItems.Remove(item);
        if (item.TryGetComponent(out Firearm _))
        {
            return;
        }
        _spawning = true;
        Util.SpawnItem(item.data.id, $"[Lazy Pouch - Default ammo on {_lastFirearm?.item?.itemId}]", newItem =>
        {
            item.DisallowDespawn = true;
            StartCoroutine(DelayedSnap(newItem));
            spawnedItems.Add(newItem);
        });
    }

    private void Holder_Snapped(Item item)
    {
        item.Hide(true);
        if (!_setup)
        {
            return;
        }
        Util.IgnoreCollision(gameObject, item.gameObject, true);
    }

    private IEnumerator DelayedSnap(Item item)
    {
        yield return new WaitForSeconds(0.05f);

        holder.Snap(item, true);
        _spawning = false;
    }

    private void UpdateAllLightVolumeReceivers(LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
    {
        foreach (var lvr in GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != pouchItem.lightVolumeReceiver))
        {
            Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
        }
    }
}