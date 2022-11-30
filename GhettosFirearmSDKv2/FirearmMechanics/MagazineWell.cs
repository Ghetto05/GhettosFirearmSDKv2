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
        public string caliber;
        public List<string> alternateCalibers;
        public Collider loadingCollider;
        public Transform mountPoint;
        public bool canEject;
        public bool ejectOnEmpty = false;
        public Magazine currentMagazine;
        public bool spawnMagazineOnAwake;
        public string roundCounterMessage;
        public bool allowLoad = false;

        public virtual void Awake()
        {
            firearm.OnCollisionEvent += TryMount;
            firearm.OnColliderToggleEvent += Firearm_OnColliderToggleEvent;
            if (spawnMagazineOnAwake) StartCoroutine(delayedLoad());
            else allowLoad = true;
        }

        void Update()
        {
            if (currentMagazine != null)
            {
                currentMagazine.item.SetColliderLayer(firearm.item.currentPhysicsLayer);
                currentMagazine.item.SetMeshLayer(firearm.item.gameObject.layer);
                roundCounterMessage = currentMagazine.cartridges.Count.ToString();
            }
            else roundCounterMessage = "N/A";
        }

        private void Firearm_OnColliderToggleEvent(bool active)
        {
            if (currentMagazine != null) currentMagazine.ToggleCollision(active);
        }

        public virtual IEnumerator delayedLoad()
        {
            yield return new WaitForSeconds(4f);
            if (firearm.item.TryGetCustomData(out MagazineSaveData data))
            {
                List<ContentCustomData> cdata = new List<ContentCustomData>();
                cdata.Add(data);
                Catalog.GetData<ItemData>(data.itemID).SpawnAsync(magItem =>
                {
                    Magazine mag = magItem.gameObject.GetComponent<Magazine>();
                    StartCoroutine(delayedInsert(mag, 1f, true));
                }, mountPoint.position + Vector3.up * 3, null, null, true, cdata);
            }
            else allowLoad = true;
        }

        public virtual IEnumerator delayedInsert(Magazine mag, float delay, bool initialLoad = false)
        {
            yield return new WaitForSeconds(delay);
            if (initialLoad) allowLoad = true;
            mag.Mount(this, firearm.item.rb);
        }

        public virtual void TryMount(Collision collision)
        {
            if (allowLoad && collision.collider.GetComponentInParent<Magazine>() is Magazine mag && collision.contacts[0].thisCollider == loadingCollider)
            {
                if (collision.contacts[0].otherCollider == mag.mountCollider && Util.AllowLoadMagazine(mag, this))
                {
                    mag.Mount(this, firearm.item.rb);
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
