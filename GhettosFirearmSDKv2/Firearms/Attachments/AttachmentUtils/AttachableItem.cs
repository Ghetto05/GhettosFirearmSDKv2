using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class AttachableItem : MonoBehaviour
    {
        public Item item;
        public string attachmentType;
        public List<Collider> attachColliders;
        public string attachmentId;
        public List<AudioSource> attachSounds;
    }
}
