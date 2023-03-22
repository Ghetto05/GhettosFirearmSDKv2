using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Lock : MonoBehaviour
    {
        public virtual bool isUnlocked()
        {
            return false;
        }

        public void InvokeChange() => ChangedEvent?.Invoke();
        public delegate void OnLockChanged();
        public event OnLockChanged ChangedEvent;
    }
}
