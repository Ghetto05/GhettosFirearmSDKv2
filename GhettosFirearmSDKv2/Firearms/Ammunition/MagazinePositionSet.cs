using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MagazinePositionSet : MonoBehaviour
{
    public string caliber;
    public int capacity;
    public Transform[] positions;
    public Transform[] oddCountPositions;
    public List<GameObject> feeders;
}