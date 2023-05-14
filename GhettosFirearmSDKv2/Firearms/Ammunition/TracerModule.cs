using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class TracerModule : MonoBehaviour
    {
        public Cartridge cartridge;
        public float speed;
        public GameObject tracerObject;

        public void Start()
        {
            cartridge.OnFiredWithHitPointsAndMuzzle += Fire;
        }

        public void Fire(List<Vector3> hitPoints, List<Vector3> trajectories, Transform muzzle)
        {
            transform.SetParent(null);
            gameObject.SetActive(true);
            for (int i = 0; i < hitPoints.Count; i++)
            {
                GameObject obj = Instantiate(tracerObject);

                obj.SetActive(true);
                obj.transform.position = muzzle.position;
                obj.GetComponent<Tracer>().Fire(muzzle, hitPoints[i], trajectories[i], speed);
            }
        }

        public IEnumerator DelayedDestroy()
        {
            yield return new WaitForSeconds(20f);
            Destroy(gameObject);
        }
    }
}