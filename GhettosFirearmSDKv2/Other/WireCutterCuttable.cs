using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private bool cut = false;

        public static void CutFound(Vector3 root, float range)
        {
            Dictionary<Collider, bool> states = new Dictionary<Collider, bool>();
            
            foreach (WireCutterCuttable cuttable in All)
            {
                foreach (Collider collider in cuttable.cuttableColliders)
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

            Collider[] results = new Collider[0];
            Physics.OverlapSphereNonAlloc(root, range, results);
            
            List<WireCutterCuttable> found = new List<WireCutterCuttable>();
            foreach (Collider c in results)
            {
                if (c.GetComponentInParent<WireCutterCuttable>() is { } wcc && !found.Contains(wcc) && !wcc.cut)
                {
                    found.Add(wcc);
                    wcc.cut = true;
                    wcc.onCut.Invoke();
                }
            }
            
            foreach (KeyValuePair<Collider, bool> pair in states)
            {
                pair.Key.enabled = pair.Value;
            }
        }
    }
}
