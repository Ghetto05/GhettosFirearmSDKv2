using UnityEngine;

namespace GhettosFirearmSDKv2;

public class Lock : MonoBehaviour
{
    public bool inverted;

    public bool IsUnlocked()
    {
        return !inverted? GetIsUnlocked() : !GetIsUnlocked();
    }

    public virtual bool GetIsUnlocked()
    {
        return false;
    }

    public void InvokeChange() => ChangedEvent?.Invoke();
    public delegate void OnLockChanged();
    public event OnLockChanged ChangedEvent;
}