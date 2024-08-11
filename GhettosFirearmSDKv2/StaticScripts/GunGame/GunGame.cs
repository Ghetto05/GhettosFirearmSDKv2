using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class GunGame : LevelModule
    {
        public static GunGame local;
        public int KillsWithCurrentWeapon;
        public int CurrentWeapon;
        public string PouchItemId;
        private AmmunitionPouch _pouch;
        private Holder _pouchHolder;
        private Item _weapon;

        private List<GunGameWeaponData> _datas;
        
        public override IEnumerator OnLoadCoroutine()
        {
            if (local == null) local = this;
            else
            {
                return base.OnLoadCoroutine();
            }

            _datas = Catalog.GetDataList<GunGameWeaponData>().OrderByDescending(data => data.Order).ToList();
            EventManager.OnPlayerPrefabSpawned += EventManager_OnPlayerSpawned;
            EventManager.onCreatureKill += EventManager_onCreatureKill;
            return base.OnLoadCoroutine();
        }

        private void EventManager_onCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd) return;
            Debug.Log("Kill " + KillsWithCurrentWeapon);
            KillsWithCurrentWeapon++;
            if (KillsWithCurrentWeapon == 6)
            {
                KillsWithCurrentWeapon = 0;
                CurrentWeapon++;
                SpawnWeaponAndMagazine();
            }
        }

        private void EventManager_OnPlayerSpawned()
        {
            Player.local.StartCoroutine(DelayedLoad());
        }

        private IEnumerator DelayedLoad()
        {
            yield return new WaitForSeconds(2f);
            KillsWithCurrentWeapon = 0;
            CurrentWeapon = 0;

            if (_datas.Count == 0) LevelManager.LoadLevel("Home");

            foreach (var i in Player.local.creature.equipment.GetAllHolsteredItems())
            {
                i.Despawn();
            }

            Catalog.GetData<ItemData>(PouchItemId).SpawnAsync(newPouch =>
            {
                _pouch = newPouch.GetComponent<AmmunitionPouch>();
                Player.local.creature.equipment.GetHolder(Holder.DrawSlot.HipsLeft).Snap(_pouch.pouchItem);
                _pouchHolder = _pouch.GetComponentInChildren<Holder>();
            }, Player.local.transform.position);

            SpawnWeaponAndMagazine();
        }

        public ItemData GetCurrentWeapon()
        {
            if (CurrentWeapon == _datas.Count) return null;
            return Catalog.GetData<ItemData>(_datas[CurrentWeapon].WeaponId);
        }

        public ItemData GetCurrentMagazine()
        {
            return Catalog.GetData<ItemData>(_datas[CurrentWeapon].PouchItemId);
        }

        public void SpawnWeaponAndMagazine()
        {
            if (_weapon != null) _weapon.Despawn();
            if (GetCurrentWeapon() == null) LevelManager.LoadLevel("Home");
            Player.local.creature.GetHand(Side.Left).UnGrab(false);
            Player.local.creature.GetHand(Side.Right).UnGrab(false);
            GetCurrentWeapon()?.SpawnAsync(newWeapon => { _weapon = newWeapon; Player.local.creature.GetHand(Side.Right).Grab(_weapon.mainHandleRight); }, Player.local.transform.position);
            GetCurrentMagazine()?.SpawnAsync(newMag => { _pouchHolder.UnSnapOneItem(true); _pouch.Reset(); _pouchHolder.Snap(newMag); });
        }
    }
}
