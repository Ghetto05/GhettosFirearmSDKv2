using UnityEngine;

namespace GhettosFirearmSDKv2;

public class BeltFedCoverAxis : MonoBehaviour
{
    public BeltFedCover beltFed;
    public bool foldOnFullyOpened;
    public Transform axis;
    public Transform idlePosition;
    public Transform foldedPosition;

    private void FixedUpdate()
    {
        if ((!foldOnFullyOpened && beltFed.state != BoltBase.BoltState.Locked) || beltFed.state == BoltBase.BoltState.Back)
            axis.SetLocalPositionAndRotation(foldedPosition.localPosition, foldedPosition.localRotation);
        else
            axis.SetLocalPositionAndRotation(idlePosition.localPosition, idlePosition.localRotation);
    }
}