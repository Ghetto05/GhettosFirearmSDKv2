using System;
using System.Linq;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public static class Extensions
{
    #region Transform

    public static void AlignRotationWith(this Transform transform, Transform target, Transform child)
    {
        AlignRotationWith(transform, target, child, Vector3.zero);
    }

    public static void AlignRotationWith(this Transform transform, Transform target, Transform child, Vector3 ignoredAxes)
    {
        if (!target || !child || ignoredAxes == Vector3.one)
        {
            return;
        }

        var targetRotation = target.rotation;
        var quaternion = targetRotation * Quaternion.Inverse(child.rotation);
        transform.rotation = quaternion * transform.transform.rotation;
    }

    public static void AlignRotationWithB(this Transform transform, Transform target, Transform child, Vector3 ignoredAxes)
    {
        if (!target || !child || ignoredAxes == Vector3.one)
        {
            return;
        }

        var relativeRotation = target.rotation * Quaternion.Inverse(child.rotation);
        relativeRotation.eulerAngles = new Vector3(
            Mathf.Approximately(ignoredAxes.x, 1) ? transform.rotation.eulerAngles.x : relativeRotation.eulerAngles.x,
            Mathf.Approximately(ignoredAxes.y, 1) ? transform.rotation.eulerAngles.y : relativeRotation.eulerAngles.y,
            Mathf.Approximately(ignoredAxes.z, 1) ? transform.rotation.eulerAngles.z : relativeRotation.eulerAngles.z
        );
        transform.rotation = relativeRotation * transform.rotation;
    }

    #endregion

    #region Enums

    public static T Next<T>(this T value) where T : Enum
    {
        var values = Enum.GetValues(value.GetType()).Cast<T>().ToList();
        var index = values.IndexOf(value);
        index++;
        if (index >= values.Count)
        {
            index = 0;
        }
        return values[index];
    }

    public static T Previous<T>(this T value) where T : Enum
    {
        var values = Enum.GetValues(value.GetType()).Cast<T>().ToList();
        var index = values.IndexOf(value);
        index--;
        if (index <= 0)
        {
            index = values.Count - 1;
        }
        return values[index];
    }

    public static string GetName<T>(this T value) where T : Enum
    {
        try
        {
            var enumType = typeof(T);
            var memberInfos =
                enumType.GetMember(value.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m =>
                m.DeclaringType == enumType);
            var valueAttributes =
                enumValueMemberInfo?.GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
            return ((EnumDescriptionAttribute)valueAttributes?[0])?.Name ?? value.ToString();
        }
        catch
        {
            return value.ToString();
        }
    }

    #endregion
}