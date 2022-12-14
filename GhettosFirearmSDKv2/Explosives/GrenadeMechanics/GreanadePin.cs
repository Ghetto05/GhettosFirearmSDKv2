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

        void Awake()
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

        public override bool isUnlocked()
        {
            return broken;
        }

        void OnJointBreak(float breakForce)
        {
            broken = true;
            Util.PlayRandomAudioSource(pullSounds);
            this.GetComponent<Rigidbody>().useGravity = true;
            this.transform.SetParent(null);
        }
    }
}
