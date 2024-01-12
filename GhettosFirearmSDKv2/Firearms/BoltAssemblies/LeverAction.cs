using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class LeverAction : MonoBehaviour
    {
        private const float Threshold = 3f;

        public BoltSemiautomatic bolt;
        
        public Rigidbody rb;
        public Transform lever;
        public Transform start;
        public Transform end;

        public float minAngle;
        public float maxAngle;
        private float _targetAngle;

        public AudioSource[] openSounds;
        public AudioSource[] closeSounds;

        public Handle grip;
        public Handle leverHandle;

        private HingeJoint _joint;

        private BoltBase.BoltState _state;
        private bool _reachedEnd;

        public void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
            leverHandle.SetTouch(false);
        }
        
        private void InvokedStart()
        {
            _targetAngle = Quaternion.Angle(start.localRotation, end.localRotation);
            InitializeJoint();
            Lock(true);
            bolt.overrideHeldState = true;
            bolt.firearm.OnAltActionEvent += FirearmOnAltActionEvent;
            bolt.OnFireLogicFinishedEvent += BoltOnFireLogicFinishedEvent;
        }

        private void BoltOnFireLogicFinishedEvent()
        {
            if (_state != BoltBase.BoltState.Locked)
                return;
            //Unlock();
            Invoke(nameof(Unlock), 0.05f);
        }

        private void FirearmOnAltActionEvent(bool longPress)
        {
            if (longPress || _state != BoltBase.BoltState.Locked)
                return;
            Unlock();
        }

        private void FixedUpdate()
        {
            if (_state == BoltBase.BoltState.Locked)
            {
                bolt.bolt.localPosition = bolt.startPoint.localPosition;
                return;
            }

            float target = Mathf.Clamp(Util.NormalizeAngle(rb.transform.localEulerAngles.x), minAngle, maxAngle);
            lever.localEulerAngles = new Vector3(target, 0, 0);

            bolt.rigidBody.transform.localPosition = Vector3.Lerp(bolt.startPoint.localPosition, bolt.endPoint.localPosition, Time());
            bolt.bolt.transform.localPosition = Vector3.Lerp(bolt.startPoint.localPosition, bolt.endPoint.localPosition, Time());

            if (Quaternion.Angle(lever.localRotation, start.localRotation) <= Threshold && _state == BoltBase.BoltState.Moving && _reachedEnd)
            {
                Lock();
                Util.PlayRandomAudioSource(closeSounds);
            }

            if (Quaternion.Angle(lever.localRotation, end.localRotation) <= Threshold && !_reachedEnd)
            {
                _reachedEnd = true;
                Util.PlayRandomAudioSource(openSounds);
            }
        }

        private float Time()
        {
            float currentAngle = Quaternion.Angle(start.localRotation, lever.localRotation);
            return Mathf.Clamp01(currentAngle / _targetAngle);
        }

        [EasyButtons.Button]
        public void Lock(bool forced = false)
        {
            if (_state == BoltBase.BoltState.Locked && !forced)
                return;
            _state = BoltBase.BoltState.Locked;
            lever.localRotation = start.localRotation;
            rb.transform.localRotation = start.localRotation;
            _joint.limits = new JointLimits { max = 0, min = 0 };
            bolt.heldState = false;
            bolt.rigidBody.transform.position = bolt.startPoint.position;
            bolt.bolt.position = bolt.startPoint.position;
            bolt.laststate = bolt.state;
            bolt.state = BoltBase.BoltState.Locked;

            leverHandle.SetTouch(false);
            grip.SetTouch(true);
            List<RagdollHand> handlers = leverHandle.handlers.ToList();
            leverHandle.Release();
            foreach (RagdollHand ragdollHand in handlers)
            {
                ragdollHand.Grab(grip);
            }
        }

        [EasyButtons.Button]
        public void Unlock()
        {
            if (_state != BoltBase.BoltState.Locked)
                return;
            _joint.limits = new JointLimits { max = maxAngle, min = minAngle };
            _reachedEnd = false;
            _state = BoltBase.BoltState.Moving;
            bolt.heldState = true;

            leverHandle.SetTouch(true);
            grip.SetTouch(false);
            List<RagdollHand> handlers = grip.handlers.ToList();
            grip.Release();
            foreach (RagdollHand ragdollHand in handlers)
            {
                ragdollHand.Grab(leverHandle);
            }
            if (bolt.firearm.triggerState)
                bolt.firearm.ChangeTrigger(false);
        }

        private void InitializeJoint()
        {
            _joint = bolt.firearm.gameObject.AddComponent<HingeJoint>();
            _joint.axis = Vector3.left;
            rb.transform.position = start.position;
            rb.transform.rotation = start.rotation;
            _joint.connectedBody = rb;
            _joint.massScale = 0.00001f;
            _joint.anchor = BoltBase.GrandparentLocalPosition(rb.transform, bolt.firearm.item.transform);
            _joint.useLimits = true;
            _joint.limits = new JointLimits { min = 0, max = 0 };
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = Vector3.zero;
        }
    }
}
