using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Explosives
{
    public class Explosive : MonoBehaviour
    {
        public Explosive followUpExplosive;
        public float followUpDelay;
        public bool detonated;
        public Item item;
        public Vector3 impactNormal;

        public virtual void Detonate(float delay)
        {
            if (detonated) return;
            if (delay > 0f)
            {
                StartCoroutine(Delay(delay));
            }
        }

        public virtual void Detonate()
        {
            if (detonated) return;
            ActualDetonate();
        }

        public virtual void ActualDetonate()
        {
            detonated = true;
            if (followUpExplosive != null)
                followUpExplosive.Detonate(followUpDelay);
        }

        public static IEnumerator DelayedDestroy(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(obj);
        }

        public IEnumerator Delay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Detonate();
        }
    }
}