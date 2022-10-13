using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
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

        public override bool isUnlocked()
        {
            if (filter == Filters.LockAlwaysExcept)
            {
                if (bolt.state == requiredState) return true;
                else return false;
            }
            else
            {
                if (bolt.state != requiredState) return true;
                else return false;
            }
        }
    }
}
