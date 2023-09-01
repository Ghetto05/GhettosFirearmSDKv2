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
        public float cyclePercentage;
        public bool externalTriggerState = false;

        private void Awake()
        {
            Util.DelayedExecute(3f, UpdateChamberedRounds, this);
        }

        public virtual void UpdateChamberedRounds()
        {
            //Debug.Log("Updated chambered round");
        }

        public virtual List<Handle> GetNoInfluenceHandles()
        {
            return new List<Handle>();
        }

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
            float f = FirearmsSettings.cartridgeEjectionTorque;
            Vector3 torque = new Vector3
            {
                x = Random.Range(-f, f),
                y = Random.Range(-f, f),
                z = Random.Range(-f, f)
            };
            c.item.physicBody.AddTorque(torque, ForceMode.Impulse);
        }

        public static void AddForceToCartridge(Cartridge c, Transform direction, float force)
        {
            float f = FirearmsSettings.cartridgeEjectionForceRandomizationDevision;
            c.item.physicBody.AddForce(direction.forward * (force + Random.Range(-(force / f), (force / f))), ForceMode.Impulse);
        }

        public virtual bool LoadChamber(Cartridge c, bool forced = false)
        {
            return false;
        }

        public void ChamberSaved()
        {
            if (FirearmSaveData.GetNode(firearm) != null && FirearmSaveData.GetNode(firearm).TryGetValue("ChamberSaveData", out SaveNodeValueString chamber))
            {
                Catalog.GetData<ItemData>(chamber.value)?.SpawnAsync(carItem =>
                {
                    Cartridge car = carItem.gameObject.GetComponent<Cartridge>();
                    LoadChamber(car);
                }, transform.position + Vector3.up * 3);
            }
        }

        void LoadChamber(Cartridge c)
        {
            bool succ = LoadChamber(c, true);
            if (!succ) c.item.Despawn();
        }

        public void SaveChamber(string id)
        {
            FirearmSaveData.AttachmentTreeNode node;
            if (firearm.GetType() == typeof(Firearm))
            {
                Firearm f = (Firearm)firearm;
                node = f.saveData.firearmNode;
            }
            else
            {
                AttachmentFirearm f = (AttachmentFirearm)firearm;
                node = f.attachment.node;
            }

            if (node != null)
            {
                node.GetOrAddValue("ChamberSaveData", new SaveNodeValueString()).value = id;
            }
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
