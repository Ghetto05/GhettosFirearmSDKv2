using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class EffectSpawner : MonoBehaviour
    {
        public string effectId;
        public bool parentToThis;

        public void Awake()
        {
            EffectInstance ei = Catalog.GetData<EffectData>(effectId).Spawn(parentToThis ? this.transform : null);
            ei.Play();
        }
    }
}
