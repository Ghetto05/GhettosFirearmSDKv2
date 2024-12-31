using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class BoltBase : MonoBehaviour
    {
        public const string ChamberSaveDataId = "BoltChamberSaveData";
        
        public FirearmBase firearm;
        [HideInInspector]
        public BoltState state = BoltState.Locked;
        public BoltState laststate = BoltState.Locked;
        public bool caught;
        public bool isHeld;
        public bool fireOnTriggerPress = true;
        public ReciprocatingBarrel reciprocatingBarrel;
        public float cyclePercentage;
        protected float LastCyclePercentage;
        public bool externalTriggerState;
        public bool disallowRelease;
        private float _breachSmokeTime;
        public ParticleSystem[] breachSmokeEffects;

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
            var f = Settings.cartridgeEjectionTorque;
            var torque = new Vector3
                         {
                             x = Random.Range(-f, f),
                             y = Random.Range(-f, f),
                             z = Random.Range(-f, f)
                         };
            c.item.physicBody.AddTorque(torque, ForceMode.Impulse);
        }

        public static void AddForceToCartridge(Cartridge c, Transform direction, float force)
        {
            var f = Settings.cartridgeEjectionForceRandomizationDevision;
            c.item.physicBody.AddForce(direction.forward * (force + Random.Range(-(force / f), (force / f))), ForceMode.Impulse);
        }

        public virtual bool CanPlayBreachSmoke()
        {
            return true;
        }

        internal void BaseUpdate()
        {
            foreach (var par in breachSmokeEffects)
            {
                if (CanPlayBreachSmoke() && _breachSmokeTime > 0 && !par.isPlaying)
                    par.Play();
                if ((!CanPlayBreachSmoke() || _breachSmokeTime <= 0) && par.isPlaying)
                    par.Stop();
            }

            _breachSmokeTime -= Time.deltaTime;
            if (_breachSmokeTime < 0)
                _breachSmokeTime = 0;
        }

        public void IncrementBreachSmokeTime()
        {
            float breachSmokeBaseTime = 4;
            var breachSmokeIncrement = 2.2f;

            if (_breachSmokeTime < breachSmokeBaseTime)
                _breachSmokeTime = breachSmokeBaseTime;
            else
                _breachSmokeTime += breachSmokeIncrement;
        }

        public void ChamberSaved()
        {
            if (FirearmSaveData.GetNode(firearm) != null && FirearmSaveData.GetNode(firearm).TryGetValue(ChamberSaveDataId, out SaveNodeValueCartridgeData chamber))
            {
                Util.SpawnItem(chamber.Value.ItemId, "Bolt Chamber", carItem =>
                {
                    var car = carItem.gameObject.GetComponent<Cartridge>();
                    chamber.Value.Apply(car);
                    LoadChamber(car);
                }, transform.position + Vector3.up * 3);
            }
        }

        public virtual bool LoadChamber(Cartridge c, bool forced)
        {
            return false;
        }

        private void LoadChamber(Cartridge c)
        {
            var succ = LoadChamber(c, true);
            if (!succ)
                c.item.Despawn();
        }

        public void SaveChamber(string id, bool fired)
        {
            FirearmSaveData.AttachmentTreeNode node;
            if (firearm.GetType() == typeof(Firearm))
            {
                var f = (Firearm)firearm;
                node = f.SaveData.FirearmNode;
            }
            else
            {
                var f = (AttachmentFirearm)firearm;
                node = f.attachment.Node;
            }

            if (node != null)
            {
                node.GetOrAddValue(ChamberSaveDataId, new SaveNodeValueCartridgeData()).Value = new CartridgeSaveData(id, fired);
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

        public void InvokeFireLogicFinishedEvent() => OnFireLogicFinishedEvent?.Invoke();
        public delegate void OnFireLogicFinished();
        public event OnFire OnFireLogicFinishedEvent;

        public void InvokeEjectRound(Cartridge cartridge) => OnRoundEjectEvent?.Invoke(cartridge);
        public delegate void OnEject(Cartridge cartridge);
        public event OnEject OnRoundEjectEvent;
    }
}
