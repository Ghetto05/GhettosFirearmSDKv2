using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2.Explosives
{
    public class Explosive : MonoBehaviour
    {
        public bool detonated = false;
        public Item item;

        public virtual void Detonate(float delay = 0f)
        {
            if (detonated) return;
            if (delay > 0f)
            {
                StartCoroutine(Delay(delay));
                return;
            }

            if (detonated) return;
            detonated = true;
            ActualDetonate();
        }

        public virtual void ActualDetonate()
        {}

        public static IEnumerator delayedDestroy(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(obj);
        }

        public IEnumerator Delay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ActualDetonate();
        }
    }
}