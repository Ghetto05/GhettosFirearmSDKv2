using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2
{
    public class Util
    {
        public static Vector3 RandomCartridgeRotation()
        {
            return new Vector3(0, 0, Random.Range(0f, 360f));
        }
        
        public static Vector3 RandomRotation()
        {
            return new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
        }
        
        public static void RandomizeZRotation(Transform target)
        {
            var randomAngle = Random.Range(0f, 360f);
            target.Rotate(0f, 0f, randomAngle, Space.Self);
        }

        public static bool AllowLoadCartridge(Cartridge cartridge, string requiredCaliber)
        {
            return CheckCaliberMatch(cartridge.caliber, requiredCaliber);
        }

        public static bool AllowLoadCartridge(Cartridge cartridge, IAmmunitionLoadable magazine)
        {
            if (!Settings.doCaliberChecks && !magazine.GetForceCorrectCaliber())
                return true;

            return AllowLoadCartridge(cartridge.caliber, magazine);
        }

        public static bool AllowLoadCartridge(string cartridgeCaliber, IAmmunitionLoadable magazine, bool ignoreSameCaliber = false)
        {
            var correctCaliber = 
                CheckCaliberMatch(cartridgeCaliber, magazine.GetCaliber(), magazine.GetForceCorrectCaliber()) || 
                CheckCaliberMatch(cartridgeCaliber, magazine.GetAlternativeCalibers(), magazine.GetForceCorrectCaliber());
            var magHasSameCaliber = ignoreSameCaliber || magazine.GetLoadedCartridges() == null || !magazine.GetLoadedCartridges().Any() || (magazine.GetLoadedCartridges().FirstOrDefault()?.caliber.Equals(cartridgeCaliber) ?? true);
            return correctCaliber && magHasSameCaliber;
        }

        public static bool AllowLoadCartridge(string cartridgeCaliber, string otherCaliber)
        {
            return CheckCaliberMatch(cartridgeCaliber, otherCaliber);
        }
        
        public static bool CheckCaliberMatch(string insertedCaliber, List<string> targetCalibers, bool ignoreCheat = false)
        {
            if (targetCalibers == null)
                return false;
            
            foreach (var targetCaliber in targetCalibers)
            {
                if (CheckCaliberMatch(insertedCaliber, targetCaliber, ignoreCheat))
                    return true;
            }

            return false;
        }

        public static bool CheckCaliberMatch(string insertedCaliber, string targetCaliber, bool ignoreCheat = false)
        {
            if (!Settings.doCaliberChecks && !ignoreCheat)
                return true;
            if (insertedCaliber.Equals("DEBUG UNIVERSAL"))
                return true;
            return insertedCaliber.Equals(targetCaliber) || CaliberSubstituteData.AllowSubstitution(insertedCaliber, targetCaliber);
        }

        public static bool AllowLoadMagazine(Magazine magazine, MagazineWell well)
        {
            if (magazine.currentWell != null || magazine.item.holder != null || well.currentMagazine != null) return false;
            if (!Settings.doMagazineTypeChecks) return true;
            if (magazine.magazineType.Equals("DEBUG UNIVERSAL")) return true;

            var sameType = magazine.magazineType.Equals(well.acceptedMagazineType);
            foreach (var t in well.alternateMagazineTypes)
            {
                if (t.Equals(magazine.magazineType)) sameType = true;
            }
            var compatibleCaliber = magazine.cartridges.Count == 0 ||
                                    !Settings.doCaliberChecks ||
                                    well.caliber.Equals(magazine.cartridges[0].caliber) ||
                                    well.alternateCalibers.Any(x => x.Equals(magazine.cartridges[0].caliber));

            return sameType && compatibleCaliber;
        }

        public static void AlertAllCreaturesInRange(Vector3 point, float range)
        {
            foreach (var cr in Creature.allActive)
            {
                if (cr.animator.GetBoneTransform(HumanBodyBones.Head) is { } bone && Vector3.Distance(bone.position, point) <= range)
                {
                    cr.brain.SetState(Brain.State.Alert);
                }
            }
        }

        public static bool CheckForCollisionWithColliders(List<Collider> theseColliders, List<Collider> otherColliders, Collision collision)
        {
            if (theseColliders != null && otherColliders == null)
            {
                foreach (var con in collision.contacts)
                {
                    foreach (var c in theseColliders)
                    {
                        if (c == con.thisCollider) return true;
                    }
                }
            }
            else if (theseColliders == null && otherColliders != null)
            {
                foreach (var con in collision.contacts)
                {
                    foreach (var c in otherColliders)
                    {
                        if (c == con.otherCollider) return true;
                    }
                }
            }
            else if (theseColliders != null)
            {
                foreach (var con in collision.contacts)
                {
                    var thisColliderFound = false;
                    foreach (var thisCollider in theseColliders)
                    {
                        if (thisCollider == con.thisCollider) thisColliderFound = true;
                    }

                    foreach (var otherCollider in otherColliders)
                    {
                        if (otherCollider == con.otherCollider && thisColliderFound) return true;
                    }
                }
            }

            return false;
        }

        public static bool CheckForCollisionWithThisCollider(Collision collision, Collider thisCollider)
        {
            foreach (var con in collision.contacts)
            {
                if (con.thisCollider == thisCollider) return true;
            }
            return false;
        }

        public static bool CheckForCollisionWithOtherCollider(Collision collision, Collider otherCollider)
        {
            foreach (var con in collision.contacts)
            {
                if (con.otherCollider == otherCollider) return true;
            }
            return false;
        }

        public static bool CheckForCollisionWithBothColliders(Collision collision, Collider thisCollider, Collider otherCollider)
        {
            foreach (var con in collision.contacts)
            {
                if (con.thisCollider == thisCollider && con.otherCollider == otherCollider) return true;
            }
            return false;
        }

        public static void IgnoreCollision(GameObject obj1, GameObject obj2, bool ignore)
        {
            if (obj1 == null || obj2 == null) return;
            try
            {
                foreach (var c1 in obj1.GetComponentsInChildren<Collider>())
                {
                    foreach (var c2 in obj2.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(c1, c2, ignore);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void DelayIgnoreCollision(GameObject obj1, GameObject obj2, bool ignore, float delay, Item handler)
        {
            handler.StartCoroutine(DelayIgnoreCollisionCoroutine(obj1, obj2, ignore, delay));
        }

        private static IEnumerator DelayIgnoreCollisionCoroutine(GameObject obj1, GameObject obj2, bool ignore, float delay)
        {
            yield return new WaitForSeconds(delay);
            IgnoreCollision(obj1, obj2, ignore);
        }

        public static AudioSource PlayRandomAudioSource(List<AudioSource> sources)
        {
            var source = GetRandomFromList(sources);
            if (source != null)
            {
                source.PlayOneShot(source.clip);
                return source;
            }
            return null;
        }

        public static AudioSource PlayRandomAudioSource(AudioSource[] sources) => PlayRandomAudioSource(sources.ToList());

        public static T GetRandomFromList<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default;
            var i = Random.Range(0, list.Count);
            return list[i];
        }

        public static T GetRandomFromList<T>(IList<T> array)
        {
            if (array == null) return default;
            return GetRandomFromList(array.ToList());
        }

        public static float AbsDist(Vector3 v1, Vector3 v2)
        {
            return Mathf.Abs(Vector3.Distance(v1, v2));
        }

        public static float AbsDist(Transform v1, Transform v2)
        {
            return Mathf.Abs(Vector3.Distance(v1.position, v2.position));
        }

        public static void DelayedExecute(float delay, Action action, MonoBehaviour handler)
        {
            handler.StartCoroutine(DelayedExecuteIE(delay, action));
        }

        private static IEnumerator DelayedExecuteIE(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }

        public static bool AllLocksUnlocked(List<Lock> locks)
        {
            foreach (var l in locks)
            {
                if (!l.IsUnlocked()) return false;
            }
            return true;
        }

        public static Transform RecursiveFindChild(Transform parent, string childName)
        {
            foreach (Transform parent1 in parent)
            {
                if (parent1.gameObject.name.Equals(childName))
                    return parent1;
                var child = RecursiveFindChild(parent1, childName);
                if (child != null)
                    return child;
            }
            return null;
        }
        
        public static void UpdateLightVolumeReceiver(LightVolumeReceiver receiverToBeUpdated, LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
        {
            var method = typeof(LightVolumeReceiver).GetMethod("OnParentVolumeChange", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(receiverToBeUpdated, new object[] { currentLightProbeVolume, lightProbeVolumes });
        }
        
        /**
         * <summary>
         * Function to normalize angles to be within [-180, 180]
         * </summary>>
         */
        public static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        public static void DisableCollision(Item item, bool disable)
        {
            foreach (var c in item.colliderGroups.SelectMany(i => i.colliders))
            {
                c.enabled = !disable;
            }
        }

        public static void ApplyAudioConfig(ICollection<AudioSource> sources, bool suppressed = false)
        {
            if (Player.local == null || Player.local.head == null || Player.local.head.cam == null)
                return;
            
            float range = 800;
            foreach (var source in sources)
            {
                source.spatialBlend = 0.1f;
                if (suppressed)
                {
                    source.volume = 0.7f;
                    source.maxDistance = 50f;
                }
                else
                {
                    source.volume = 1f;
                    source.maxDistance = 100f;
                }

                var distance = Vector3.Distance(source.transform.position, Player.local.head.cam.transform.position);
                if (distance <= 2)
                    source.volume *= 1;
                else if (distance >= range)
                    source.volume *= 0;
                else
                {
                    var decayFactor = Mathf.Exp(-distance / 100);
                    source.volume *= (1f - 0.3f) * decayFactor;
                }
                    //source.volume *= 1 - (distance / range);
            }
        }

        public static void SpawnItem(string id, string handler,
                                     Action<Item> callback,
                                     Vector3? position = null,
                                     Quaternion? rotation = null,
                                     Transform parent = null,
                                     bool pooled = true,
                                     List<ContentCustomData> customDataList = null)
        {
            Catalog.GetData<ItemData>(GetSubstituteId(id, handler), Settings.debugMode)?.SpawnAsync(callback, position, rotation, parent, pooled, customDataList);
        }

        private static ObsoleteIdData _obsoleteIdData;
        private static bool _triedLoadingObsoleteIds;
        public static string GetSubstituteId(string id, string handler)
        {
            if (_obsoleteIdData == null && !_triedLoadingObsoleteIds)
            {
                _triedLoadingObsoleteIds = true;
                _obsoleteIdData = Catalog.GetDataList<ObsoleteIdData>().FirstOrDefault();
            }

            if (_obsoleteIdData != null)
            {
                if (_obsoleteIdData.IdMatches.TryGetValue(id, out var substituteId))
                {
                    if (Settings.debugMode)
                    {
                        Debug.Log($"OBSOLETE ID! {id} was replaced by {substituteId}! Handler: {handler}");
                    }

                    return substituteId;
                }
            }
            return id;
        }
        
        public static void DebugObject(object obj, string debugName)
        {
            var output = new StringBuilder();

            if (obj == null)
            {
                Debug.Log("The object is null.");
                return;
            }

            output.AppendLine(debugName);
            output.AppendLine();

            var type = obj.GetType();
            output.AppendLine("Object Type: " + type.Name);

            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(obj, null);
                    output.AppendLine(property.Name + " (Property): " + value);
                }
                catch (Exception e)
                {
                    output.AppendLine("Could not retrieve the value of property: " + property.Name + ". Error: " + e.Message);
                }
            }

            var fields = type.GetFields();
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(obj);
                    output.AppendLine(field.Name + " (Field): " + value);
                }
                catch (Exception e)
                {
                    output.AppendLine("Could not retrieve the value of field: " + field.Name + ". Error: " + e.Message);
                }
            }

            Debug.Log(output.ToString());
        }
    }
}
