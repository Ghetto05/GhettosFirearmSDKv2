using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Bolt assemblies/Handle folder")]
    public class HandleFolder : MonoBehaviour
    {
        public Transform axis;
        public List<Handle> handles;
        public List<string> handleNames;
        public Transform defaultPosition;
        public List<Transform> positions;

        private void Start()
        {
            if (handleNames.Count > 0)
            {
                handles = new List<Handle>();
                for (int i = 0; i < handleNames.Count; i++)
                {
                    Item it = GetComponentInParent<Item>();
                    for (int x = 0; x < it.handles.Count; x++)
                    {
                        if (it.handles[x].gameObject.name.Equals(handleNames[i])) handles.Add(it.handles[x]);
                    }
                }
            }
        }

        private void Update()
        {
            bool held = false;
            foreach (Handle h in handles)
            {
                if (h.IsHanded())
                {
                    held = true;
                    axis.localPosition = positions[handles.IndexOf(h)].localPosition;
                    axis.localEulerAngles = positions[handles.IndexOf(h)].localEulerAngles;
                }
            }
            if (!held && defaultPosition != null)
            {
                axis.localPosition = defaultPosition.localPosition;
                axis.localEulerAngles = defaultPosition.localEulerAngles;
            }
        }
    }
}
