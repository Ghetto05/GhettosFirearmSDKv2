using UnityEngine;

namespace GhettosFirearmSDKv2;

public class SlidingStockLatch : MonoBehaviour
{
    public SlidingStock stock;
    public Transform axis;
    public Transform lockedPosition;
    public Transform unlockedPosition;

    private void Start()
    {
        if (!stock)
        {
            stock = GetComponentInParent<SlidingStock>();
        }
    }

    private void Update()
    {
        if (!stock)
        {
            return;
        }

        var pos = stock.Unlocked ? unlockedPosition : lockedPosition;
        axis.SetLocalPositionAndRotation(pos.localPosition, pos.localRotation);
    }
}