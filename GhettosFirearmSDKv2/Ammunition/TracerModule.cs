using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class TracerModule : MonoBehaviour
    {
        public Cartridge cartridge;
        public float speed;
        private bool fired = false;
        public GameObject tracerObject;
        private List<GameObject> initializedObjects;
        private List<Vector3> hits;
        private Quaternion[] previousRotations;
        private float startTime;

        public void Awake()
        {
            cartridge.OnFiredWithHitPointsAndMuzzle += Fire;
            startTime = Time.time;
        }

        public void Fire(List<Vector3> hitPoints, Transform muzzle)
        {
            Debug.Log("Firing!");
            hits = hitPoints;
            initializedObjects = new List<GameObject>();
            previousRotations = new Quaternion[hitPoints.Count];
            for (int i = 0; i < hitPoints.Count; i++)
            {
                GameObject obj = Instantiate(tracerObject);
                obj.transform.position = muzzle.position;
                obj.name = i.ToString();
                initializedObjects.Add(obj);
                obj.transform.SetParent(null);
                obj.GetComponent<Tracer>().tracerRenderer.enabled = true;
                if (hitPoints[i] == Vector3.zero) obj.transform.eulerAngles = muzzle.forward;
                else obj.transform.LookAt(hitPoints[i]);
                previousRotations[i] = obj.transform.rotation;
            }
            transform.SetParent(null);
            gameObject.SetActive(true);
            fired = true;
        }

        private void Update()
        {
            if (fired)
            {
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (GameObject obj in initializedObjects)
                {
                    if (hits[int.Parse(obj.name)] != Vector3.zero) obj.transform.LookAt(hits[int.Parse(obj.name)]);

                    if (previousRotations[int.Parse(obj.name)] != obj.transform.rotation || Time.time - startTime > 10f)
                    {
                        toDestroy.Add(obj);
                    }
                    else
                    {
                        previousRotations[int.Parse(obj.name)] = obj.transform.rotation;
                        obj.transform.Translate(obj.transform.forward * (speed * Time.deltaTime) / 2, Space.World);
                    }
                }
                foreach (GameObject obj in toDestroy)
                {
                    initializedObjects.Remove(obj);
                    Destroy(obj);
                }
                if (initializedObjects.Count == 0) Destroy(this.gameObject);
            }
        }
    }
}