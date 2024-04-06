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
        public Transform leverColliders;
        public Transform start;
        public Transform end;

        public float minAngle;
        public float maxAngle;
        private float targetAngle;

        public AudioSource[] openSounds;
        public AudioSource[] closeSounds;

        public Handle grip;
        public Handle leverHandle;

        private HingeJoint joint;
        private RagdollHand handJointHand;
        private HingeJoint handJoint;

        private BoltBase.BoltState state;
        private bool reachedEnd;

        public void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
            leverHandle.SetTouch(false);
        }
        
        private void InvokedStart()
        {
            targetAngle = Quaternion.Angle(start.localRotation, end.localRotation);
            Lock(true);
            bolt.overrideHeldState = true;
            bolt.firearm.OnAltActionEvent += OnAltAction;
            bolt.OnFireLogicFinishedEvent += OnFireLogicFinished;
            bolt.firearm.item.OnGrabEvent += OnGrab;
            bolt.firearm.item.OnUngrabEvent += OnUnGrab;
        }

        private void OnUnGrab(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            
        }

        private void OnGrab(Handle handle, RagdollHand ragdollHand)
        {
            if (handle == leverHandle)
            {
                //InitializeHandJoint(ragdollHand);
            }
        }

        private void OnFireLogicFinished()
        {
            if (state != BoltBase.BoltState.Locked)
                return;
            //Unlock();
            Invoke(nameof(Unlock), 0.05f);
        }

        private void OnAltAction(bool longPress)
        {
            if (longPress || state != BoltBase.BoltState.Locked)
                return;
            Unlock();
        }

        private void FixedUpdate()
        {
            leverColliders.parent = state == BoltBase.BoltState.Locked ? lever : rb.transform;
            leverColliders.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (state != BoltBase.BoltState.Locked && LimitLeverChanged(out bool limit))
            {
                joint.limits = new JointLimits { max = limit ? lever.localEulerAngles.x : maxAngle, min = limit ? lever.localEulerAngles.x : minAngle };
            }

            if (state == BoltBase.BoltState.Locked)
            {
                bolt.bolt.localPosition = bolt.startPoint.localPosition;
                return;
            }

            float target = Mathf.Clamp(Util.NormalizeAngle(rb.transform.localEulerAngles.x), minAngle, maxAngle);
            lever.localEulerAngles = new Vector3(target, 0, 0);

            bolt.rigidBody.transform.localPosition = Vector3.Lerp(bolt.startPoint.localPosition, bolt.endPoint.localPosition, Time());
            bolt.bolt.transform.localPosition = Vector3.Lerp(bolt.startPoint.localPosition, bolt.endPoint.localPosition, Time());

            if (Quaternion.Angle(lever.localRotation, start.localRotation) <= Threshold && state == BoltBase.BoltState.Moving && reachedEnd)
            {
                Lock();
                Util.PlayRandomAudioSource(closeSounds);
            }

            if (Quaternion.Angle(lever.localRotation, end.localRotation) <= Threshold && !reachedEnd)
            {
                reachedEnd = true;
                Util.PlayRandomAudioSource(openSounds);
            }
        }

        private bool LimitLeverChanged(out bool limit)
        {
            bool held = leverHandle.handlers.Count > 0;
            bool limited = Mathf.Abs(Mathf.Abs(joint.limits.min) - Mathf.Abs(joint.limits.max)) < 0.001f;
            bool notLimited = MathF.Abs(MathF.Abs(joint.limits.max) - maxAngle) < 0.001f && MathF.Abs(MathF.Abs(joint.limits.min) - minAngle) < 0.001f;

            limit = !held;
            return (held && limited) || (!held && notLimited);
        }

        private float Time()
        {
            float currentAngle = Quaternion.Angle(start.localRotation, lever.localRotation);
            return Mathf.Clamp01(currentAngle / targetAngle);
        }

        [EasyButtons.Button]
        public void Lock(bool forced = false)
        {
            if (state == BoltBase.BoltState.Locked && !forced)
                return;
            if (joint != null)
                Destroy(joint);
            rb.isKinematic = true;
            state = BoltBase.BoltState.Locked;
            lever.localRotation = start.localRotation;
            rb.transform.localRotation = start.localRotation;
            //joint.limits = new JointLimits { max = 0, min = 0 };
            bolt.heldState = false;
            bolt.rigidBody.transform.position = bolt.startPoint.position;
            bolt.bolt.position = bolt.startPoint.position;
            bolt.laststate = bolt.state;
            bolt.state = BoltBase.BoltState.Locked;

            //DestroyHandleJoint();
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
            if (state != BoltBase.BoltState.Locked)
                return;

            rb.isKinematic = false;
            InitializeJoint();
            joint.limits = new JointLimits { max = maxAngle, min = minAngle };
            reachedEnd = false;
            state = BoltBase.BoltState.Moving;
            bolt.heldState = true;

            leverHandle.SetTouch(true);
            grip.SetTouch(false);
            List<RagdollHand> handlers = grip.handlers.ToList();
            grip.Release();
            foreach (RagdollHand ragdollHand in handlers)
            {
                ragdollHand.Grab(leverHandle);
                //InitializeHandJoint(ragdollHand);
            }
            if (bolt.firearm.triggerState)
                bolt.firearm.ChangeTrigger(false);
        }

        private void InitializeJoint()
        {
            if (joint == null)
                joint = bolt.firearm.gameObject.AddComponent<HingeJoint>();
            joint.axis = Vector3.left;
            rb.transform.position = start.position;
            rb.transform.rotation = start.rotation;
            joint.connectedBody = rb;
            //joint.massScale = 0.00001f;
            joint.anchor = BoltBase.GrandparentLocalPosition(rb.transform, bolt.firearm.item.transform);
            joint.useLimits = true;
            joint.limits = new JointLimits { max = maxAngle, min = minAngle }; //{ min = 0, max = 0 };
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
        }

        private void InitializeHandJoint(RagdollHand hand)
        {
            if (handJoint != null)
                Destroy(handJoint);
            handJointHand = hand;
            handJoint = bolt.firearm.item.gameObject.AddComponent<HingeJoint>();
            handJoint.axis = Vector3.left;
            handJoint.massScale = 1f;
            handJoint.anchor = BoltBase.GrandparentLocalPosition(rb.transform, bolt.firearm.item.transform);
            handJoint.useLimits = false;
            handJoint.autoConfigureConnectedAnchor = true;
            // handJoint.autoConfigureConnectedAnchor = false;
            // handJoint.connectedAnchor = BoltBase.GrandparentLocalPosition(rb.transform, hand.physicBody.rigidBody.transform);
            handJoint.connectedBody = hand.physicBody.rigidBody;
        }

        private void DestroyHandleJoint()
        {
            if (handJoint == null)
                return;

            Destroy(handJoint);
            handJointHand = null;
        }
    }
}
