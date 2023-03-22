using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class BoltBase : MonoBehaviour
    {
        public FirearmBase firearm;
        [HideInInspector]
        public BoltState state = BoltState.Locked;
        public BoltState laststate = BoltState.Locked;
        public bool caught;
        public bool isHeld;
        public bool fireOnTriggerPress = true;
        public ReciprocatingBarrel reciprocatingBarrel;

        public static Vector3 GrandparentLocalPosition(Transform child, Transform grandparent)
        {
            return grandparent.InverseTransformPoint(child.position);
        }

        public virtual void TryFire()
        { }

        public virtual void TryRelease(bool forced = false)
        { }

        public virtual void TryEject()
        { }

        public virtual void EjectRound()
        { }

        public virtual void TryLoadRound()
        { }

        public static void AddTorqueToCartridge(Cartridge c)
        {
            float f = Settings_LevelModule.local.cartridgeEjectionTorque;
            Vector3 torque = new Vector3
            {
                x = Random.Range(-f, f),
                y = Random.Range(-f, f),
                z = Random.Range(-f, f)
            };
            c.item.rb.AddTorque(torque);
        }

        public static void AddForceToCartridge(Cartridge c, Transform direction, float force)
        {
            float f = Settings_LevelModule.local.cartridgeEjectionForceRandomizationDevision;
            c.item.rb.AddForce(direction.forward * (force + Random.Range(-(force / f), (force / f))), ForceMode.Impulse);
        }

        public virtual bool LoadChamber(Cartridge c, bool forced = false)
        {
            return false;
        }

        public IEnumerator delayedGetChamber()
        {
            yield return new WaitForSeconds(1f);
            if (firearm.GetType() == typeof(Firearm) && firearm.item.TryGetCustomData(out ChamberSaveData data))
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
            bool succ = LoadChamber(c, true);
            if (!succ) c.item.Despawn();
        }

        public void SaveChamber(string id)
        {
            if (!firearm.SaveChamber()) return;
            firearm.item.RemoveCustomData<ChamberSaveData>();
            ChamberSaveData data = new ChamberSaveData();
            data.itemId = id;
            firearm.item.AddCustomData(data);
        }

        public virtual void Initialize()
        { }

        public virtual Cartridge GetChamber()
        {
            return null;
        }

        public enum BoltState
        {
            Locked,
            Front,
            Moving,
            Back,
            LockedBack
        }

        public void InvokeFireEvent() => OnFireEvent?.Invoke();
        public delegate void OnFire();
        public event OnFire OnFireEvent;

        public void InvokeEjectRound(Cartridge cartridge) => OnRoundEjectEvent?.Invoke(cartridge);
        public delegate void OnEject(Cartridge cartridge);
        public event OnEject OnRoundEjectEvent;
    }
}
