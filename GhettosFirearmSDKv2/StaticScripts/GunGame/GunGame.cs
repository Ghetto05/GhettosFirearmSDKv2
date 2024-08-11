using IngameDebugConsole;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class GunGame : LevelModule
    {
        public static GunGame local;
        public int killsWithCurrentWeapon;
        public int currentWeapon;
        public string pouchItemId;
        private AmmunitionPouch pouch;
        private Holder pouchHolder;
        private Item weapon;

        private List<GunGameWeaponData> datas;
        
        public override IEnumerator OnLoadCoroutine()
        {
            if (local == null) local = this;
            else
            {
                return base.OnLoadCoroutine();
            }

            datas = Catalog.GetDataList<GunGameWeaponData>().OrderByDescending(data => data.order).ToList();
            EventManager.OnPlayerPrefabSpawned += EventManager_OnPlayerSpawned;
            EventManager.onCreatureKill += EventManager_onCreatureKill;
            return base.OnLoadCoroutine();
        }

        private void EventManager_onCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd) return;
            Debug.Log("Kill " + killsWithCurrentWeapon);
            killsWithCurrentWeapon++;
            if (killsWithCurrentWeapon == 6)
            {
                killsWithCurrentWeapon = 0;
                currentWeapon++;
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
            killsWithCurrentWeapon = 0;
            currentWeapon = 0;

            if (datas.Count == 0) LevelManager.LoadLevel("Home");

            foreach (var i in Player.local.creature.equipment.GetAllHolsteredItems())
            {
                i.Despawn();
            }

            Catalog.GetData<ItemData>(pouchItemId).SpawnAsync(newPouch =>
            {
                pouch = newPouch.GetComponent<AmmunitionPouch>();
                Player.local.creature.equipment.GetHolder(Holder.DrawSlot.HipsLeft).Snap(pouch.pouchItem);
                pouchHolder = pouch.GetComponentInChildren<Holder>();
            }, Player.local.transform.position);

            SpawnWeaponAndMagazine();
        }

        public ItemData GetCurrentWeapon()
        {
            if (currentWeapon == datas.Count) return null;
            return Catalog.GetData<ItemData>(datas[currentWeapon].weaponId);
        }

        public ItemData GetCurrentMagazine()
        {
            return Catalog.GetData<ItemData>(datas[currentWeapon].pouchItemId);
        }

        public void SpawnWeaponAndMagazine()
        {
            if (weapon != null) weapon.Despawn();
            if (GetCurrentWeapon() == null) LevelManager.LoadLevel("Home");
            Player.local.creature.GetHand(Side.Left).UnGrab(false);
            Player.local.creature.GetHand(Side.Right).UnGrab(false);
            GetCurrentWeapon()?.SpawnAsync(newWeapon => { weapon = newWeapon; Player.local.creature.GetHand(Side.Right).Grab(weapon.mainHandleRight); }, Player.local.transform.position);
            GetCurrentMagazine()?.SpawnAsync(newMag => { pouchHolder.UnSnapOneItem(true); pouch.Reset(); pouchHolder.Snap(newMag); });
        }
    }
}
