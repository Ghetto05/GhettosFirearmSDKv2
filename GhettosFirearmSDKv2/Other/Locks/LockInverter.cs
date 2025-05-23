using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Locks/Lock inverter")]
public class LockInverter : Lock
{
    public Lock lockToBeInverted;

    public override bool GetIsUnlocked()
    {
        if (!lockToBeInverted)
        {
            return true;
        }
        return !lockToBeInverted.IsUnlocked();
    }
}