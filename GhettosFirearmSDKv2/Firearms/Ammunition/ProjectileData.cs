using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using GhettosFirearmSDKv2.Explosives;

namespace GhettosFirearmSDKv2
{
    public class ProjectileData : MonoBehaviour
    {
        public enum PenetrationLevels
        {
            None,
            Leather,
            Plate,
            Items,
            Kevlar,
            World
        }

        public string additionalInformation;

        public bool isHitscan = true;
        public float accuracyMultiplier = 1;
        public float recoil;
        public float recoilUpwardsModifier = 2f;
        public bool playFirearmDefaultMuzzleFlash = true;
        public bool alwaysSuppressed = false;
        public bool allowPitchVariation = true;
        public bool overrideFireSounds = false;
        public List<AudioSource> fireSounds;
        public List<AudioSource> suppressedFireSounds;
        public bool overrideMuzzleFlashLightColor = false;
        public Color muzzleFlashLightColorOne;
        public Color muzzleFlashLightColorTwo;

        //hitscan
        public int projectileCount = 1;
        public float projectileSpread = 0;
        public float projectileRange = 200;
        public float damagePerProjectile = 20;
        public float forcePerProjectile = 100;
        public bool drawsImpactDecal = true;
        public bool hasImpactEffect = true;
        public bool hasBodyImpactEffect = true;
        public string customImpactDecalId;
        public string customRagdollImpactEffectId;
        public string customImpactEffectId;
        public PenetrationLevels penetrationPower;
        public bool isExplosive = false;
        public bool isElectrifying = false;
        public bool slicesBodyParts = false;
        public bool enoughToIncapitate = true;
        public bool lethalHeadshot = true;
        public bool forceDestabilize = false;
        public bool forceIncapitate = false;

        //temporary knockout
        public bool knocksOutTemporarily = false;
        public float temporaryKnockoutTime = 0f;
        public float kockoutDelay = 0f;

        //hitscan explosive
        public ExplosiveData explosiveData;
        public ParticleSystem explosiveEffect;
        public List<AudioSource> explosiveSoundEffects;

        //hitscan electrification
        public float tasingDuration = 5;
        public float tasingForce = 1;
        public bool playTasingEffect = true;

        //physical
        public string projectileItemId;
        public float muzzleVelocity = 200;
        public float destroyTime = 0f;

        void Awake()
        {
            if (projectileCount <= 1) projectileSpread = 0f;
        }
    }
}
