using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GhettosFirearmSDKv2.Explosives
{
    public class ExplosiveData : MonoBehaviour
    {
        public float damage;
        public float radius;
        public float force;
        public string effectId;
        public float upwardsModifier;
        public int shrapnelCount;
        public float shrapnelDamage;
        public float shrapnelRange;
    }
}
