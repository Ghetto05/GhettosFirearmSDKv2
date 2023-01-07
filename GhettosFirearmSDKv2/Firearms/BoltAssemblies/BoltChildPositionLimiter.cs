using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Bolt assemblies/Bolt child position limiter")]
    public class BoltChildPositionLimiter : MonoBehaviour
    {
        public Transform boltToFollow;
        public Transform frontEnd;
        public Transform backEnd;
        public Transform objectToBeMoved;

        void Update()
        {
            if (boltToFollow.localPosition.z <= frontEnd.localPosition.z && boltToFollow.localPosition.z >= backEnd.localPosition.z)
            {
                objectToBeMoved.localPosition = new Vector3(objectToBeMoved.localPosition.x, objectToBeMoved.localPosition.y, boltToFollow.localPosition.z);
            }
            else if (boltToFollow.localPosition.z >= frontEnd.localPosition.z)
            {
                objectToBeMoved.localPosition = frontEnd.localPosition;
            }
            else if (boltToFollow.localPosition.z <= backEnd.localPosition.z)
            {
                objectToBeMoved.localPosition = backEnd.localPosition;
            }
        }
    }
}
