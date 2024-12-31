using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class MagazineWell : MonoBehaviour
    {
        public FirearmBase firearm;
        public string acceptedMagazineType;
        public List<string> alternateMagazineTypes;
        public string caliber;
        public List<string> alternateCalibers;
        public Collider loadingCollider;
        public Transform mountPoint;
        public bool canEject;
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
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            firearm.OnCollisionEvent += TryMount;
            firearm.OnColliderToggleEvent += Firearm_OnColliderToggleEvent;
            firearm.item.OnDespawnEvent += Item_OnDespawnEvent;
            if (spawnMagazineOnAwake) Load();
            else
            {
                if (currentMagazine != null && mountCurrentMagazine) currentMagazine.OnLoadFinished += Mag_onLoadFinished;
                else allowLoad = true;
            }
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            if (currentMagazine != null && !currentMagazine.overrideItem && !currentMagazine.overrideAttachment) currentMagazine.item.Despawn();
        }

        private void Update()
        {
            if (currentMagazine != null)
            {
                if (!currentMagazine.overrideItem && !currentMagazine.overrideAttachment)
                {
                    currentMagazine.item.SetMeshLayer(firearm.item.gameObject.layer);
                }
                roundCounterMessage = currentMagazine.cartridges.Count.ToString();
            }
            else roundCounterMessage = "N/A";
        }

        private void Firearm_OnColliderToggleEvent(bool active)
        {
            if (currentMagazine != null) currentMagazine.ToggleCollision(active);
        }

        public virtual void Load()
        {
            if (FirearmSaveData.GetNode(firearm).TryGetValue(SaveID, out SaveNodeValueMagazineContents data))
            {
                var cdata = new List<ContentCustomData>();
                cdata.Add(data.Value.CloneJson());
                if (data.Value == null || data.Value.ItemID == null || data.Value.ItemID.IsNullOrEmptyOrWhitespace())
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
            else allowLoad = true;
        }

        private void Mag_onLoadFinished(Magazine mag)
        {
            mag.Mount(this, firearm.item.physicBody.rigidBody, true);
            allowLoad = true;
        }

        public virtual void TryMount(Collision collision)
        {
            if (allowLoad && Util.AllLocksUnlocked(insertionLocks) && collision.collider.GetComponentInParent<Magazine>() is { } mag && collision.contacts[0].thisCollider == loadingCollider && BoltExistsAndIsPulled())
            {
                if (collision.contacts[0].otherCollider == mag.mountCollider && Util.AllowLoadMagazine(mag, this) && mag.loadable)
                {
                    mag.Mount(this, firearm.item.physicBody.rigidBody);
                    if (tryReleasingBoltIfMagazineIsInserted && firearm.bolt != null) firearm.bolt.TryRelease(true);
                }
            }
        }

        public virtual Cartridge ConsumeRound()
        {
            if (currentMagazine == null)
            {
                return null;
            }
            var success = currentMagazine.ConsumeRound();
            if (success == null && ejectOnEmpty) Eject(true);
            return success;
        }

        public virtual bool IsEmpty()
        {
            if (currentMagazine == null) return true;
            return currentMagazine.cartridges.Count < 1;
        }

        private bool BoltExistsAndIsPulled() => !onlyAllowEjectionWhenBoltIsPulled || firearm.bolt == null || firearm.bolt.state == BoltBase.BoltState.Back || firearm.bolt.state == BoltBase.BoltState.LockedBack;

        public virtual void Eject(bool forced = false)
        {
            if (!currentMagazine || currentMagazine.overrideItem || currentMagazine.overrideAttachment || (!forced && !BoltExistsAndIsPulled()) || !(canEject | forced) && currentMagazine.canBeGrabbedInWell)
                return;
            var mag = currentMagazine;
            currentMagazine.Eject();
            if (ejectDir != null)
                StartCoroutine(DelayedApplyForce(mag));
        }

        private IEnumerator DelayedApplyForce(Magazine mag)
        {
            yield return new WaitForSeconds(0.03f);
            mag.item.physicBody.velocity = Vector3.zero;
            mag.item.physicBody.AddForce(ejectDir.forward * ejectForce, ForceMode.Impulse);
        }

        public virtual bool IsEmptyAndHasMagazine()
        {
            if (currentMagazine == null) return false;
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
}
