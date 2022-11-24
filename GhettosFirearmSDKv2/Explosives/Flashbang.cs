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

        private void Awake()
        {
        }

        public override void ActualDetonate()
        {
            if (effect != null)
            {
                effect.gameObject.transform.SetParent(null);
                Player.local.StartCoroutine(delayedDestroy(effect.gameObject, effect.main.duration + 1f));
                effect.Play();
            }
            
            Util.PlayRandomAudioSource(audioEffects);
            Util.AlertAllCreaturesInRange(this.transform.position, 50);
            foreach (AudioSource s in audioEffects)
            {
                s.gameObject.transform.SetParent(null);
                Player.local.StartCoroutine(delayedDestroy(s.gameObject, s.clip.length + 1f));
            }

            if (Vector3.Distance(this.transform.position, Player.local.head.cam.transform.position) < range) Chemicals.PlayerEffectsAndChemicalsModule.Flashbang(time);
            
            foreach (Creature cr in Creature.allActive)
            {
                Transform t = cr.animator.GetBoneTransform(HumanBodyBones.Head);
                if (!cr.isPlayer && !cr.isKilled && Vector3.Distance(t.position, this.transform.position) < range)
                {
                    cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                }
            }
            base.ActualDetonate();
        }
    }
}
