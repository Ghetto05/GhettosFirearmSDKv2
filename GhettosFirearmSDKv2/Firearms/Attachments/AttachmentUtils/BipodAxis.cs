using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class BipodAxis : MonoBehaviour
{
    public Bipod bipod;
    public Transform axis;
    public List<Transform> positions;

    private void Start()
    {
        bipod.ApplyPositionEvent += ApplyPosition;
    }

    private void OnDestroy()
    {
        bipod.ApplyPositionEvent -= ApplyPosition;
    }

    public void ApplyPosition()
    {
        axis.localPosition = positions[bipod.index].localPosition;
        axis.localEulerAngles = positions[bipod.index].localEulerAngles;
        axis.localScale = positions[bipod.index].localScale;
    }
}