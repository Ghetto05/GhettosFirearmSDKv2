using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Other.LockSpell;

public class LockedItemModule : MonoBehaviour
{
    private Item _item;

    private void Start()
    {
        _item = GetComponent<Item>();
        _item.DisallowDespawn = true;
        _item.OnGrabEvent += OnGrab;
        _item.OnUngrabEvent += OnUnGrab;
        _item.OnDespawnEvent += OnDespawn;
    }

    private void OnDespawn(EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd)
        {
            return;
        }
        _item.OnGrabEvent += OnGrab;
        _item.OnUngrabEvent += OnUnGrab;
        _item.OnDespawnEvent += OnDespawn;
    }

    private void OnUnGrab(Handle handle, RagdollHand ragdollHand, bool throwing)
    {
        if (!_item.handlers.Any())
        {
            _item.physicBody.isKinematic = true;
        }
    }

    private void OnGrab(Handle handle, RagdollHand ragdollHand)
    {
        _item.physicBody.isKinematic = false;
    }
}