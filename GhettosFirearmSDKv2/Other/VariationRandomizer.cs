using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class VariationRandomizer : MonoBehaviour
    {
        public GameObject[] variants;

        private void Start()
        {
            foreach (var v in variants)
            {
                v.SetActive(false);
            }

            variants[Random.Range(0, variants.Length)].SetActive(true);
        }
    }
}