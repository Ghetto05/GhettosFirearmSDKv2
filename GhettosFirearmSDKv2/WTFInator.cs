using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class WTFInator : MonoBehaviour
    {
        public List<MeshRenderer> meshRenderers;
        public List<GameObject> objects;
        
        void Update()
        {
            foreach (MeshRenderer ob in meshRenderers)
            {
                //Debug.Log("mesh " + ob.name + " " + (ob != null) + " " + ob.enabled + " " + (ob.gameObject.activeInHierarchy && ob.gameObject.activeSelf));
            }
        }
    }
}
