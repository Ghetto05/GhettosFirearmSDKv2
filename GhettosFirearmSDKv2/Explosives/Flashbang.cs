using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2.Explosives
{
    public class Flashbang : Explosive
    {
        public AudioSource[] audioEffects;
        public ParticleSystem effect;
        public float range;
        public float time;
        public string effectId;

        private void Awake()
        {
        }

        public override void ActualDetonate()
        {
            try
            {
                if (effect != null)
                {
                    effect.gameObject.transform.SetParent(null);
                    Player.local.StartCoroutine(delayedDestroy(effect.gameObject, effect.main.duration + 1f));
                    effect.Play();
                }

                item.physicBody.rigidBody.AddForce(Random.insideUnitSphere * 500);

                if (!string.IsNullOrWhiteSpace(effectId)) Catalog.GetData<EffectData>(effectId).Spawn(transform).Play();

                Util.PlayRandomAudioSource(audioEffects);
                Util.AlertAllCreaturesInRange(transform.position, 50);
                foreach (AudioSource s in audioEffects)
                {
                    s.gameObject.transform.SetParent(null);
                    Player.local.StartCoroutine(delayedDestroy(s.gameObject, s.clip.length + 1f));
                }

                if (Vector3.Distance(transform.position, Player.local.head.cam.transform.position) < range && !Raycast(Player.currentCreature)) Chemicals.PlayerEffectsAndChemicalsModule.Flashbang(time);

                foreach (Creature cr in Creature.allActive)
                {
                    Transform t = cr.animator.GetBoneTransform(HumanBodyBones.Head);
                    if (!cr.isPlayer && !cr.isKilled && Vector3.Distance(t.position, transform.position) < range && !Raycast(cr))
                    {
                        cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                        StartCoroutine(FireMethods.TemporaryKnockout(time, 0, cr));
                    }
                }
            }
            catch (System.Exception)
            { }
            base.ActualDetonate();
        }

        private bool Raycast(Creature cr)
        {
            int layer = LayerMask.GetMask("Default");
            Vector3 pos = cr.animator.GetBoneTransform(HumanBodyBones.Head).position;
            return Physics.Raycast(cr.centerEyes.position, (transform.position - pos).normalized, out RaycastHit hit, Vector3.Distance(pos, transform.position) - 0.1f, layer);
        }
    }
}
