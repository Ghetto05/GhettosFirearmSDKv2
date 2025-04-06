using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Attachments/Systems/Illuminators/NVG Only Renderer - Mesh module")]
public class NvgOnlyRendererMeshModule : MonoBehaviour
{
    public static List<NvgOnlyRendererMeshModule> all = [];

    public NvgOnlyRenderer.Types renderType;
    public List<GameObject> objects;

    public void Start()
    {
        all.Add(this);
        foreach (var obj in objects)
        {
            obj.SetActive(false);
        }
    }

    public void OnDestroy()
    {
        all.Remove(this);
    }
}