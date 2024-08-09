using System.Collections;
using System.Dynamic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class HomeAdjustments : ThunderScript
    {
        public static HomeAdjustments local;
        
        public override void ScriptEnable()
        {
            local = this;
            //EventManager.OnPlayerPrefabSpawned += EventManagerOnOnPlayerPrefabSpawned;
        }

        private void EventManagerOnOnPlayerPrefabSpawned()
        {
            if (Level.current.data.id.Equals("Home"))
                Util.DelayedExecute(20f, RemoveDrawers, Player.local);
        }

        #region Gun Locker / Liam
        
        public GameObject workbenchAndLocker;
        public GameObject liam;
        
        public void SpawnHomeItems()
        {
            if (Settings.spawnWorkbenchAndLocker && AllowSpawnLocker)
                Level.current.StartCoroutine(DelayedLockerSpawn());
            if (Settings.spawnLiam)
                Level.current.StartCoroutine(DelayedRigEditorSpawn());
        }

        private bool AllowSpawnLocker
        {
            get
            {
                return !GameModeManager.instance.currentGameMode.name.Equals("CrystalHunt");
            }
        }

        private IEnumerator DelayedLockerSpawn()
        {
            yield return new WaitForSeconds(3f);
            SpawnWorkbenchAndLocker();
        }

        private IEnumerator DelayedRigEditorSpawn()
        {
            yield return new WaitForSeconds(3f);
            SpawnLiam();
        }

        public void SpawnLiam()
        {
            if (liam != null || !Level.current.data.id.Equals("Home"))
                return;
            Vector3 position = new Vector3(44.03f, 2.5f, -44.37f);
            Vector3 rotation = new Vector3(0, -36, 0);
            Addressables.InstantiateAsync("Ghetto05.Firearms.Clothes.Rigs.Editor", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning(("Unable to instantiate rig editor!"));
                    Addressables.ReleaseInstance(handle);
                }
                liam = handle.Result;
            };
        }

        public void SpawnWorkbenchAndLocker()
        {
            if (workbenchAndLocker != null || !Level.current.data.id.Equals("Home"))
                return;
            Vector3 position = new Vector3(41.3f, 2.5f, -43.0f);
            Vector3 rotation = new Vector3(0, 120, 0);
            Addressables.InstantiateAsync("Ghetto05.FirearmFrameworkV2.Locker", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning(("Unable to instantiate gun locker!"));
                    Addressables.ReleaseInstance(handle);
                }
                workbenchAndLocker = handle.Result;
            };
        }

        #endregion

        #region Remove Drawers

        private static string[] _naughtyList = new[]
                                              {
                                                  "Drawer1",
                                                  "Table4m",
                                                  "Bench2m",
                                                  "Jar2",
                                                  "Pottery_05",
                                                  "Pottery_02",
                                                  "PotionHealth",
                                                  "Pottery_06",
                                                  "Stool2",
                                                  "Chair1",
                                                  "Apple"
                                              };
        
        private void RemoveDrawers()
        {
            foreach (Item item in Item.all.Where(i => _naughtyList.Contains(i.data.id)).ToArray())
            {
                item.Despawn(0.1f);
            }
        }

        #endregion
    }
}