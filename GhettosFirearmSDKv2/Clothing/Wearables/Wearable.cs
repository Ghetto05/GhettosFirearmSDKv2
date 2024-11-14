using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Clothing.Wearables;

public class Wearable : MonoBehaviour
{
    [Flags]
    public enum Channels
    {
        None = 0,
        Head = 1 << 0,
        Mouth = 1 << 1,
        Eyes = 1 << 2,
        Ears = 2 << 3,
        Neck = 1 << 4,
        Shoulders = 1 << 5,
        Shirt = 1 << 6,
        Jacket = 1 << 7,
        Rig = 1 << 8,
        WristLeft = 1 << 9,
        WristRight = 1 << 10,
        GloveLeft = 1 << 11,
        GloveRight = 1 << 12,
        Belt = 1 << 13,
        Pants = 1 << 14,
        DroplegLeft = 1 << 15,
        DroplegRight = 1 << 16,
        Boots = 1 << 17,
        ElbowLeft = 1 << 18,
        ElbowRight = 1 << 19,
        KneeLeft = 1 << 20,
        KneeRight = 1 << 21
    }

    private Creature _creature;

    public Channels usedChannels;

    public List<Transform> maleBones;
    public List<Transform> femaleBones;

    public GameObject maleRoot;
    public GameObject femaleRoot;

    [NonSerialized]
    public List<ColliderGroup> ColliderGroups = new();

    #region Wrappers

    public void Apply(Creature creature)
        {
            _creature = creature;
            
            if (_creature.data.gender == CreatureData.Gender.Male)
            {
                Apply(femaleRoot, maleBones);
            }
            else if (_creature.data.gender == CreatureData.Gender.Female)
            {
                Apply(maleRoot, femaleBones);
            }
        }
    
        public void Remove()
        {
            if (_creature.data.gender == CreatureData.Gender.Male)
            {
                Remove(femaleRoot, maleBones);
            }
            else if (_creature.data.gender == CreatureData.Gender.Female)
            {
                Remove(maleRoot, femaleBones);
            }
        }

    #endregion

    private void Apply(GameObject objectToDelete, List<Transform> bones)
    {
        Destroy(objectToDelete);

        var meshBones = _creature.ragdoll.bones.Select(x => x.mesh).ToList();
        
        foreach (var bone in bones)
        {
            var target = meshBones.FirstOrDefault(cb => cb.name.Equals(bone.name + "_Mesh"));
            bone.SetParent(target);
            bone.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            bone.localScale = Vector3.one;
        }
    }

    private void Remove(GameObject root, List<Transform> bones)
    {
        foreach (var bone in bones.ToArray())
        {
            Destroy(bone.gameObject);
        }
        Destroy(root);
        
        Destroy(gameObject);
    }
}