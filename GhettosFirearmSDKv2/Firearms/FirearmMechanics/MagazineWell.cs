using System.Collections;
using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MagazineWell : MonoBehaviour
{
    private IAttachmentManager _manager;
    private IComponentParent _parent;
    public FirearmBase actualFirearm;
    public GameObject firearm;
    public string acceptedMagazineType;
    public List<string> alternateMagazineTypes;
    public string caliber;
    public List<string> alternateCalibers;
    public Collider loadingCollider;
    public Transform mountPoint;
    public bool canEject;
    public bool forceCanGrab;
    public bool ejectOnEmpty;
    public Magazine currentMagazine;
    public bool mountCurrentMagazine = true;
    public bool spawnMagazineOnAwake;
    public string roundCounterMessage;
    public bool allowLoad;
    public bool onlyAllowEjectionWhenBoltIsPulled;
    public BoltBase.BoltState lockedState;
    public Transform ejectDir;
    public float ejectForce = 0.3f;
    public bool tryReleasingBoltIfMagazineIsInserted;
    public List<Lock> insertionLocks;
    public Transform beltLinkEjectDir;
    public string customSaveId;

    public virtual void Start()
    {
        Util.GetParent(firearm, null).GetInitialization(Init);
    }

    public void Init(IAttachmentManager manager, IComponentParent parent)
    {
        _manager = manager;
        _parent = parent;

        if (manager is Firearm f)
        {
            actualFirearm = f;
        }

        if (parent is Attachment a && a.GetComponent<AttachmentFirearm>() is { } af)
        {
            actualFirearm = af;
        }
        
        actualFirearm.OnCollisionEvent += TryMount;
        actualFirearm.OnColliderToggleEvent += Firearm_OnColliderToggleEvent;
        _manager.Item.OnDespawnEvent += Item_OnDespawnEvent;
        if (spawnMagazineOnAwake)
        {
            Load();
        }
        else
        {
            if (currentMagazine && mountCurrentMagazine)
            {
                currentMagazine.OnLoadFinished += Mag_onLoadFinished;
            }
            else
            {
                allowLoad = true;
            }
        }
    }

    private void Item_OnDespawnEvent(EventTime eventTime)
    {
        if (currentMagazine && !currentMagazine.overrideItem && !currentMagazine.overrideAttachment)
        {
            currentMagazine.item.Despawn();
        }
    }

    private void Update()
    {
        if (currentMagazine)
        {
            if (!currentMagazine.overrideItem && !currentMagazine.overrideAttachment)
            {
                currentMagazine.item.SetMeshLayer(_manager.Item.gameObject.layer);
            }
            roundCounterMessage = currentMagazine.cartridges.Count.ToString();
        }
        else
        {
            roundCounterMessage = "N/A";
        }
    }

    private void Firearm_OnColliderToggleEvent(bool active)
    {
        if (currentMagazine)
        {
            currentMagazine.ToggleCollision(active);
        }
    }

    public virtual void Load()
    {
        if (_parent.SaveNode.TryGetValue(SaveID, out SaveNodeValueMagazineContents data))
        {
            var cdata = new List<ContentCustomData>();
            cdata.Add(data.Value.CloneJson());
            if (data.Value is null || data.Value.ItemID is null || data.Value.ItemID.IsNullOrEmptyOrWhitespace())
            {
                allowLoad = true;
                return;
            }
            Util.SpawnItem(data.Value.ItemID, "Magazine Well Save", magItem =>
            {
                var mag = magItem.gameObject.GetComponent<Magazine>();
                mag.OnLoadFinished += Mag_onLoadFinished;
            }, mountPoint.position + Vector3.up * 3, null, null, true, cdata);
        }
        else
        {
            allowLoad = true;
        }
    }

    private void Mag_onLoadFinished(Magazine mag)
    {
        mag.Mount(this, _manager.Item.physicBody.rigidBody, true);
        allowLoad = true;
    }

    public virtual void TryMount(Collision collision)
    {
        if (allowLoad && Util.AllLocksUnlocked(insertionLocks) && collision.collider.GetComponentInParent<Magazine>() is { } mag && collision.contacts[0].thisCollider == loadingCollider && BoltExistsAndIsPulled())
        {
            if (collision.contacts[0].otherCollider == mag.mountCollider && Util.AllowLoadMagazine(mag, this) && mag.loadable)
            {
                mag.Mount(this, _manager.Item.physicBody.rigidBody);
                if (tryReleasingBoltIfMagazineIsInserted && actualFirearm.bolt)
                {
                    actualFirearm.bolt.TryRelease(true);
                }
            }
        }
    }

    public virtual Cartridge ConsumeRound()
    {
        if (!currentMagazine)
        {
            return null;
        }
        var success = currentMagazine.ConsumeRound();
        if (!success && ejectOnEmpty)
        {
            Eject(true);
        }
        return success;
    }

    public virtual bool IsEmpty()
    {
        if (!currentMagazine)
        {
            return true;
        }
        return currentMagazine.cartridges.Count < 1;
    }

    private bool BoltExistsAndIsPulled()
    {
        return !onlyAllowEjectionWhenBoltIsPulled || !actualFirearm.bolt || actualFirearm.bolt.state == BoltBase.BoltState.Back || actualFirearm.bolt.state == BoltBase.BoltState.LockedBack;
    }

    public virtual void Eject(bool forced = false)
    {
        if (!currentMagazine || currentMagazine.overrideItem || currentMagazine.overrideAttachment || (!forced && !BoltExistsAndIsPulled()) || (!(canEject | forced) && currentMagazine.CanGrab))
        {
            return;
        }
        var mag = currentMagazine;
        currentMagazine.Eject();
        if (ejectDir)
        {
            StartCoroutine(DelayedApplyForce(mag));
        }
    }

    private IEnumerator DelayedApplyForce(Magazine mag)
    {
        yield return new WaitForSeconds(0.03f);

        mag.item.physicBody.velocity = Vector3.zero;
        mag.item.physicBody.AddForce(ejectDir.forward * ejectForce, ForceMode.Impulse);
    }

    public virtual bool IsEmptyAndHasMagazine()
    {
        if (!currentMagazine)
        {
            return false;
        }
        return currentMagazine.cartridges.Count < 1;
    }

    public string SaveID
    {
        get
        {
            return string.IsNullOrWhiteSpace(customSaveId) ? "MagazineSaveData" : customSaveId;
        }
    }
}