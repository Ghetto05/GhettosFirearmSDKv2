using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MortarSight : MonoBehaviour
{
    public Transform reference;
    public Transform axis;
    public Transform minimum;
    public Transform maximum;

    private void Update()
    {
        var angle = GetAngleAroundX(reference);
        var min = Util.NormalizeAngle(minimum.localEulerAngles.x);
        var max = Util.NormalizeAngle(maximum.localEulerAngles.x);
        angle = Mathf.Clamp(-angle, min, max);
        var euler = new Vector3(angle, 0, 0);
        axis.localEulerAngles = euler;
    }

    private static float GetAngleAroundX(Transform t)
    {
        var projTransformUp = new Vector3(0, t.up.y, t.up.z).normalized;
        var projWorldUp = new Vector3(0, Vector3.up.y, Vector3.up.z).normalized;
        var angle = Vector3.Angle(projWorldUp, projTransformUp);
        var cross = Vector3.Cross(projWorldUp, projTransformUp);
        if (cross.x < 0) angle = -angle;
        return angle;
    }
}