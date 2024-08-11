using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UIElements;

namespace GhettosFirearmSDKv2
{
    public class Handcuffs : MonoBehaviour
    {
        public static List<RagdollPart> allAttachedParts = new List<RagdollPart>();
        
        public Item item;
        public bool canBeReopened;
        public bool destroyOnReopen;
        public Transform leftAnchor;
        public Transform rightAnchor;
        public Transform leftFootAnchor;
        public Transform rightFootAnchor;

        public Transform leftAxis;
        public Transform leftOpenedPosition;
        public Transform leftClosedPosition;
        public Transform rightAxis;
        public Transform rightOpenedPosition;
        public Transform rightClosedPosition;

        public GameObject closedLeftObject;
        public GameObject openedLeftObject;
        public GameObject closedRightObject;
        public GameObject openedRightObject;

        public AudioSource[] closeSounds;
        public AudioSource[] openSounds;

        public Collider leftTrigger;
        public Collider rightTrigger;

        public Collider[] leftColliders;
        public Collider[] rightColliders;

        private Joint _leftJoint;
        private Joint _rightJoint;
        private RagdollPart _leftConnectedPart;
        private RagdollPart _rightConnectedPart;
        private float _lastUnlockTime;
        [Space] [Space] [Space] public string _;

        private void Start()
        {
            item.OnHeldActionEvent += OnHeldAction;
            item.OnDespawnEvent += delegate(EventTime time)
            { 
                if (time == EventTime.OnEnd)
                    Unlock(true, true);
                
            };
            var relay = item.gameObject.AddComponent<CollisionRelay>();
            relay.onCollisionEnterEvent += OnCollisionEnterEvent;
            UnlockAnimation(true);
        }

        private void OnCollisionEnterEvent(Collision collision)
        {
            Side side;
            if (collision.contacts[0].thisCollider == leftTrigger)
                side = Side.Left;
            else if (collision.contacts[0].thisCollider == rightTrigger)
                side = Side.Right;
            else
                return;

            if (AllowLock(collision, side, out var part))
                LockTo(part, side);
        }

        private void OnHeldAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart)
            {
                Unlock(false);
            }
        }

        private bool AllowLock(Collision collision, Side side, out RagdollPart foundPart)
        {
            var part = PartByCollider(collision.contacts[0].otherCollider);
            foundPart = part;
            if (part == null)
                return false;

            if (part.ragdoll.creature.isPlayer)
                return false;
            
            var correctType = part.type == RagdollPart.Type.LeftHand
                              || part.type == RagdollPart.Type.RightHand
                              || part.type == RagdollPart.Type.LeftFoot
                              || part.type == RagdollPart.Type.RightFoot;
            
            var noOtherPartAttached = side == Side.Left ? _leftConnectedPart == null : _rightConnectedPart == null;

            var notAlreadyAttached = !allAttachedParts.Contains(part);
            
            return correctType && noOtherPartAttached && notAlreadyAttached /*&& TypeMatches(part, side)*/;
        }

        private bool TypeMatches(RagdollPart part, Side side)
        {
            if (_leftConnectedPart == null && _rightConnectedPart == null)
                return true;

            var presentPart = side == Side.Right ? _leftConnectedPart : _rightConnectedPart;

            if (presentPart == null)
                return true;
            
            return part.type == presentPart.type;
        }

        public void LockTo(RagdollPart part, Side side)
        {
            if (Time.time - _lastUnlockTime < 2f)
                return;

            item.DisallowDespawn = true;
            var anchor = part.type == RagdollPart.Type.LeftFoot || part.type == RagdollPart.Type.RightFoot ? (side == Side.Left ? leftFootAnchor : rightFootAnchor) : (side == Side.Left ? leftAnchor : rightAnchor);
            allAttachedParts.Add(part);

            var joint = item.gameObject.AddComponent<HingeJoint>();
            //item.transform.MoveAlign(anchor, part.physicBody.rigidBody.transform);
            //part.physicBody.rigidBody.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            item.transform.AlignRotationWith(part.physicBody.rigidBody.transform, anchor, joint.axis);
            joint.axis = Vector3.up;
            joint.anchor = anchor.localPosition;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
            joint.connectedBody = part.physicBody.rigidBody;
            
            if (side == Side.Left)
            {
                _leftConnectedPart = part;
                _leftJoint = joint;
                leftTrigger.enabled = false;
                foreach (var coll in leftColliders)
                {
                    coll.enabled = false;
                }
            }
            else
            {
                _rightConnectedPart = part;
                _rightJoint = joint;
                rightTrigger.enabled = false;
                foreach (var coll in rightColliders)
                {
                    coll.enabled = false;
                }
            }
            var c = part.ragdoll.creature;
            
            ToggleCreaturePhysics(true);
            if (_leftConnectedPart != null && _rightConnectedPart != null)
            {
                if (part.type == RagdollPart.Type.LeftFoot || part.type == RagdollPart.Type.RightFoot)
                {
                    part.ragdoll.creature.brain.AddNoStandUpModifier(item);
                    if (c.isKilled)
                        c.ragdoll.SetState(Ragdoll.State.Inert);
                    else
                        c.ragdoll.SetState(Ragdoll.State.Destabilized);
                }
                else
                {
                    var brainData = c.brain.instance;
                    brainData.StopModuleUsingAnyBodyPart();
                    brainData.tree.Reset();
                    brainData.updateTree = false;
                }
            }

            LockAnimation(side);
        }

        private IEnumerator PreventDamage(Creature creature)
        {
            creature.OnDamageEvent += CreatureOnOnDamageEvent;
            yield return new WaitForSeconds(2f);
            creature.OnDamageEvent -= CreatureOnOnDamageEvent;
        }

        private void CreatureOnOnDamageEvent(CollisionInstance collisioninstance, EventTime eventtime)
        {
            if (eventtime == EventTime.OnStart)
                collisioninstance.ignoreDamage = true;
        }

        [EasyButtons.Button]
        public void Unlock(bool withTool, bool onDespawn = false)
        {
            if (canBeReopened || withTool)
            {
                _lastUnlockTime = Time.time;
                if (_leftConnectedPart != null && _rightConnectedPart != null)
                {
                    _rightConnectedPart.ragdoll.creature.brain.RemoveNoStandUpModifier(item);
                    _rightConnectedPart.ragdoll.creature.brain.instance.updateTree = true;
                }
                ToggleCreaturePhysics(false);

                Destroy(_leftJoint);
                Destroy(_rightJoint);
                if (_leftConnectedPart != null)
                    allAttachedParts.Remove(_leftConnectedPart);
                if (_rightConnectedPart != null)
                    allAttachedParts.Remove(_rightConnectedPart);
                _leftConnectedPart = null;
                _rightConnectedPart = null;
                
                leftTrigger.enabled = true;
                foreach (var c in leftColliders)
                {
                    c.enabled = true;
                }
                
                rightTrigger.enabled = true;
                foreach (var c in rightColliders)
                {
                    c.enabled = true;
                }
                
                UnlockAnimation();
                item.DisallowDespawn = false;
                if (destroyOnReopen && !onDespawn)
                    item.Despawn();
            }
        }

        [EasyButtons.Button]
        public void LockAnimation(Side side)
        {
            Util.PlayRandomAudioSource(closeSounds);

            var axis = side == Side.Left ? leftAxis : rightAxis;
            var target = side == Side.Left ? leftClosedPosition : rightClosedPosition;
            if (axis != null)
                axis.SetPositionAndRotation(target.position, target.rotation);
            
            if (closedLeftObject != null && side == Side.Left)
                closedLeftObject.SetActive(true);
            if (openedLeftObject != null && side == Side.Left)
                openedLeftObject.SetActive(false);
            if (closedRightObject != null && side == Side.Right)
                closedRightObject.SetActive(true);
            if (openedRightObject != null && side == Side.Right)
                openedRightObject.SetActive(false);
        }

        [EasyButtons.Button]
        public void UnlockAnimation(bool silent = false)
        {
            if (!silent) 
                Util.PlayRandomAudioSource(openSounds);

            if (leftAxis != null)
            {
                leftAxis.SetPositionAndRotation(leftOpenedPosition.position, leftOpenedPosition.rotation);
                rightAxis.SetPositionAndRotation(rightOpenedPosition.position, rightOpenedPosition.rotation);
            }

            if (closedLeftObject != null)
                closedLeftObject.SetActive(false);
            if (openedLeftObject != null)
                openedLeftObject.SetActive(true);
            if (closedRightObject != null)
                closedRightObject.SetActive(false);
            if (openedRightObject != null)
                openedRightObject.SetActive(true);
        }

        private void ToggleCreaturePhysics(bool forcedOn)
        {
            var c = _leftConnectedPart != null ? _leftConnectedPart.ragdoll.creature :
                _rightConnectedPart != null ? _rightConnectedPart.ragdoll.creature : null;

            if (c != null)
                c.ragdoll.physicToggle = !forcedOn;
        }

        private RagdollPart PartByCollider(Collider collider)
        {
            var ragdoll = collider.GetComponentInParent<Ragdoll>();
            if (ragdoll == null)
                return null;

            foreach (var part in ragdoll.parts)
            {
                if (part.colliderGroup.colliders.Contains(collider))
                    return part;
            }

            return null;
        }
    }
}
