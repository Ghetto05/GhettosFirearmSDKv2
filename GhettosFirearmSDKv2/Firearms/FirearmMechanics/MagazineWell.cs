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

        public virtual void Start()
        {
            firearm.OnCollisionEvent += TryMount;
            firearm.OnColliderToggleEvent += Firearm_OnColliderToggleEvent;
            firearm.item.OnDespawnEvent += Item_OnDespawnEvent;
            if (spawnMagazineOnAwake) Load();
            else
            {
                if (currentMagazine != null && mountCurrentMagazine) currentMagazine.onLoadFinished += Mag_onLoadFinished;
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
                    mag.onLoadFinished += Mag_onLoadFinished;
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
            if (allowLoad && collision.collider.GetComponentInParent<Magazine>() is Magazine mag && collision.contacts[0].thisCollider == loadingCollider)
            {
                if (collision.contacts[0].otherCollider == mag.mountCollider && Util.AllowLoadMagazine(mag, this) && mag.loadable)
                {
                    mag.Mount(this, firearm.item.physicBody.rigidBody);
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

        public virtual void Eject(bool forced = false)
        {
            if ((canEject || forced) && currentMagazine != null)
            {
                currentMagazine.Eject();
            }
        }

        public virtual bool IsEmptyAndHasMagazine()
        {
            if (currentMagazine == null) return false;
            return currentMagazine.cartridges.Count < 1;
        }
    }
}
