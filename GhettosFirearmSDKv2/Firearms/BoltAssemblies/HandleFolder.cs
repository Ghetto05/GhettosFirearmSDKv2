using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Bolt assemblies/Handle folder")]
public class HandleFolder : MonoBehaviour
{
    public Transform axis;
    public List<Handle> handles;
    public List<string> handleNames;
    public Transform defaultPosition;
    public List<Transform> positions;
    public bool parentToPosition;
    public bool foldIfBoltCaught;
    public BoltSemiautomatic bolt;

    private void Start()
    {
        if (handleNames.Count > 0)
        {
            handles = new List<Handle>();
            for (var i = 0; i < handleNames.Count; i++)
            {
                var it = GetComponentInParent<Item>();
                for (var x = 0; x < it.handles.Count; x++)
                {
                    if (it.handles[x].gameObject.name.Equals(handleNames[i])) handles.Add(it.handles[x]);
                }
            }
        }
    }

    private bool _firstDebug = true;
    private void Update()
    {
        var held = false;
        foreach (var h in handles)
        {
            if (!h)
            { 
                if (_firstDebug)
                    Debug.LogError("Null handle on handle folder on " + (GetComponentInParent<Attachment>()?.name ?? GetComponentInParent<Item>()?.name));
                _firstDebug = false;
                return;
            }

            if (h.IsHanded() || (foldIfBoltCaught && bolt && bolt.caught))
            {
                held = true;
                try
                {
                    if (parentToPosition)
                    {
                        axis.SetParent(positions[handles.IndexOf(h)]);
                        axis.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    }
                    else
                    {
                        axis.localPosition = positions[handles.IndexOf(h)].localPosition;
                        axis.localEulerAngles = positions[handles.IndexOf(h)].localEulerAngles;
                    }
                }
                catch (Exception)
                {
                    if (!positions.Any())
                        Debug.Log($"Position array is empty! (Handle folder ---- {transform.name} ---- {(GetComponentInParent<Attachment>() != null ? GetComponentInParent<Attachment>().gameObject.name : GetComponentInParent<Item>().gameObject.name)})");
                    else if (!handles.Any())
                        Debug.Log($"Handle array is empty! (Handle folder ---- {transform.name} ---- {(GetComponentInParent<Attachment>() != null ? GetComponentInParent<Attachment>().gameObject.name : GetComponentInParent<Item>().gameObject.name)})");
                    else
                        Debug.Log($"Unknown error! (Handle folder ---- {transform.name} ---- {(GetComponentInParent<Attachment>() != null ? GetComponentInParent<Attachment>().gameObject.name : GetComponentInParent<Item>().gameObject.name)})");
                }
            }
        }
        if (!held && defaultPosition != null)
        {
            if (parentToPosition)
            {
                axis.SetParent(defaultPosition);
                axis.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                axis.localPosition = defaultPosition.localPosition;
                axis.localEulerAngles = defaultPosition.localEulerAngles;
            }
        }
    }
}