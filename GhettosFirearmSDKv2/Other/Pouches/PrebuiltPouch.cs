using System;
using System.Collections;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2;

public class PrebuiltPouch : MonoBehaviour
{
    public Item pouchItem;
    public Holder holder;
    private bool _spawning;

    private void Start()
    {
        holder.Snapped += Holder_Snapped;
        holder.UnSnapped += Holder_UnSnapped;
    }

    private void Holder_UnSnapped(Item item)
    {
        item.Hide(false);
    }

    private void Holder_Snapped(Item item)
    {
        item.Hide(true);
    }

    private void Update()
    {
        if (!_spawning && holder.items.Count > 0 && (holder.items[0] == null || holder.items[0].holder == null))
        {
            holder.items.Clear();
            holder.currentQuantity = 0;
        }

        if (!_spawning && holder.HasSlotFree())
        {
            _spawning = true;
            try
            {
                var data = GunLockerSaveData.allPrebuilts[Random.Range(0, GunLockerSaveData.allPrebuilts.Count)];
                data.SpawnAsync(item =>
                {
                    item.physicBody.rigidBody.isKinematic = true;
                    StartCoroutine(DelayedSnap(item));
                }, holder.transform.position);
            }
            catch (Exception)
            {
                _spawning = false;
            }
        }
    }

    private IEnumerator DelayedSnap(Item item)
    {
        yield return new WaitForSeconds(Settings.invokeTime + 0.06f);
        if (item != null) holder.Snap(item);
        _spawning = false;
    }
}