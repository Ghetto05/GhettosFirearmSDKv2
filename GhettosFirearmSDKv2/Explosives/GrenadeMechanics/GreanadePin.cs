using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class GreanadePin : Lock
    {
        public AudioSource[] pullSounds;
        private bool _broken;
        public Item parentItem;
        public Handle handle;
        public Joint joint;
        public float breakForce;

        private void Start()
        {
            parentItem.OnGrabEvent += ParentItem_OnGrabEvent;
            parentItem.OnUngrabEvent += ParentItem_OnUngrabEvent;
            joint.breakForce = Mathf.Infinity;
        }

        private void ParentItem_OnUngrabEvent(Handle handle2, RagdollHand ragdollHand, bool throwing)
        {
            if (handle2 == handle && joint != null)
            {
                joint.breakForce = Mathf.Infinity;
            }
        }

        private void ParentItem_OnGrabEvent(Handle handle2, RagdollHand ragdollHand)
        {
            if (handle2 == handle && joint != null)
            {
                joint.breakForce = breakForce;
            }
        }

        public override bool GetIsUnlocked()
        {
            return _broken;
        }

        private void OnJointBreak(float force)
        {
            _broken = true;
            InvokeChange();
            Util.PlayRandomAudioSource(pullSounds);
            GetComponent<Rigidbody>().useGravity = true;
            transform.SetParent(null);
        }
    }
}
