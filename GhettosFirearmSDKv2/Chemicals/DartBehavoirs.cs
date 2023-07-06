using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
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
        public Behaviours behaviour;

        private void Awake()
        {
            cartridge.OnFiredWithHitPointsAndMuzzleAndCreatures += Cartridge_OnFiredWithHitPointsAndMuzzleAndCreatures;
        }

        private void Cartridge_OnFiredWithHitPointsAndMuzzleAndCreatures(List<Vector3> hitPoints, List<Vector3> trajectories, List<Creature> hitCreatures, Transform muzzle)
        {
            if (behaviour == Behaviours.Heal) Heal(hitCreatures);
            else if (behaviour == Behaviours.MissingTextures) MissingTexture(hitCreatures);
            else if (behaviour == Behaviours.Gun) Gun(hitCreatures);
            else if (behaviour == Behaviours.Poison) Poison(hitCreatures);
        }

        private void Heal(List<Creature> creatures)
        {
            foreach (Creature c in creatures)
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
            foreach (Creature c in creatures)
            {
                c.Kill();
                c.ragdoll.SetState(Ragdoll.State.Disabled);
                foreach (Creature.RendererData r in c.renderers)
                {
                    r.renderer.material = null;
                }
            }
        }

        private void Gun(List<Creature> creatures)
        {
            foreach (Creature c in creatures)
            {
                Vector3 v = c.transform.position;
                c.Despawn();
                if (GunLockerSaveData.allPrebuilts.Count > 0) GunLockerSaveData.allPrebuilts[Random.Range(0, GunLockerSaveData.allPrebuilts.Count - 1)].SpawnAsync(item => { }, v + Vector3.up);
            }
        }

        private void Poison(List<Creature> creatures)
        {
            foreach (Creature c in creatures)
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
}
