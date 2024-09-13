using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2;

public class CustomCartridgeDeathSound : MonoBehaviour
{
    public Cartridge cartridge;
    public AudioContainer audioContainer;

    private void Start()
    {
        cartridge.OnFiredWithHitPointsAndMuzzleAndCreatures += OnFired;
    }

    private void OnFired(List<Vector3> hitpoints, List<Vector3> trajectories, List<Creature> hitcreatures, Transform muzzle, List<Creature> killedCreatures)
    {
        foreach (var cr in killedCreatures)
        {
            cr.brain.instance.GetModule<BrainModuleSpeak>().Unload();
            cr.brain.instance.tree.Reset();
            audioContainer.GetRandomAudioClip().PlayClipAtPoint(cr.ragdoll.GetPart(RagdollPart.Type.Head).bone.mesh.position, 1, AudioMixerName.Voice);
        }
    }
}