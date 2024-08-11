using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        private bool _fired;
        private Vector3 _dir;
        private float _speed;

        private void OnValidate()
        {
            foreach (Renderer tracerRenderer in tracerRenderers)
            {
                tracerRenderer.material = GetMaterial(color);
            }
        }

        public Material GetMaterial(Colors targetColor)
        {
            if (targetColor == Colors.Red) return redMaterial;

            if (targetColor == Colors.Green) return greenMaterial;

            if (targetColor == Colors.Blue) return blueMaterial;

            if (targetColor == Colors.Orange) return orangeMaterial;
            if (targetColor == Colors.White) return whiteMaterial;

            return null;
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
            _dir = direction;
            _speed = pSpeed;
            if (hit == Vector3.one)
            {
                StartCoroutine(DestroyDelayedIE(10f));
            }
            else
            {
                StartCoroutine(DestroyDelayedIE(Vector3.Distance(muzzle.position, hit) / _speed));
            }
            _fired = true;
        }

        public void Update()
        {
            if (_fired) transform.position += _dir * _speed * Time.deltaTime;
        }
    }
}
