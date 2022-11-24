using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2.Explosives
{
    public class SimpleExplosive : Explosive
    {
        public ExplosiveData data;
        public AudioSource[] audioEffects;
        public ParticleSystem effect;
        public bool destroyItem = true;

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
            FireMethods.HitscanExplosion(this.transform.position, data, item, out List<Creature> hc, out List<Item> hi);
            if (item != null && destroyItem)
            {
                item.Despawn();
                item.Despawn(0.2f);
                item.Despawn(0.6f);
                item.Despawn(1f);
                item.Despawn(2f);
            }
            base.ActualDetonate();
        }
    }
}
