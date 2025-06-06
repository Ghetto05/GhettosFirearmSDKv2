﻿using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class GhettoHandle : Handle
{
    public enum HandleType
    {
        Foregrip,
        MainGrip,
        Bolt,
        PumpAction
    }

    public enum HandlePriority
    {
        NoAI = 0,
        Foregrip = 10,
        AttachForegrip = 20
    }

    public enum Tags
    {
        None = 0,
        SlidingStockHandle = 1 << 0
    }

    public HandleType type;
    public HandlePriority aiPriority;
    public Tags tags;

    protected override void Awake()
    {
        if (axisLength > 0)
        {
            slideBehavior = SlideBehavior.CanSlide;
        }

        base.Awake();
    }

    public override void RefreshJointDrive()
    {
        if (!IsHanded())
        {
            return;
        }

        if (handlers.Count == 1)
        {
            if (item && item.IsTwoHanded())
            {
                var dominantRotation = new Vector2(handlers.First().creature.data.forceRotationSpringDamper2HMult.x * data.rotationSpring2hMultiplier, handlers.First().creature.data.forceRotationSpringDamper2HMult.y * data.rotationDamper2hMultiplier);

                //case: item two handed by one creature
                if (handlers.First().otherHand.grabbedHandle && handlers.First().otherHand.grabbedHandle.item && handlers.First().otherHand.grabbedHandle.item == item)
                {
                    if (IsOtherHandleGhetto(handlers.First().otherHand.grabbedHandle, out var otherHandle))
                    {
                        if (type == HandleType.MainGrip)
                        {
                            SetThisDominant();
                        }
                        else if (otherHandle.type == HandleType.MainGrip)
                        {
                            SetOtherDominant();
                        }

                        else if (type == HandleType.Foregrip && otherHandle.type == HandleType.Foregrip)
                        {
                            if (handlers.First().side == dominantHand)
                            {
                                SetThisDominant();
                            }
                            else
                            {
                                SetOtherDominant();
                            }
                        }

                        else if (type == HandleType.Foregrip && otherHandle.type == HandleType.PumpAction)
                        {
                            SetThisDominant();
                        }
                        else if (type == HandleType.PumpAction && otherHandle.type == HandleType.Foregrip)
                        {
                            SetOtherDominant();
                        }

                        else if (type == HandleType.Bolt)
                        {
                            SetOtherDominant();
                        }
                        else if (otherHandle.type == HandleType.Bolt)
                        {
                            SetThisDominant();
                        }
                    }
                    else
                    {
                        if (data.dominantWhenTwoHanded && handlers.First().otherHand.grabbedHandle.data.dominantWhenTwoHanded)
                        {
                            SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, handlers.First().side == dominantHand ? dominantRotation : Vector2.zero, data.rotationDrive);
                        }
                        else
                        {
                            SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, data.dominantWhenTwoHanded ? dominantRotation : Vector2.zero, data.rotationDrive);
                        }
                    }
                }

                //case: item two handed by different creatures
                else
                {
                    SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, data.dominantWhenTwoHanded ? dominantRotation : Vector2.zero, data.rotationDrive);
                }
            }

            //case: item onehanded
            else
            {
                SetJointConfig(handlers.First(), Vector2.one, Vector2.one, data.rotationDrive);
            }
        }

        //original
        if (handlers.Count == 2)
        {
            var ragdollHand1 = handlers.First();
            var ragdollHand2 = handlers.Last();
            if (data.dominantWhenTwoHanded && twoHandedMode != TwoHandedMode.Position)
            {
                if (Vector3.Dot(ragdollHand1.gripInfo.transform.up, ragdollHand2.gripInfo.transform.up) > 0.0)
                {
                    if (Vector3.Dot(ragdollHand1.gripInfo.transform.up, transform.up) > 0.0)
                    {
                        switch (twoHandedMode)
                        {
                            case TwoHandedMode.AutoFront:
                                if (GetNearestAxisPosition(ragdollHand1.gripInfo.transform.position) > (double)GetNearestAxisPosition(ragdollHand2.gripInfo.transform.position))
                                {
                                    SetJointToTwoHanded(ragdollHand1.side);
                                    break;
                                }
                                SetJointToTwoHanded(ragdollHand2.side);
                                break;

                            case TwoHandedMode.AutoRear:
                                if (GetNearestAxisPosition(ragdollHand1.gripInfo.transform.position) > (double)GetNearestAxisPosition(ragdollHand2.gripInfo.transform.position))
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
                {
                    SetJointToTwoHanded(dominantHand, 0.1f);
                }
            }
            else
            {
                SetJointToTwoHanded(dominantHand, 0.1f);
            }
        }
    }

    public void SetThisDominant()
    {
        var dominantRotation = new Vector2(handlers.First().creature.data.forceRotationSpringDamper2HMult.x * data.rotationSpring2hMultiplier, handlers.First().creature.data.forceRotationSpringDamper2HMult.y * data.rotationDamper2hMultiplier);
        if (type != HandleType.PumpAction)
        {
            SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, dominantRotation, data.rotationDrive);
        }
        else
        {
            SetJointConfigV2(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, dominantRotation, data.rotationDrive);
        }
    }

    public void SetOtherDominant()
    {
        if (type != HandleType.PumpAction)
        {
            SetJointConfig(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, Vector2.zero, data.rotationDrive);
        }
        else
        {
            SetJointConfigV2(handlers.First(), handlers.First().creature.data.forcePositionSpringDamper2HMult, Vector2.zero, data.rotationDrive);
        }
    }

    public bool IsOtherHandleGhetto(Handle handle, out GhettoHandle ghettoHandle)
    {
        ghettoHandle = null;
        if (handle.GetType() == typeof(GhettoHandle))
        {
            ghettoHandle = (GhettoHandle)handle;
            return true;
        }

        return false;
    }

    public virtual void SetJointConfigV2(
        RagdollHand handler,
        Vector2 positionMultiplier,
        Vector2 rotationMultiplier,
        HandleData.RotationDrive rotationDrive)
    {
        if (!handler.creature.player)
        {
            return;
        }
        if (handler.gripInfo.joint)
        {
            SetJointConfig(handler.gripInfo.joint, handler, positionMultiplier, rotationMultiplier, handler.creature.data.forceMaxPosition, handler.creature.data.forceMaxRotation, rotationDrive);
        }
        if (handler.gripInfo.playerJoint)
            //Destroy(handler.gripInfo.playerJoint);
        {
            SetJointConfig(handler.gripInfo.playerJoint, handler, Vector2.zero, Vector2.zero, 0.0f, 0.0f, rotationDrive);
        }
        playerJointActive = false;
    }
}