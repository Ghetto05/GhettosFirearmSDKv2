using UnityEngine;

namespace GhettosFirearmSDKv2;

public class RotationLimitedFollower : MonoBehaviour
{
    public Transform lever;
    public Transform hammer;
    public Transform cockedPosition;

    private void Update()
    {
        if (Util.NormalizeAngle(hammer.localEulerAngles.x) < Util.NormalizeAngle(cockedPosition.localEulerAngles.x))
            hammer.localEulerAngles = cockedPosition.localEulerAngles;

        if (Util.NormalizeAngle(hammer.localEulerAngles.x) > Util.NormalizeAngle(lever.localEulerAngles.x))
            hammer.localEulerAngles = lever.localEulerAngles;
    }
}