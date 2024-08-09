using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Lock : MonoBehaviour
    {
        public bool inverted = false;

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
}
