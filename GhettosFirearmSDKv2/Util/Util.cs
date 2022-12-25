using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Util : MonoBehaviour
    {
        public void LoadAudioData(AudioSource source)
        {
            if (source == null || source.clip == null) return;
            source.clip.LoadAudioData();
        }

        public static bool AllowLoadCatridge(Cartridge cartridge, string requiredCaliber)
        {
            if (!Settings_LevelModule.local.doCaliberChecks) return true;
            if (cartridge.caliber.Equals("DEBUG UNIVERSAL")) return true;

            return cartridge.caliber.Equals(requiredCaliber);
        }

        public static bool AllowLoadCatridge(Cartridge cartridge, Magazine magazine)
        {
            if (!Settings_LevelModule.local.doCaliberChecks) return true;
            if (cartridge.caliber.Equals("DEBUG UNIVERSAL")) return true;
            bool correctCaliber = cartridge.caliber.Equals(magazine.caliber) || ListContainsString(magazine.alternateCalibers, cartridge.caliber);
            bool magHasSameCaliber = magazine.cartridges.Count == 0 || magazine.cartridges[0].caliber.Equals(cartridge.caliber);
            return correctCaliber && magHasSameCaliber;
        }

        public static bool AllowLoadCatridge(string cartridgeCaliber, Magazine magazine)
        {
            if (!Settings_LevelModule.local.doCaliberChecks) return true;
            if (cartridgeCaliber.Equals("DEBUG UNIVERSAL")) return true;
            bool correctCaliber = cartridgeCaliber.Equals(magazine.caliber) || ListContainsString(magazine.alternateCalibers, cartridgeCaliber);
            bool magHasSameCaliber = magazine.cartridges.Count == 0 || magazine.cartridges[0].caliber.Equals(cartridgeCaliber);
            return correctCaliber && magHasSameCaliber;
        }

        public static bool AllowLoadMagazine(Magazine magazine, MagazineWell well)
        {
            if (magazine.currentWell != null || magazine.item.holder != null || well.currentMagazine != null) return false;
            if (!Settings_LevelModule.local.doMagazineTypeChecks) return true;
            if (magazine.magazineType.Equals("DEBUG UNIVERSAL")) return true;

            bool sameType = magazine.magazineType.Equals(well.acceptedMagazineType);
            bool compatibleCaliber = (magazine.cartridges.Count == 0) || !Settings_LevelModule.local.doCaliberChecks || ((well.caliber.Equals(magazine.cartridges[0].caliber))||(ListContainsString(well.alternateCalibers, magazine.cartridges[0].caliber)));

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
                    cr.brain.SetState(Brain.State.Investigate);
                }
            }
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
            foreach (Collider c1 in obj1.GetComponentsInChildren<Collider>())
            {
                foreach (Collider c2 in obj2.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(c1, c2, ignore);
                }
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
            yield break;
        }

        public static void PlayRandomAudioSource(List<AudioSource> sources) => PlayRandomAudioSource(sources.ToArray());

        public static void PlayRandomAudioSource(AudioSource[] sources)
        {
            if (sources == null || sources.Length == 0) return;
            int i = Random.Range(0, sources.Length);
            if (sources[i] != null) sources[i].Play();
        }

        public static float AbsDist(Vector3 v1, Vector3 v2)
        {
            return Mathf.Abs(Vector3.Distance(v1, v2));
        }
    }
}
