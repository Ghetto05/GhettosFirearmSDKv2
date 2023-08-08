using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Illuminators/NVG Only Renderer - Mesh module")]
    public class NVGOnlyRendererMeshModule : MonoBehaviour
    {
        public static List<NVGOnlyRendererMeshModule> all;

        public NVGOnlyRenderer.Types renderType;
        public List<GameObject> objects;

        public void Start()
        {
            if (all == null) all = new List<NVGOnlyRendererMeshModule>();
            all.Add(this);
            foreach (GameObject obj in objects)
            {
                obj.SetActive(false);
            }
        }

        public void OnDestroy()
        {
            all.Remove(this);
        }
    }
}
