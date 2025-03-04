using System;
using GhettosFirearmSDKv2.Chemicals;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2.Explosives;

public class Flashbang : Explosive
{
    public AudioSource[] audioEffects;
    public ParticleSystem effect;
    public float range;
    public float time;
    public string effectId;

    public override void ActualDetonate()
    {
        try
        {
            if (effect != null)
            {
                effect.gameObject.transform.SetParent(null);
                Player.local.StartCoroutine(DelayedDestroy(effect.gameObject, effect.main.duration + 1f));
                effect.Play();
            }

            item.physicBody.rigidBody.AddForce(Random.insideUnitSphere * 500);

            if (!string.IsNullOrWhiteSpace(effectId)) Catalog.GetData<EffectData>(effectId).Spawn(transform).Play();

            Util.PlayRandomAudioSource(audioEffects);
            Util.AlertAllCreaturesInRange(transform.position, 50);
            foreach (var s in audioEffects)
            {
                s.gameObject.transform.SetParent(null);
                Player.local.StartCoroutine(DelayedDestroy(s.gameObject, s.clip.length + 1f));
            }

            if (Vector3.Distance(transform.position, Player.local.head.cam.transform.position) < range && !Raycast(Player.currentCreature)) PlayerEffectsAndChemicalsModule.Flashbang(time);

            foreach (var cr in Creature.allActive)
            {
                var t = cr.animator.GetBoneTransform(HumanBodyBones.Head);
                if (!cr.isPlayer && !cr.isKilled && Vector3.Distance(t.position, transform.position) < range && !Raycast(cr))
                {
                    cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                    StartCoroutine(FireMethods.TemporaryKnockout(time, 0, cr));
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        base.ActualDetonate();
    }

    private bool Raycast(Creature cr)
    {
        var layer = LayerMask.GetMask("Default");
        var pos = cr.animator.GetBoneTransform(HumanBodyBones.Head).position;
        return Physics.Raycast(cr.centerEyes.position, (transform.position - pos).normalized, out _, Vector3.Distance(pos, transform.position) - 0.1f, layer);
    }
}