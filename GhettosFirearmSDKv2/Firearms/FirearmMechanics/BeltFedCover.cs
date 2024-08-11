using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class BeltFedCover : Lock
    {
        public bool EDITOR_HELD;
        public float Angle;
        
        private const float Threshold = 4f;
        
        public Transform axis;
        public Rigidbody rb;
        public Transform closedPosition;
        public Transform openedPosition;
        public MagazineWell magazineWell;

        public List<Handle> coverHandles;

        public List<AudioSource> openSounds;
        public List<AudioSource> closeSounds;

        public float minAngle;
        public float maxAngle;
        
        public BoltBase.BoltState state = BoltBase.BoltState.Locked;

        private HingeJoint _joint;
        private bool _locked;
        
        private void Start()
        {
            Invoke(nameof(InvokedStart), 1f);
        }

        private void InvokedStart()
        {
            InitializeJoint();
            Lock();
            foreach (var coverHandle in coverHandles)
            {
                coverHandle.customRigidBody = rb;
            }
        }
        
        private void FixedUpdate()
        {
            Angle = CurrentAngle();
            if (_joint == null)
                return;
            if (!coverHandles.Any(h => h.handlers.Count > 0))
            {
                if (!_locked)
                    Lock();
                return;
            }
            if (_locked)
            {
                Unlock();
            }
            
            var target = Mathf.Clamp(Util.NormalizeAngle(rb.transform.localEulerAngles.x), minAngle, maxAngle);
            axis.localEulerAngles = new Vector3(target, 0, 0);

            if (Quaternion.Angle(axis.localRotation, closedPosition.localRotation) <= Threshold)
            {
                if (state == BoltBase.BoltState.Locked)
                    return;
                state = BoltBase.BoltState.Locked;
                Util.PlayRandomAudioSource(closeSounds);
            }
            else if (Quaternion.Angle(axis.localRotation, openedPosition.localRotation) <= Threshold)
            {
                if (state == BoltBase.BoltState.Back)
                    return;
                state = BoltBase.BoltState.Back;
                Util.PlayRandomAudioSource(openSounds);
            }
            else
            {
                state = BoltBase.BoltState.Moving;
            }
        }

        private float CurrentAngle()
        {
            return Quaternion.Angle(axis.localRotation, closedPosition.localRotation);
        }

        private void Lock()
        {
            if (_locked)
                return;
            if (state == BoltBase.BoltState.Locked)
            {
                axis.localRotation = closedPosition.localRotation;
                rb.transform.localRotation = closedPosition.localRotation;
            }

            _locked = true;
            _joint.limits = new JointLimits { min = CurrentAngle() - 0.001f, max = CurrentAngle() };
        }

        private void Unlock()
        {
            if (!_locked)
                return;
            _locked = false;
            _joint.limits = new JointLimits { min = minAngle, max = maxAngle };
        }

        private void InitializeJoint()
        {
            _joint = magazineWell.firearm.gameObject.AddComponent<HingeJoint>();
            _joint.axis = Vector3.left;
            rb.transform.position = closedPosition.position;
            rb.transform.rotation = closedPosition.rotation;
            _joint.connectedBody = rb;
            _joint.massScale = 0.00001f;
            _joint.anchor = BoltBase.GrandparentLocalPosition(rb.transform, magazineWell.firearm.item.transform);
            _joint.useLimits = true;
            _joint.limits = new JointLimits { min = 0, max = 0 };
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = Vector3.zero;
        }

        public override bool GetIsUnlocked()
        {
            return state == BoltBase.BoltState.Back;
        }
    }
}
