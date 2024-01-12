using ThunderRoad;
using UnityEngine;
using System;

namespace GhettosFirearmSDKv2
{
    public static class Extensions
    {
        #region Transform

        public static void AlignRotationWith(this Transform transform, Transform target, Transform child)
        {
            AlignRotationWith(transform, target, child, Vector3.zero);
        }
                
        public static void AlignRotationWith(this Transform transform, Transform target, Transform child, Vector3 ignoredAxes)
        {
            if (target == null || child == null || ignoredAxes == Vector3.one)
                return;

            Quaternion targetRotation = target.rotation;
            Quaternion quaternion = targetRotation * Quaternion.Inverse(child.rotation);
            transform.rotation = quaternion * transform.transform.rotation;
        }
        
        public static void AlignRotationWithB(this Transform transform, Transform target, Transform child, Vector3 ignoredAxes)
        {
            if (target == null || child == null || ignoredAxes == Vector3.one)
                return;

            Quaternion relativeRotation = target.rotation * Quaternion.Inverse(child.rotation);
            relativeRotation.eulerAngles = new Vector3(
                ignoredAxes.x == 1 ? transform.rotation.eulerAngles.x : relativeRotation.eulerAngles.x,
                ignoredAxes.y == 1 ? transform.rotation.eulerAngles.y : relativeRotation.eulerAngles.y,
                ignoredAxes.z == 1 ? transform.rotation.eulerAngles.z : relativeRotation.eulerAngles.z
            );
            transform.rotation = relativeRotation * transform.rotation;
        }

        #endregion
    }
}