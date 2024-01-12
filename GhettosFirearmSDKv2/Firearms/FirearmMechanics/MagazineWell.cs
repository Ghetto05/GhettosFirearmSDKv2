using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

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
        public bool ejectOnEmpty = false;
        public Magazine currentMagazine;
        public bool mountCurrentMagazine = true;
        public bool spawnMagazineOnAwake;
        public string roundCounterMessage;
        public bool allowLoad = false;
        public bool onlyAllowEjectionWhenBoltIsPulled = false;
        public BoltBase.BoltState lockedState;
        public Transform ejectDir;
        public float ejectForce = 0.3f;
        public bool tryReleasingBoltIfMagazineIsInserted = false;
        public List<Lock> insertionLocks;
        public Transform beltLinkEjectDir;

        public virtual void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
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
            if (currentMagazine != null && currentMagazine.overrideItem == null) currentMagazine.item.Despawn();
        }

        void Update()
        {
            if (currentMagazine != null)
            {
                if (currentMagazine.overrideItem == null)
                {
                    currentMagazine.item.SetColliderLayer(firearm.item.currentPhysicsLayer);
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
            if (FirearmSaveData.GetNode(firearm).TryGetValue("MagazineSaveData", out SaveNodeValueMagazineContents data))
            {
                List<ContentCustomData> cdata = new List<ContentCustomData>();
                cdata.Add(data.value.CloneJson());
                if (data.value == null || data.value.itemID == null || data.value.itemID.IsNullOrEmptyOrWhitespace())
                {
                    allowLoad = true;
                    return;
                }
                Catalog.GetData<ItemData>(data.value.itemID).SpawnAsync(magItem =>
                {
                    Magazine mag = magItem.gameObject.GetComponent<Magazine>();
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
            if (allowLoad && Util.AllLocksUnlocked(insertionLocks) && collision.collider.GetComponentInParent<Magazine>() is Magazine mag && collision.contacts[0].thisCollider == loadingCollider && BoltExistsAndIsPulled())
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
            Cartridge success = currentMagazine.ConsumeRound();
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
            if (currentMagazine == null || currentMagazine.overrideItem != null || !forced && !BoltExistsAndIsPulled() || !(canEject | forced) && currentMagazine.canBeGrabbedInWell)
                return;
            Magazine mag = currentMagazine;
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
    }
}
