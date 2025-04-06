using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class HolsterLid : MonoBehaviour
{
    public Holder holder;
    public Transform axis;
    public Transform closedPosition;
    public Transform openedPosition;

    private void Start()
    {
        holder.Snapped += HolderOnSnapped;
        holder.UnSnapped += HolderOnUnSnapped;
        if (holder.items.Count == 0)
        {
            HolderOnUnSnapped(null);
        }
    }

    private void HolderOnUnSnapped(Item item)
    {
        axis.SetLocalPositionAndRotation(openedPosition.localPosition, openedPosition.localRotation);
    }

    private void HolderOnSnapped(Item item)
    {
        axis.SetLocalPositionAndRotation(closedPosition.localPosition, closedPosition.localRotation);
    }
}