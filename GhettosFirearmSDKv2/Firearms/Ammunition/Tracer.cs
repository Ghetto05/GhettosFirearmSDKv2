using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class Tracer : MonoBehaviour
    {
        public enum Colors
        {
            Red,
            Green,
            Blue,
            Orange,
            White
        }

        public Colors color;
        [HideInInspector]
        public Material redMaterial;
        [HideInInspector]
        public Material greenMaterial;
        [HideInInspector]
        public Material blueMaterial;
        [HideInInspector]
        public Material orangeMaterial;
        [HideInInspector]
        public Material whiteMaterial;
        public List<MeshRenderer> tracerRenderers;

        bool fired = false;
        Vector3 dir;
        float speed;

        private void OnValidate()
        {
            foreach (Renderer tracerRenderer in tracerRenderers)
            {
                tracerRenderer.material = getMaterial(color);
            }
        }

        public Material getMaterial(Colors targetColor)
        {
            if (targetColor == Colors.Red) return redMaterial;
            else if (targetColor == Colors.Green) return greenMaterial;
            else if (targetColor == Colors.Blue) return blueMaterial;
            else if (targetColor == Colors.Orange) return orangeMaterial;
            else if (targetColor == Colors.White) return whiteMaterial;
            else return null;
        }

        public void DestroyDelayed(float delay)
        {
            StartCoroutine(DestroyDelayedIE(delay));
        }

        private IEnumerator DestroyDelayedIE(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        public void Fire(Transform muzzle, Vector3 hit, Vector3 direction, float pSpeed)
        {
            dir = direction;
            speed = pSpeed;
            if (hit == Vector3.one)
            {
                StartCoroutine(DestroyDelayedIE(10f));
            }
            else
            {
                StartCoroutine(DestroyDelayedIE(Vector3.Distance(muzzle.position, hit) / speed));
            }
            fired = true;
        }

        public void Update()
        {
            if (fired) transform.position += dir * speed * Time.deltaTime;
        }
    }
}
