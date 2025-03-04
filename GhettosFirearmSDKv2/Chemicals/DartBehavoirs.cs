using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Chemicals/Darts")]
public class DartBehavoirs : MonoBehaviour
{
    public enum Behaviours
    {
        Heal,
        MissingTextures,
        Gun,
        Poison
    }

    public Cartridge cartridge;
    public Projectile projectile;
    public Behaviours behaviour;

    private void Awake()
    {
        if (cartridge)
            cartridge.OnFiredWithHitPointsAndMuzzleAndCreatures += Cartridge_OnFiredWithHitPointsAndMuzzleAndCreatures;
            
        if (projectile)
            projectile.HitEvent += ProjectileOnHitEvent;
    }

    private void OnDestroy()
    {
        if (cartridge)
            cartridge.OnFiredWithHitPointsAndMuzzleAndCreatures -= Cartridge_OnFiredWithHitPointsAndMuzzleAndCreatures;
            
        if (projectile)
            projectile.HitEvent -= ProjectileOnHitEvent;
    }

    private void ProjectileOnHitEvent(List<Creature> hitCreatures, List<Creature> killedCreatures)
    {
        switch (behaviour)
        {
            case Behaviours.Gun:
                Gun(hitCreatures);
                break;
            case Behaviours.MissingTextures:
                MissingTexture(hitCreatures);
                break;
            case Behaviours.Heal:
                Heal(hitCreatures);
                break;
            case Behaviours.Poison:
                Poison(hitCreatures);
                break;
        }
    }

    private void Cartridge_OnFiredWithHitPointsAndMuzzleAndCreatures(List<Vector3> hitPoints, List<Vector3> trajectories, List<Creature> hitCreatures, Transform muzzle, List<Creature> killedCreatures)
    {
        switch (behaviour)
        {
            case Behaviours.Gun:
                Gun(hitCreatures);
                break;
            case Behaviours.MissingTextures:
                MissingTexture(hitCreatures);
                break;
            case Behaviours.Heal:
                Heal(hitCreatures);
                break;
            case Behaviours.Poison:
                Poison(hitCreatures);
                break;
        }
    }

    private void Heal(List<Creature> creatures)
    {
        foreach (var c in creatures)
        {
            c.Heal(500, null);
            if (c.isKilled)
            {
                c.Heal(500, null);
                c.Resurrect(c.maxHealth, null);
                c.brain.Load(c.brain.instance.id);
                c.ragdoll.SetState(Ragdoll.State.Destabilized);
                c.Heal(500, null);
            }
        }
    }

    private void MissingTexture(List<Creature> creatures)
    {
        foreach (var c in creatures)
        {
            c.Kill();
            c.ragdoll.SetState(Ragdoll.State.Disabled);
            foreach (var r in c.renderers)
            {
                r.renderer.material = null;
            }
        }
    }

    private void Gun(List<Creature> creatures)
    {
        foreach (var c in creatures)
        {
            var v = c.ragdoll.GetPart(RagdollPart.Type.Torso).bone.mesh.position;
            c.Despawn();
            if (GunLockerSaveData.allPrebuilts.Count > 0)
                GunLockerSaveData.allPrebuilts[Random.Range(0, GunLockerSaveData.allPrebuilts.Count - 1)].SpawnAsync(_ => { }, v + Vector3.up);
        }
    }

    private void Poison(List<Creature> creatures)
    {
        foreach (var c in creatures)
        {
            StartCoroutine(PoisonIE(c));
        }
    }

    private IEnumerator PoisonIE(Creature c)
    {
        yield return new WaitForSeconds(2f);
        while (!c.isKilled)
        {
            c.Damage(new CollisionInstance(new DamageStruct(DamageType.Energy, FireMethods.EvaluateDamage(5, c))));
            yield return new WaitForSeconds(2f);
        }
    }
}