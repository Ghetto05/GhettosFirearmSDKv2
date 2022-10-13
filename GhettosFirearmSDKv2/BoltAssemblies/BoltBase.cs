using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class BoltBase : MonoBehaviour
    {
        public Firearm firearm;
        [HideInInspector]
        public BoltState state = BoltState.Locked;
        public BoltState laststate = BoltState.Locked;
        public bool caught;

        public virtual void TryFire()
        {
        }

        public virtual void TryRelease()
        {
        }

        public virtual bool ForceLoadChamber(Cartridge c)
        {
            return false;
        }

        public IEnumerator delayedGetChamber()
        {
            yield return new WaitForSeconds(1f);
            if (firearm.item.TryGetCustomData(out ChamberSaveData data))
            {
                Catalog.GetData<ItemData>(data.itemId).SpawnAsync(carItem =>
                {
                    Cartridge car = carItem.gameObject.GetComponent<Cartridge>();
                    firearm.item.StartCoroutine(delayedLoadChamber(car, 1f));
                }, this.transform.position + Vector3.up * 3);
            }
        }

        IEnumerator delayedLoadChamber(Cartridge c, float delay)
        {
            yield return new WaitForSeconds(delay);
            bool succ = ForceLoadChamber(c);
            if (!succ) c.item.Despawn();
        }

        public void SaveChamber(string id)
        {
            firearm.item.RemoveCustomData<ChamberSaveData>();
            ChamberSaveData data = new ChamberSaveData();
            data.itemId = id;
            firearm.item.AddCustomData(data);
        }

        public enum BoltState
        {
            Locked,
            Front,
            Moving,
            Back,
            LockedBack
        }
    }
}
