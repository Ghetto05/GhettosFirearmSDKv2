using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Explosives
{
    public class SimpleExplosive : Explosive
    {
        public ExplosiveData data;
        public AudioSource[] audioEffects;
        public ParticleSystem effect;
        public bool destroyItem = true;

        public override void ActualDetonate()
        {
            if (effect != null)
            {
                effect.gameObject.transform.SetParent(null);
                Player.local.StartCoroutine(DelayedDestroy(effect.gameObject, effect.main.duration + 1f));
                effect.Play();
            }
            
            Util.PlayRandomAudioSource(audioEffects);
            Util.AlertAllCreaturesInRange(transform.position, 50);
            foreach (var s in audioEffects)
            {
                s.gameObject.transform.SetParent(null);
                Player.local.StartCoroutine(DelayedDestroy(s.gameObject, s.clip.length + 1f));
            }
            FireMethods.HitscanExplosion(transform.position, data, item, out _, out _);
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
