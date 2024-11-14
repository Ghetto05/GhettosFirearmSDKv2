using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class WireCutterCuttable : MonoBehaviour
    {
        private static List<WireCutterCuttable> _all;
        public static List<WireCutterCuttable> All
        {
            get
            {
                if (_all == null)
                    _all = new List<WireCutterCuttable>();
                return _all;
            }
            set
            {
                _all = value;
            }
        }

        public UnityEvent onCut;
        public Collider[] cuttableColliders;
        private bool _cut;

        private void Start()
        {
            All.Add(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        public static void CutFound(Vector3 root, float range)
        {
            var states = new Dictionary<Collider, bool>();
            
            foreach (var cuttable in All)
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
                Debug.Log("Cuttables - " + _all.Count + " " + results.Length);
            
            var found = new List<WireCutterCuttable>();
            foreach (var c in results)
            {
                if (c.GetComponentInParent<WireCutterCuttable>() is { } wcc && !found.Contains(wcc) && !wcc._cut)
                {
                    found.Add(wcc);
                    wcc._cut = true;
                    wcc.onCut.Invoke();
                    if (Settings.debugMode)
                        Debug.Log("Cut a cuttable");
                }
            }
            
            foreach (var pair in states)
            {
                pair.Key.enabled = pair.Value;
            }
        }
    }
}
