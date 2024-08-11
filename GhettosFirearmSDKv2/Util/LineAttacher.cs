using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class LineAttacher : MonoBehaviour
    {
        public Transform target;
        public LineRenderer line;

        private void Update()
        {
            line.SetPosition(0, transform.position);
            line.SetPosition(1, target.position);    
        }
    }
}
