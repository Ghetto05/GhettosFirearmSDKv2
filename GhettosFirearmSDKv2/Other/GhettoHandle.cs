using System;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class GhettoHandle : Handle
    {
        public enum HandleType
        {
            None,
            Dominant,
            NonDominant
        }

        public HandleType overrideType;

        protected override void Awake()
        {
            base.Awake();
            if (overrideType != HandleType.None) return;
            if (item && item.GetComponent<Firearm>() is Firearm firearm && firearm.bolt && firearm.bolt.GetNoInfluenceHandles().Contains(this)) overrideType = HandleType.Dominant;
            if (item && (item.mainHandleLeft == this || item.mainHandleRight == this)) overrideType = HandleType.NonDominant;
        }

        public override void RefreshJointDrive()
        {
            if (!IsHanded())  return;
            if (handlers.Count == 1)
            {
                if (item && item.IsTwoHanded())
                {
                    Vector2 vector2 = new Vector2(handlers.First().creature.data.forceRotationSpringDamper2HMult.x * data.rotationSpring2hMultiplier, handlers.First().creature.data.forceRotationSpringDamper2HMult.y * data.rotationDamper2hMultiplier);
                    
                    if (handlers.First().otherHand.grabbedHandle && handlers.First().otherHand.grabbedHandle.item && handlers.First().otherHand.grabbedHandle.item == item)
                    {
                        //decision is made here
                        if (data.dominantWhenTwoHanded && handlers.First().otherHand.grabbedHandle.data.dominantWhenTwoHanded) SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, handlers.First().side == dominantHand ? vector2 : Vector2.zero, data.rotationDrive);
                        else SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, data.dominantWhenTwoHanded || overrideType == HandleType.Dominant ? vector2 : Vector2.zero, data.rotationDrive);
                    }
                    else SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, data.dominantWhenTwoHanded ? vector2 : Vector2.zero, data.rotationDrive);
                }
                else SetJointConfig(handlers.First(), Vector2.one, Vector2.one, data.rotationDrive);
            }
            if (handlers.Count == 2)
            {
                RagdollHand ragdollHand1 = handlers.First();
                RagdollHand ragdollHand2 = handlers.Last();
                if (data.dominantWhenTwoHanded && twoHandedMode != TwoHandedMode.Position)
                {
                    if (Vector3.Dot(ragdollHand1.gripInfo.transform.up, ragdollHand2.gripInfo.transform.up) > 0.0)
                    {
                        if (Vector3.Dot(ragdollHand1.gripInfo.transform.up, transform.up) > 0.0)
                        {
                            switch (twoHandedMode)
                            {
                                case TwoHandedMode.AutoFront:
                                    if (GetNearestAxisPosition(ragdollHand1.gripInfo.transform.position) > GetNearestAxisPosition(ragdollHand2.gripInfo.transform.position))
                                    {
                                        SetJointToTwoHanded(ragdollHand1.side);
                                        break;
                                    }
                                    SetJointToTwoHanded(ragdollHand2.side);
                                    break;
                                case TwoHandedMode.AutoRear:
                                    if (GetNearestAxisPosition(ragdollHand1.gripInfo.transform.position) > GetNearestAxisPosition(ragdollHand2.gripInfo.transform.position))
                                    {
                                        SetJointToTwoHanded(ragdollHand2.side);
                                        break;
                                    }
                                    SetJointToTwoHanded(ragdollHand1.side);
                                    break;
                                default:
                                    SetJointToTwoHanded(dominantHand);
                                    break;
                            }
                        }
                        else
                        {
                            switch (twoHandedMode)
                            {
                                case TwoHandedMode.AutoFront:
                                    if (GetNearestAxisPosition(ragdollHand1.gripInfo.transform.position) < (double)GetNearestAxisPosition(ragdollHand2.gripInfo.transform.position))
                                    {
                                        SetJointToTwoHanded(ragdollHand1.side);
                                        break;
                                    }
                                    SetJointToTwoHanded(ragdollHand2.side);
                                    break;
                                case TwoHandedMode.AutoRear:
                                    if (GetNearestAxisPosition(ragdollHand1.gripInfo.transform.position) < (double)GetNearestAxisPosition(ragdollHand2.gripInfo.transform.position))
                                    {
                                        SetJointToTwoHanded(ragdollHand2.side);
                                        break;
                                    }
                                    SetJointToTwoHanded(ragdollHand1.side);
                                    break;
                                default:
                                    SetJointToTwoHanded(dominantHand);
                                    break;
                            }
                        }
                    }
                    else
                        SetJointToTwoHanded(dominantHand, 0.1f);
                }
                else
                    SetJointToTwoHanded(dominantHand, 0.1f);
            }
        }
    }
}