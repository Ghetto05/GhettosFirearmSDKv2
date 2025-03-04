using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MagazineWellLatch : MonoBehaviour
{
    public MagazineWell magazineWell;
    public BeltFedCover beltFedCover;
    public Transform axis;
    public Transform magazineInsertedPosition;
    public Transform magazineRemovedPosition;

    private void Update()
    {
        var target = magazineWell.currentMagazine == null && (beltFedCover == null || beltFedCover.GetIsUnlocked()) ? magazineRemovedPosition : magazineInsertedPosition;
        axis.SetLocalPositionAndRotation(target.localPosition, target.localRotation);
    }
}