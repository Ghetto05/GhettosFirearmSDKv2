using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class LineAttacher : MonoBehaviour
    {
        public Transform target;
        public LineRenderer line;

        void Update()
        {
            line.SetPosition(0, transform.position);
            line.SetPosition(1, target.position);    
        }
    }
}
