using UnityEngine;

namespace GhettosFirearmSDKv2;

public interface ICaliberGettable
{
    string GetCaliber();

    Transform GetTransform();
}