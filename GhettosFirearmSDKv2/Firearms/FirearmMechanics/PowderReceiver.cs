using UnityEngine;

namespace GhettosFirearmSDKv2;

public class PowderReceiver : MonoBehaviour
{
    public int grainCapacity;
    public int currentAmount;
    public int minimum;
    public bool blocked;
    public Collider loadCollider;
    public Transform fillRoot;
    public Transform emptyPosition;
    public Transform filledPosition;
    public Transform fillPosition;

    public bool Sufficient()
    {
        return currentAmount >= minimum;
    }

    private void FixedUpdate()
    {
        UpdatePositions();
    }

    public void UpdatePositions()
    {
        if (fillRoot != null)
            fillRoot.localScale = new Vector3(1, 1, currentAmount / (float)minimum);
        if (fillPosition != null)
            fillPosition.position = Vector3.LerpUnclamped(emptyPosition.position, filledPosition.position, currentAmount / (float)minimum);
    }
}