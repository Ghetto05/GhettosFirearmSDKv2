using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2;

public class WireCutterCuttable : MonoBehaviour
{
    public static List<WireCutterCuttable> all = [];

    public UnityEvent onCut;
    public Collider[] cuttableColliders;
    private bool _cut;

    private void Start()
    {
        all.Add(this);
    }

    private void OnDestroy()
    {
        all.Remove(this);
    }

    public static void CutFound(Vector3 root, float range)
    {
        var states = new Dictionary<Collider, bool>();

        foreach (var cuttable in all)
        {
            foreach (var collider in cuttable.cuttableColliders)
            {
                try
                {
                    states.Add(collider, collider.enabled);
                    collider.enabled = true;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        var results = Physics.OverlapSphere(root, range);
        if (Settings.debugMode)
        {
            Debug.Log("Cuttables - " + all.Count + " " + results.Length);
        }

        var found = new List<WireCutterCuttable>();
        foreach (var c in results)
        {
            if (c.GetComponentInParent<WireCutterCuttable>() is { } wcc && !found.Contains(wcc) && !wcc._cut)
            {
                found.Add(wcc);
                wcc._cut = true;
                wcc.onCut.Invoke();
                if (Settings.debugMode)
                {
                    Debug.Log("Cut a cuttable");
                }
            }
        }

        foreach (var pair in states)
        {
            pair.Key.enabled = pair.Value;
        }
    }
}