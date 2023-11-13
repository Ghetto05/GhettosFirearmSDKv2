using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static bool AllowLoadCatridge(Cartridge cartridge, string requiredCaliber)
        {
            if (!FirearmsSettings.doCaliberChecks) return true;
            if (cartridge.caliber.Equals("DEBUG UNIVERSAL")) return true;

            return cartridge.caliber.Equals(requiredCaliber);
        }

        public static bool AllowLoadCatridge(Cartridge cartridge, Magazine magazine)
        {
            if (!FirearmsSettings.doCaliberChecks && !magazine.forceCorrectCaliber) return true;
            if (cartridge.caliber.Equals("DEBUG UNIVERSAL")) return true;
            bool correctCaliber = cartridge.caliber.Equals(magazine.caliber) || ListContainsString(magazine.alternateCalibers, cartridge.caliber);
            bool magHasSameCaliber = magazine.cartridges.Count == 0 || magazine.cartridges[0].caliber.Equals(cartridge.caliber);
            return correctCaliber && magHasSameCaliber;
        }

        public static bool AllowLoadCatridge(string cartridgeCaliber, Magazine magazine)
        {
            if (!FirearmsSettings.doCaliberChecks) return true;
            if (cartridgeCaliber.Equals("DEBUG UNIVERSAL")) return true;
            bool correctCaliber = cartridgeCaliber.Equals(magazine.caliber) || ListContainsString(magazine.alternateCalibers, cartridgeCaliber);
            bool magHasSameCaliber = magazine.cartridges.Count == 0 || magazine.cartridges[0].caliber.Equals(cartridgeCaliber);
            return correctCaliber && magHasSameCaliber;
        }

        public static bool AllowLoadCatridge(string cartridgeCaliber, string otherCaliber)
        {
            if (!FirearmsSettings.doCaliberChecks) return true;
            if (cartridgeCaliber.Equals("DEBUG UNIVERSAL")) return true;
            bool correctCaliber = cartridgeCaliber.Equals(otherCaliber);
            return correctCaliber;
        }

        public static bool AllowLoadMagazine(Magazine magazine, MagazineWell well)
        {
            if (magazine.currentWell != null || magazine.item.holder != null || well.currentMagazine != null) return false;
            if (!FirearmsSettings.doMagazineTypeChecks) return true;
            if (magazine.magazineType.Equals("DEBUG UNIVERSAL")) return true;

            bool sameType = magazine.magazineType.Equals(well.acceptedMagazineType);
            foreach (string t in well.alternateMagazineTypes)
            {
                if (t.Equals(magazine.magazineType)) sameType = true;
            }
            bool compatibleCaliber = (magazine.cartridges.Count == 0) || !FirearmsSettings.doCaliberChecks || ((well.caliber.Equals(magazine.cartridges[0].caliber))||(ListContainsString(well.alternateCalibers, magazine.cartridges[0].caliber)));

            return sameType && compatibleCaliber;
        }

        public static bool ListContainsHandle(List<Handle> list, Handle handle)
        {
            foreach (Handle h in list)
            {
                if (handle == h) return true;
            }
            return false;
        }

        public static bool ListContainsString(List<string> list, string str)
        {
            foreach (string s in list)
            {
                if (s.Equals(str)) return true;
            }
            return false;
        }

        public static void AlertAllCreaturesInRange(Vector3 point, float range)
        {
            foreach (Creature cr in Creature.allActive)
            {
                if (Vector3.Distance(cr.animator.GetBoneTransform(HumanBodyBones.Neck).position, point) <= range)
                {
                    cr.brain.SetState(Brain.State.Alert);
                }
            }
        }

        public static bool CheckForCollisionWithColliders(List<Collider> theseColliders, List<Collider> otherColliders, Collision collision)
        {
            if (theseColliders != null && otherColliders == null)
            {
                foreach (ContactPoint con in collision.contacts)
                {
                    foreach (Collider c in theseColliders)
                    {
                        if (c == con.thisCollider) return true;
                    }
                }
            }
            else if (theseColliders == null && otherColliders != null)
            {
                foreach (ContactPoint con in collision.contacts)
                {
                    foreach (Collider c in otherColliders)
                    {
                        if (c == con.otherCollider) return true;
                    }
                }
            }
            else if (theseColliders != null && otherColliders != null)
            {
                foreach (ContactPoint con in collision.contacts)
                {
                    bool thisColliderFound = false;
                    foreach (Collider thisCollider in theseColliders)
                    {
                        if (thisCollider == con.thisCollider) thisColliderFound = true;
                    }

                    foreach (Collider otherCollider in otherColliders)
                    {
                        if (otherCollider == con.otherCollider && thisColliderFound) return true;
                    }
                }
            }

            return false;
        }

        public static bool CheckForCollisionWithThisCollider(Collision collision, Collider thisCollider)
        {
            foreach (ContactPoint con in collision.contacts)
            {
                if (con.thisCollider == thisCollider) return true;
            }
            return false;
        }

        public static bool CheckForCollisionWithOtherCollider(Collision collision, Collider otherCollider)
        {
            foreach (ContactPoint con in collision.contacts)
            {
                if (con.otherCollider == otherCollider) return true;
            }
            return false;
        }

        public static bool CheckForCollisionWithBothColliders(Collision collision, Collider thisCollider, Collider otherCollider)
        {
            foreach (ContactPoint con in collision.contacts)
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
                foreach (Collider c1 in obj1.GetComponentsInChildren<Collider>())
                {
                    foreach (Collider c2 in obj2.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(c1, c2, ignore);
                    }
                }
            }
            catch (System.Exception)
            { }
        }

        public static void DelayIgnoreCollision(GameObject obj1, GameObject obj2, bool ignore, float delay, Item handler)
        {
            handler.StartCoroutine(DelayIgnoreCollisionCoroutine(obj1, obj2, ignore, delay));
        }

        private static IEnumerator DelayIgnoreCollisionCoroutine(GameObject obj1, GameObject obj2, bool ignore, float delay)
        {
            yield return new WaitForSeconds(delay);
            IgnoreCollision(obj1, obj2, ignore);
            yield break;
        }

        public static AudioSource PlayRandomAudioSource(List<AudioSource> sources)
        {
            AudioSource source = GetRandomFromList(sources);
            if (source != null)
            {
                source.Play();
                return source;
            }
            return null;
        }

        public static AudioSource PlayRandomAudioSource(AudioSource[] sources) => PlayRandomAudioSource(sources.ToList());

        public static T GetRandomFromList<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default;
            int i = Random.Range(0, list.Count);
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

        public static void DelayedExecute(float delay, System.Action action, MonoBehaviour handler)
        {
            handler.StartCoroutine(DelayedExecuteIE(delay, action));
        }

        private static IEnumerator DelayedExecuteIE(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }

        public static bool AllLocksUnlocked(List<Lock> locks)
        {
            foreach (Lock l in locks)
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
                Transform child = RecursiveFindChild(parent1, childName);
                if (child != null)
                    return child;
            }
            return null;
        }
        
        public static void UpdateLightVolumeReceiver(LightVolumeReceiver receiverToBeUpdated, LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
        {
            MethodInfo method = receiverToBeUpdated.GetType().GetMethod("OnParentVolumeChange", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(receiverToBeUpdated, new object[] { currentLightProbeVolume, lightProbeVolumes });
        }
    }
}
