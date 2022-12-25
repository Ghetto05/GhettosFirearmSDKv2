using System.Text;
using UnityEngine;
using ThunderRoad;
using System.Collections;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class Settings_LevelModule : LevelModule
    {
        public static Settings_LevelModule local;
        public bool incapitateOnTorsoShot;
        public bool infiniteAmmo;
        public bool doCaliberChecks;
        public bool doMagazineTypeChecks;
        public float damageMultiplier;
        public bool magazinesHaveNoCollision = true;
        public float scopeX1MagnificationFOV = 28.5f;

        public int shotsFired = 0;
        public int shotsHit = 0;
        public int headshots = 0;
        public float accuracy = 0f;

        public override IEnumerator OnLoadCoroutine()
        {
            local = this;

            Debug.Log("------------> Loaded FirearmSDKv2 settings!");
            Debug.Log($"-------------- Incapitate: {incapitateOnTorsoShot}");
            Debug.Log($"-------------- Infinite ammo: {infiniteAmmo}");
            Debug.Log($"-------------- Caliber checks: {doCaliberChecks}");
            Debug.Log($"-------------- Magazine type checks: {doMagazineTypeChecks}");
            Debug.Log($"-------------- Damage multiplier: {damageMultiplier}");
            Debug.Log($"-------------- Disable magazine collisions: {magazinesHaveNoCollision}");

            EventManager.OnPlayerSpawned += EventManager_OnPlayerSpawned;
            EventManager.onCreatureSpawn += EventManager_onCreatureSpawn;
            return base.OnLoadCoroutine();
        }

        private void SpawnGunLocker()
        {
            Level.current.StartCoroutine(DelayedLockerSpawn());
        }

        private IEnumerator DelayedLockerSpawn()
        {
            yield return new WaitForSeconds(3f);
            Vector3 position = new Vector3(41.3f, 2.5f, -43.0f);
            Vector3 rotation = new Vector3(0, 120, 0);
            Addressables.InstantiateAsync("GunLocker_Ghetto05_FirearmSDKv2", position, Quaternion.Euler(rotation.x, rotation.y, rotation.z), null, false).Completed += (System.Action<AsyncOperationHandle<GameObject>>)(handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                }
                else
                {
                    Debug.LogWarning((object)("Unable to instantiate gun locker!"));
                    Addressables.ReleaseInstance(handle);
                }
            });
        }

        private void EventManager_onCreatureSpawn(Creature creature)
        {
            if (creature.isPlayer) return;
            creature.gameObject.AddComponent<Chemicals.NPCChemicalsModule>();
        }

        public void CalculateAccuracy()
        {
            accuracy = (shotsHit / shotsFired) * 100f;
        }

        private void EventManager_OnPlayerSpawned()
        {
            shotsFired = 0;
            shotsHit = 0;
            headshots = 0;
            accuracy = 0f;

            Player.local.gameObject.AddComponent<Chemicals.PlayerEffectsAndChemicalsModule>();

            foreach (RequiredPenetrationPowerData rppd in Catalog.GetDataList<RequiredPenetrationPowerData>())
            {
                rppd.Init();
            }

            if (Level.current.data.id.Equals("Home")) SpawnGunLocker();
        }

        public void SendUpdate()
        {
            string s = JsonConvert.SerializeObject(this, Catalog.jsonSerializerSettings);
            OverrideJson(s);
            OnValueChangedEvent?.Invoke();
        }

        private void OverrideJson(string content)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("    \"$type\": \"ThunderRoad.LevelData, ThunderRoad\",");
            builder.AppendLine("    \"id\": \"Master\",");
            builder.AppendLine("    \"saveFolder\": \"Bas\",");
            builder.AppendLine("    \"version\": 3,");
            builder.AppendLine("    \"name\": null,");
            builder.AppendLine("    \"description\": null,");
            builder.AppendLine("    \"modes\":");
            builder.AppendLine("    [");
            builder.AppendLine("        {");
            builder.AppendLine("            \"name\": \"Default\",");
            builder.AppendLine("            \"displayName\": \"{Default}\", ");
            builder.AppendLine("            \"description\": \"{NoDescription}\", ");
            builder.AppendLine("            \"modules\":");
            builder.AppendLine("            [");
            builder.AppendLine(content);
            builder.AppendLine("            ]");
            builder.AppendLine("        }");
            builder.AppendLine("    ]");
            builder.AppendLine("}");
            string assemblyFullName = type.Assembly.GetName().Name;
            string jsonName = assemblyFullName + "\\Level_Master.json";
            File.WriteAllText(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, jsonName), builder.ToString());
        }

        public delegate void OnValueChangedDelegate();
        public static event OnValueChangedDelegate OnValueChangedEvent;
    }
}
