namespace GhettosFirearmSDKv2;

public class BoltBasedLock : Lock
{
    public enum Filters
    {
        LockAlwaysExcept,
        LockOnlyWhen
    }

    public Filters filter;
    public BoltBase.BoltState requiredState;
    public BoltBase bolt;

    public override bool GetIsUnlocked()
    {
        if (filter == Filters.LockAlwaysExcept)
        {
            if (bolt.state == requiredState) return true;

            return false;
        }

        if (bolt.state != requiredState) return true;

        return false;
    }
}