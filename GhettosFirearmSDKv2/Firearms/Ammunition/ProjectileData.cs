using System.Collections.Generic;
using System.Text;
using GhettosFirearmSDKv2.Explosives;
using UnityEngine;

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

        public bool isInert;
        public bool isHitscan = true;
        public float accuracyMultiplier = 1;
        public float recoil;
        public float recoilUpwardsModifier = 2f;
        public bool playFirearmDefaultMuzzleFlash = true;
        public bool alwaysSuppressed;
        public bool allowPitchVariation = true;
        public bool overrideFireSounds;
        public List<AudioSource> fireSounds;
        public List<AudioSource> suppressedFireSounds;
        public bool overrideMuzzleFlashLightColor;
        public Color muzzleFlashLightColorOne;
        public Color muzzleFlashLightColorTwo;

        //hitscan
        public int projectileCount = 1;
        public float projectileSpread;
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
        public bool isExplosive;
        public bool isElectrifying;
        public bool slicesBodyParts;
        public bool enoughToIncapitate = true;
        public bool lethalHeadshot = true;
        public bool forceDestabilize;
        public bool forceIncapitate;
        public float fireDamage;

        //temporary knockout
        public bool knocksOutTemporarily;
        public float temporaryKnockoutTime;
        public float kockoutDelay;

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
        public float destroyTime;

        private void Awake()
        {
            if (projectileCount <= 1) projectileSpread = 0f;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            if (isInert)
            {
                builder.AppendLine("Inert");
                return builder.ToString();
            }

            if (!isHitscan)
            {
                Util.AddInfoToBuilder("Velocity", muzzleVelocity, builder);
                if (!string.IsNullOrWhiteSpace(additionalInformation))
                {
                    builder.AppendLine();
                    builder.AppendLine(additionalInformation);
                }
            }
            else
            {
                if (projectileCount > 0)
                {
                    if (projectileCount == 1)
                    {
                        Util.AddInfoToBuilder("Damage", $"{damagePerProjectile / 50 * 100}%", builder);
                        Util.AddInfoToBuilder("Force", forcePerProjectile, builder);
                    }
                    else if (projectileCount > 1)
                    {
                        
                        Util.AddInfoToBuilder("Projectile count", projectileCount, builder);
                        Util.AddInfoToBuilder("Damage per projectile", $"{damagePerProjectile / 50 * 100}%", builder);
                        Util.AddInfoToBuilder("Force per projectile", forcePerProjectile, builder);
                    }
                    if (fireDamage > 0)
                    {
                        Util.AddInfoToBuilder("Fire damage", $"{fireDamage}%", builder);
                    }
                    Util.AddInfoToBuilder("Range", projectileRange, builder);
                    Util.AddInfoToBuilder("Penetration level", penetrationPower, builder);
                    if (gameObject.GetComponentInChildren<TracerModule>() != null)
                        builder.AppendLine("Has tracer charge");
                    if (forceDestabilize && !knocksOutTemporarily)
                        builder.AppendLine("Always destabilizes target");
                    if (forceIncapitate)
                        builder.AppendLine("Incapacitates permanently");
                    else if (knocksOutTemporarily)
                        builder.AppendLine("$Incapacitates {temporaryKnockoutTime}s");
                    if (isElectrifying)
                        builder.AppendLine("$Electrifies for {tasingDuration}s, force: {tasingForce}");
                    if (isExplosive && explosiveData.radius > 0)
                        builder.AppendLine("$Explodes: {explosiveData.radius}m radius, {explosiveData.force} force, {explosiveData.damage} damage");
                }
                if (!string.IsNullOrWhiteSpace(additionalInformation))
                {
                    builder.AppendLine(additionalInformation);
                }
            }

            return builder.ToString();
        }
    }
}
