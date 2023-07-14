using System;
using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;

namespace GhettosFirearmSDKv2
{
    public class GreanadePin : Lock
    {
        public AudioSource[] pullSounds;
        bool broken = false;
        public Item parentItem;
        public Handle handle;
        public Joint joint;
        public float breakForce;

        void Start()
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

        public override bool GetState()
        {
            return broken;
        }

        void OnJointBreak(float breakForce)
        {
            broken = true;
            InvokeChange();
            Util.PlayRandomAudioSource(pullSounds);
            GetComponent<Rigidbody>().useGravity = true;
            transform.SetParent(null);
        }
    }
}
