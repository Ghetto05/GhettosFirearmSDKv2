using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using ThunderRoad.Reveal;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2;

public class FireMethods : MonoBehaviour
{
    public static void Fire(Item gun, Transform muzzle, ProjectileData data, out List<Vector3> hitPoints, out List<Vector3> trajectories, out List<Creature> hitCreatures, out List<Creature> killedCreatures, float damageMultiplier, bool useAISpread)
    {
        hitPoints = [];
        trajectories = [];
        hitCreatures = [];
        killedCreatures = [];
        try
        {
            if (data.isHitscan)
            {
                hitCreatures = FireHitScan(muzzle, data, gun, out hitPoints, out trajectories, out killedCreatures, damageMultiplier, useAISpread);
            }
            else
            {
                FireItem(muzzle, data, gun);
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void ApplyRecoil(Transform transform, Item item, float force, float upwardsModifier, float firearmRecoilModifier, List<FirearmBase.RecoilModifier> modifiers)
    {
        if (Settings.noRecoil)
        {
            return;
        }

        var upMod = 1f;
        var linMod = 1f;

        if (modifiers is not null)
        {
            foreach (var mod in modifiers)
            {
                upMod *= mod.MuzzleRiseModifier;
                linMod *= mod.Modifier;
            }
        }

        item.physicBody.AddForce(-transform.forward * (force * linMod) * firearmRecoilModifier, ForceMode.Impulse);
        item.physicBody.AddRelativeTorque(Vector3.right * (force * upMod * upwardsModifier) * firearmRecoilModifier, ForceMode.Impulse);
    }

    public static List<Creature> FireHitScan(Transform muzzle, ProjectileData data, Item item, out List<Vector3> returnedEndpoints, out List<Vector3> returnedTrajectories, out List<Creature> killedCreatures, float damageMultiplier, bool useAISpread)
    {
        returnedEndpoints = [];
        returnedTrajectories = [];
        killedCreatures = [];
        var hitParts = new List<RagdollPart>();
        var hitCreatures = new List<Creature>();
        var hitShootables = new List<object>();
        try
        {
            for (var i = 0; i < data.projectileCount; i++)
            {
                var tempMuz = new GameObject().transform;
                tempMuz.parent = muzzle;
                tempMuz.localPosition = Vector3.zero;
                if (!useAISpread || data.projectileCount > 1)
                {
                    tempMuz.localEulerAngles = new Vector3(Random.Range(-data.projectileSpread, data.projectileSpread), Random.Range(-data.projectileSpread, data.projectileSpread), 0);
                }
                else
                {
                    tempMuz.localEulerAngles = new Vector3(Random.Range(-Settings.aiFirearmSpread, Settings.aiFirearmSpread), Random.Range(-Settings.aiFirearmSpread, Settings.aiFirearmSpread), 0);
                }
                hitCreatures.AddRange(HitScan(tempMuz, data, item, out var endpoint, damageMultiplier, killedCreatures, hitParts, hitShootables));
                returnedEndpoints.Add(endpoint);
                returnedTrajectories.Add(tempMuz.forward);
                Destroy(tempMuz.gameObject);
            }
        }
        catch (Exception)
        {
            // ignored
        }

        Score.local.ShotFired(hitCreatures.Any() || hitShootables.Any(), hitParts.Any(x => x.type == RagdollPart.Type.Head));

        return hitCreatures;
    }

    private static List<Creature> HitScan(Transform muzzle, ProjectileData data, Item gunItem, out Vector3 endpoint, float damageMultiplier, List<Creature> killedCreatures, List<RagdollPart> hitParts, List<object> hitShootables)
    {
        var forward = muzzle.forward;

        #region physics toggle

        foreach (var physicsToggleHit in Physics.RaycastAll(muzzle.position, muzzle.forward, Mathf.Infinity, LayerMask.GetMask("BodyLocomotion")))
        {
            if (physicsToggleHit.collider.gameObject.GetComponentInParent<Creature>() is not { } cr)
            {
                continue;
            }

            foreach (var part in cr.ragdoll.parts)
            {
                part.gameObject.SetActive(true);
            }

            if (!cr.equipment)
            {
                continue;
            }

            if (cr.equipment.GetHeldItem(Side.Left))
            {
                cr.equipment.GetHeldItem(Side.Left).SetColliders(true);
            }

            if (cr.equipment.GetHeldItem(Side.Right))
            {
                cr.equipment.GetHeldItem(Side.Right).SetColliders(true);
            }
        }

        #endregion physics toggle

        var hitCreatures = new List<Creature>();
        var layer = LayerMask.GetMask(
            "NPC",
            "Ragdoll",
            "Default",
            "DroppedItem",
            "MovingItem",
            "PlayerLocomotionObject",
            "Avatar",
            "PlayerHandAndFoot",
            "MovingObjectOnly",
            "NoLocomotion",
            "ItemAndRagdollOnly");

        var hits = Physics.RaycastAll(muzzle.position, forward, data.projectileRange, layer)
                          .OrderBy(h => Vector3.Distance(h.point, muzzle.position))
                          .ToList();
        var power = (int)data.penetrationPower;

        #region no hits

        if (hits.Count == 0)
        {
            endpoint = Vector3.zero;
            return hitCreatures;
        }

        #endregion no hits

        var successfulHits = new List<HitData>();

        #region explosive

        if (data.isExplosive)
        {
            var hit = hits[0];
            HitscanExplosion(hit.point, data.explosiveData, gunItem, out _, out _);
            if (data.explosiveEffect)
            {
                data.explosiveEffect.gameObject.transform.SetParent(null);
                data.explosiveEffect.transform.position = hit.point;
                Player.local.StartCoroutine(Explosive.DelayedDestroy(data.explosiveEffect.gameObject, data.explosiveEffect.main.duration + 9f));
                data.explosiveEffect.Play();

                var audio = Util.GetRandomFromList(data.explosiveSoundEffects);
                audio.gameObject.transform.SetParent(null);
                audio.transform.position = hit.point;
                audio.Play();
                Player.local.StartCoroutine(Explosive.DelayedDestroy(audio.gameObject, audio.clip.length + 1f));
            }
        }

        #endregion explosive

        var processing = true;
        foreach (var hit in hits.Where(_ => processing))
        {
            try
            {
                var c = ProcessHit(muzzle, (HitData)hit, successfulHits, data, damageMultiplier, hitCreatures, killedCreatures, hitParts, hitShootables, gunItem, out var lowerDamageLevel, out var cancel, ref power);
                if (lowerDamageLevel)
                {
                    if (power is (int)ProjectileData.PenetrationLevels.None or (int)ProjectileData.PenetrationLevels.Leather)
                    {
                        processing = false;
                    }
                    else
                    {
                        power -= 2;
                    }
                }

                if (cancel)
                {
                    processing = false;
                }
                if (c)
                {
                    hitCreatures.Add(c);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        endpoint = successfulHits.Count > 0 ? successfulHits.Last().Point : Vector3.zero;

        return hitCreatures;
    }

    public static Creature ProcessHit(Transform muzzle, HitData hit, List<HitData> successfulHits, ProjectileData data, float damageMultiplier, List<Creature> hitCreatures, List<Creature> killedCreatures, List<RagdollPart> hitParts, List<object> hitShootables, Item gunItem, out bool lowerDamageLevel, out bool cancel, ref int penetrationPower)
    {
        if (hit.Collider.GetComponentInParent<Shootable>() is { } shootable)
        {
            shootable.Shoot((ProjectileData.PenetrationLevels)penetrationPower);
            hitShootables.Add(shootable);
        }

        #region Breakables

        foreach (var breakable in hit.Collider.GetComponentsInParent<Breakable>())
        {
            breakable.Break();
            hitShootables.Add(breakable);
        }

        foreach (var breakable in hit.Collider.GetComponentsInParent<SimpleBreakable>())
        {
            breakable.Break();
            hitShootables.Add(breakable);
        }

        #endregion

        #region static non creature hit

        if (!hit.Rigidbody)
        {
            if (data.hasImpactEffect)
            {
                var ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.Point, hit.Normal == Vector3.zero ? Quaternion.LookRotation(Vector3.one * 0.0001f) : Quaternion.LookRotation(hit.Normal));
                ei.SetIntensity(100f);
                ei.Play();
            }

            successfulHits.Add(hit);
            lowerDamageLevel = true;
            cancel = GetRequiredPenetrationLevel(hit.Collider) > penetrationPower;
            return null;
        }

        #endregion static non creature hit

        #region creature hit

        if (hit.Collider.gameObject.GetComponentInParent<Ragdoll>() is { } rag)
        {
            if (!hitCreatures.Contains(rag.creature))
            {
                var cr = rag.creature;
                var ragdollPart = hit.Collider.gameObject.GetComponentInParent<RagdollPart>();
                var penetrated = GetRequiredPenetrationLevel(hit, muzzle.forward, gunItem) <= penetrationPower;

                hitCreatures.Add(cr);
                hitParts.Add(ragdollPart);

                #region Impact effect

                if (data.hasBodyImpactEffect && !Settings.disableGore)
                {
                    //Effect
                    var ei = Catalog.GetData<EffectData>(penetrated ? "BulletImpactFlesh_Ghetto05_FirearmSDKv2" : "BulletImpactGround_Ghetto05_FirearmSDKv2")
                                    .Spawn(hit.Point,
                                        hit.Normal == Vector3.zero ? Quaternion.LookRotation(Vector3.one * 0.0001f) : Quaternion.LookRotation(hit.Normal),
                                        hit.Collider.transform);
                    ei.SetIntensity(100f);
                    ei.Play();
                }

                if (data.drawsImpactDecal && penetrated)
                {
                    DrawDecal(ragdollPart, hit, data.customImpactDecalId);
                }

                #endregion Impact effect

                if (data.drawsImpactDecal)
                {
                    try
                    {
                        BloodSplatter(hit.Point, muzzle.forward, data.forcePerProjectile, data.projectileCount, penetrationPower, penetrated);
                    }
                    catch (Exception)
                    {
                        /* ignored */
                    }
                }

                #region Damage level determination

                float damageModifier = 1;
                switch (ragdollPart.type)
                {
                    case RagdollPart.Type.Head: //damage = infinity, remove voice, push(3)
                        damageModifier = penetrated && data.lethalHeadshot ? Mathf.Infinity : 2;
                        if (penetrated && data.lethalHeadshot && WouldCreatureBeKilled(data.damagePerProjectile * damageModifier, cr) && !cr.isPlayer)
                        {
                            cr.brain.instance.GetModule<BrainModuleSpeak>().Unload();
                            cr.brain.instance.tree.Reset();
                            cr.StartCoroutine(DelayedStopAnimating(cr));
                        }
                        if (penetrated && data.slicesBodyParts)
                        {
                            ragdollPart.TrySlice();
                        }
                        break;

                    case RagdollPart.Type.Neck: //damage = infinity, push(1)
                        damageModifier = penetrated && data.lethalHeadshot ? Mathf.Infinity : 2;
                        break;

                    case RagdollPart.Type.Torso: //damage = damage, push(2)
                        if (penetrated && Settings.incapacitateOnTorsoShot > 0f && data.enoughToIncapitate && !cr.isKilled && !cr.isPlayer)
                        {
                            cr.StartCoroutine(TemporaryKnockout(Settings.incapacitateOnTorsoShot, 0, cr));
                        }
                        break;

                    case RagdollPart.Type.LeftArm: //damage = damage/3, release weapon, push(1)
                        damageModifier = 1f / 3;
                        if (!cr.isKilled && !cr.isPlayer)
                        {
                            cr.handLeft.TryRelease();
                        }
                        break;

                    case RagdollPart.Type.RightArm: //damage = damage/3, release weapon, push(1)
                        damageModifier = 1f / 3;
                        if (!cr.isKilled && !cr.isPlayer)
                        {
                            cr.handRight.TryRelease();
                        }
                        break;

                    case RagdollPart.Type.RightFoot:
                    case RagdollPart.Type.LeftFoot: //damage = damage/4, destabilize, push(3)
                        damageModifier = 0.25f;
                        if (!cr.isKilled && !cr.isPlayer)
                        {
                            cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                        }
                        break;

                    case RagdollPart.Type.LeftHand: //damage = damage/4, release weapon, push(1)
                        damageModifier = 0.25f;
                        if (!cr.isKilled && !cr.isPlayer)
                        {
                            cr.handLeft.TryRelease();
                        }
                        break;

                    case RagdollPart.Type.RightHand: //damage = damage/4, release weapon, push(1)
                        damageModifier = 0.25f;
                        if (!cr.isKilled && !cr.isPlayer)
                        {
                            cr.handRight.TryRelease();
                        }
                        break;

                    case RagdollPart.Type.RightLeg:
                    case RagdollPart.Type.LeftLeg: //damage = damage/3, destabilize, push(3)
                        damageModifier = 1f / 3;
                        if (!cr.isKilled && !cr.isPlayer)
                        {
                            cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                        }
                        break;

                    case RagdollPart.Type.LeftWing:
                    case RagdollPart.Type.RightWing:
                    case RagdollPart.Type.Tail: //damage = damage/3, push(1)
                        damageModifier = 1f / 3;
                        break;
                }

                cr.TryPush(Creature.PushType.Hit, muzzle.forward, 0);
                if (penetrated && data.slicesBodyParts && !cr.isPlayer && Slice(ragdollPart))
                {
                    ragdollPart.TrySlice();
                }
                if (!penetrated)
                {
                    damageModifier /= 4;
                }

                #endregion Damage level determination

                #region Damaging

                var coll = new CollisionInstance(new DamageStruct(Settings.bulletsAreBlunt ? DamageType.Blunt : DamageType.Pierce, data.damagePerProjectile));
                coll.damageStruct.damage = EvaluateDamage(data.damagePerProjectile * damageModifier, cr);
                coll.damageStruct.damageType = Settings.bulletsAreBlunt ? DamageType.Blunt : DamageType.Pierce;
                coll.sourceMaterial = Catalog.GetData<MaterialData>("Blade");
                coll.targetMaterial = Catalog.GetData<MaterialData>("Flesh");
                coll.targetColliderGroup = ragdollPart.colliderGroup;
                coll.sourceColliderGroup = gunItem.colliderGroups[0];
                coll.contactPoint = hit.Point;
                coll.contactNormal = hit.Normal;
                coll.impactVelocity = muzzle.forward * 200;

                var penPoint = new GameObject().transform;
                penPoint.position = hit.Point;
                penPoint.rotation = hit.Normal == Vector3.zero ? Quaternion.LookRotation(Vector3.one * 0.0001f) : Quaternion.LookRotation(hit.Normal);
                penPoint.parent = hit.Transform;
                coll.damageStruct.penetration = DamageStruct.Penetration.Hit;
                coll.damageStruct.penetrationPoint = penPoint;
                coll.damageStruct.penetrationDepth = 10;
                coll.damageStruct.hitRagdollPart = ragdollPart;
                coll.intensity = EvaluateDamage(data.damagePerProjectile * damageModifier * damageMultiplier, cr);
                coll.pressureRelativeVelocity = muzzle.forward * 200;

                try
                {
                    if (WouldCreatureBeKilled(data.damagePerProjectile * damageModifier, cr) && !cr.isKilled && !killedCreatures.Contains(cr))
                    {
                        killedCreatures.Add(cr);
                        cr.Kill(coll);
                    }
                    else
                    {
                        cr.Damage(coll);
                    }
                }
                catch (Exception)
                {
                    /* ignored */
                }

                #endregion Damaging

                #region Additional Effects

                //Taser
                if (data.isElectrifying)
                {
                    cr.TryElectrocute(data.tasingForce, data.tasingDuration, true, false, data.playTasingEffect ? Catalog.GetData<EffectData>("ImbueLightningRagdoll") : null);
                }

                //Force knockout
                if ((data.forceDestabilize || data.isElectrifying) && !cr.isPlayer && !cr.isKilled)
                {
                    cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                }

                //RB Push
                if (cr.isKilled && !cr.isPlayer && hit.Rigidbody)
                {
                    hit.Rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
                    //cr.locomotion.rb.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
                }

                //Stun
                else if (!cr.isPlayer)
                {
                    if (data.forceIncapitate)
                    {
                        cr.brain.AddNoStandUpModifier(gunItem);
                    }
                    else if (data.knocksOutTemporarily)
                    {
                        gunItem.StartCoroutine(TemporaryKnockout(data.temporaryKnockoutTime, data.kockoutDelay, cr));
                    }
                }

                if (data.fireDamage > 0)
                {
                    cr.Inflict("Burning", gunItem, float.PositiveInfinity, data.fireDamage);
                }

                var hitReaction = cr.brain.instance.GetModule<BrainModuleHitReaction>();
                hitReaction.SetStagger(hitReaction.staggerMedium);

                #endregion Additional Effects

                cancel = !penetrated;
                lowerDamageLevel = true;
                return cr;
            }

            lowerDamageLevel = false;
            cancel = false;
            return null;
        }

        #endregion creature hit

        #region non creature hit

        if (data.hasImpactEffect)
        {
            var ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.Point, hit.Normal == Vector3.zero ? Quaternion.LookRotation(Vector3.one * 0.0001f) : Quaternion.LookRotation(hit.Normal));
            ei.SetIntensity(100f);
            ei.Play();
        }

        hit.Rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);

        lowerDamageLevel = true;
        cancel = GetRequiredPenetrationLevel(hit.Collider) > penetrationPower;
        return null;

        #endregion non creature hit
    }

    public struct HitData
    {
        public Collider Collider;
        public Vector3 Point;
        public Rigidbody Rigidbody;
        public Vector3 Normal;
        public Transform Transform;

        public static explicit operator HitData(RaycastHit hit)
        {
            return new HitData
                   {
                       Collider = hit.collider,
                       Transform = hit.transform,
                       Normal = hit.normal,
                       Point = hit.point,
                       Rigidbody = hit.rigidbody
                   };
        }

        public static explicit operator HitData(CollisionInstance hit)
        {
            return new HitData
                   {
                       Collider = hit.targetCollider,
                       Transform = hit.targetCollider?.transform,
                       Normal = hit.contactNormal,
                       Point = hit.contactPoint,
                       Rigidbody = hit.targetCollider?.attachedRigidbody
                   };
        }
    }

    private static IEnumerator DelayedStopAnimating(Creature cr)
    {
        yield return new WaitForSeconds(1);

        cr.brain.instance.StopModuleUsingAnyBodyPart();
        cr.brain.instance.tree.Reset();
        cr.animator.StopPlayback();
    }

    public static bool Slice(RagdollPart part)
    {
        return !Settings.disableGore && (part.sliceAllowed || part.name.Equals("Spine")) && !part.ragdoll.creature.isPlayer;
    }

    private static void DrawDecal(RagdollPart rp, HitData hit, string customDecal, bool isGore = true)
    {
        if (Settings.disableGore && isGore)
        {
            return;
        }

        EffectModuleReveal rem;
        if (string.IsNullOrWhiteSpace(customDecal))
        {
            rem = (EffectModuleReveal)Catalog.GetData<EffectData>("HitBladeDecalFlesh").modules[3];
        }
        else
        {
            rem = (EffectModuleReveal)Catalog.GetData<EffectData>(customDecal).modules[0];
        }

        var controllers = new List<RevealMaterialController>();
        foreach (var r in rp.renderers.Where(renderer => rem is not null && renderer.revealDecal && ((renderer.revealDecal.type == RevealDecal.Type.Default &&
                                                                                                       rem.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Default)) ||
                                                                                                      (renderer.revealDecal.type == RevealDecal.Type.Body &&
                                                                                                       rem.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Body)) ||
                                                                                                      (renderer.revealDecal.type == RevealDecal.Type.Outfit &&
                                                                                                       rem.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Outfit)))))
        {
            controllers.Add(r.revealDecal.revealMaterialController);
        }

        var rev = new GameObject().transform;
        rev.position = hit.Point;
        rev.rotation = hit.Normal == Vector3.zero ? Quaternion.LookRotation(Vector3.one * 0.0001f) : Quaternion.LookRotation(hit.Normal);
        GameManager.local.StartCoroutine(RevealMaskProjection.ProjectAsync(
            rev.position + rev.forward * rem.offsetDistance, -rev.forward, rev.up, rem.depth, rem.maxSize,
            rem.textureContainer.GetRandomTexture(), rem.maxChannelMultiplier, controllers, rem.revealData, null));
    }

    private static void BloodSplatter(Vector3 origin, Vector3 direction, float force, int projectileCount, int penetrationPower, bool penetratedArmor)
    {
        if (Settings.disableGore || Settings.disableBloodSpatters || penetrationPower < 2 || !penetratedArmor)
        {
            return;
        }

        var layer = LayerMask.GetMask("Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject");
        if (!Physics.Raycast(origin, direction, out var hit, force * projectileCount / 30, layer,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }
        var go = new GameObject("temp_" + Random.Range(0, 10000));
        go.transform.position = hit.point;
        go.transform.rotation = hit.normal == Vector3.zero ? Quaternion.LookRotation(Vector3.one * 0.0001f) : Quaternion.LookRotation(hit.normal);
        Util.RandomizeZRotation(go.transform);
        var ei = Catalog.GetData<EffectData>("DropBlood").Spawn(hit.point, go.transform.rotation, null, null, false);
        ei.SetIntensity(100f);

        var particle = (EffectDecal)ei.effects[0];
        particle.baseLifeTime = particle.baseLifeTime * 20f * Settings.bloodSplatterLifetimeMultiplier;
        particle.emissionLifeTime = particle.emissionLifeTime * 20 * Settings.bloodSplatterLifetimeMultiplier;
        particle.size = particle.size * force / 40 * (projectileCount * Settings.bloodSplatterSizeMultiplier);

        ei.Play();
    }

    public static void FireItem(Transform muzzle, ProjectileData data, Item item)
    {
        var fireDir = muzzle.forward;
        var firePoint = muzzle.position;
        var fireRotation = muzzle.rotation;
        Util.SpawnItem(data.projectileItemId, $"[Cartridge of {data.projectileItemId}]", thisSpawnedItem =>
        {
            item.StartCoroutine(FireItemCoroutine(thisSpawnedItem, item, firePoint, fireRotation, fireDir, data.muzzleVelocity));
            if (data.destroyTime != 0f)
            {
                thisSpawnedItem.Despawn(data.destroyTime);
            }
        }, firePoint, fireRotation);
    }

    private static IEnumerator FireItemCoroutine(Item projectileItem, Item gunItem, Vector3 pos, Quaternion rot, Vector3 dir, float velocity)
    {
        projectileItem.physicBody.rigidBody.isKinematic = true;
        Util.IgnoreCollision(projectileItem.gameObject, gunItem.gameObject, true);
        Util.IgnoreCollision(projectileItem.gameObject, Player.local.gameObject, true);
        projectileItem.transform.rotation = rot;
        projectileItem.transform.position = pos;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        projectileItem.physicBody.rigidBody.isKinematic = false;
        projectileItem.Throw();
        projectileItem.physicBody.rigidBody.velocity = dir * velocity;
    }

    public static IEnumerator TemporaryKnockout(float duration, float delay, Creature creature)
    {
        var handler = new GameObject($"TempKnockoutHandler_{Random.Range(0, 9999)}");
        yield return new WaitForSeconds(delay);

        creature.brain.AddNoStandUpModifier(handler);
        creature.ragdoll.SetState(Ragdoll.State.Inert);
        yield return new WaitForSeconds(duration);

        creature.brain.RemoveNoStandUpModifier(handler);
        Destroy(handler);
    }

    public static void HitscanExplosion(Vector3 point, ExplosiveData data, Item item, out List<Creature> hitCreatures, out List<Item> hitItems)
    {
        //PHYSICS TOGGLE
        foreach (var locomotionHit in Physics.OverlapSphere(point, data.radius, LayerMask.GetMask("BodyLocomotion")))
        {
            if (!locomotionHit.GetComponentInParent<Creature>())
            {
                continue;
            }
            var cr = locomotionHit.GetComponentInParent<Creature>();
            foreach (var part in cr.ragdoll.parts)
            {
                part.gameObject.SetActive(true);
            }
        }

        hitCreatures = [];
        hitItems = [];
        var hitShootables = new List<Shootable>();
        var hitSimpleBreakables = new List<SimpleBreakable>();
        var hitBreakables = new List<Breakable>();

        foreach (var c in Physics.OverlapSphere(point, data.radius))
        {
            if (c.GetComponentInParent<Ragdoll>() is { } hitRag && !hitCreatures.Contains(hitRag.creature))
            {
                hitCreatures.Add(hitRag.creature);
            }
            else if (c.GetComponentInParent<Item>() is { } hitItem && !hitItems.Contains(hitItem))
            {
                hitItems.Add(hitItem);
            }
            if (c.GetComponentInParent<Shootable>() is { } sb && !hitShootables.Contains(sb))
            {
                hitShootables.Add(sb);
            }
            if (c.GetComponentInParent<Breakable>() is { } br && !hitBreakables.Contains(br))
            {
                hitBreakables.Add(br);
            }
            if (c.GetComponentInParent<SimpleBreakable>() is { } sbr && !hitSimpleBreakables.Contains(sbr))
            {
                hitSimpleBreakables.Add(sbr);
            }
        }

        foreach (var hitCreature in hitCreatures)
        {
            if (!CheckExplosionCreatureHit(hitCreature, point))
            {
                continue;
            }
            var coll = new CollisionInstance(new DamageStruct(DamageType.Pierce, EvaluateDamage(data.damage, hitCreature)));
            coll.damageStruct.damage = EvaluateDamage(data.damage, hitCreature);
            coll.damageStruct.damageType = DamageType.Energy;
            coll.sourceMaterial = Catalog.GetData<MaterialData>("Blade");
            coll.targetMaterial = Catalog.GetData<MaterialData>("Flesh");
            coll.targetColliderGroup = hitCreature.ragdoll.parts[0].colliderGroup;
            coll.sourceColliderGroup = item.colliderGroups[0];

            coll.damageStruct.penetration = DamageStruct.Penetration.Hit;
            coll.damageStruct.penetrationDepth = 10;
            coll.damageStruct.hitRagdollPart = hitCreature.ragdoll.parts[0];
            coll.intensity = EvaluateDamage(data.damage, hitCreature);
            try { hitCreature.Damage(coll); }
            catch (Exception)
            {
                /*ignored*/
            }

            hitCreature.locomotion.physicBody.rigidBody.AddExplosionForce(data.force, point, data.radius, data.upwardsModifier);
            if (hitCreature.isKilled)
            {
                hitCreature.StartCoroutine(ExplodeCreature(point, data, hitCreature));
            }
        }

        foreach (var hitShootable in hitShootables) { hitShootable.Shoot(ProjectileData.PenetrationLevels.Kevlar); }

        foreach (var hitItem in hitItems) { hitItem.physicBody.rigidBody.AddExplosionForce(data.force, point, data.radius * 3, data.upwardsModifier); }

        foreach (var simpleBreakable in hitSimpleBreakables) { simpleBreakable.Break(); }

        foreach (var breakable in hitBreakables) { breakable.Break(); }

        if (!string.IsNullOrWhiteSpace(data.effectId))
        {
            var ei = Catalog.GetData<EffectData>(data.effectId).Spawn(point, Quaternion.Euler(0, 0, 0));
            ei.Play();
        }
    }

    private static IEnumerator ExplodeCreature(Vector3 point, ExplosiveData data, Creature hitCreature)
    {
        if (hitCreature.isPlayer || !Settings.explosionsDismember || Settings.disableGore)
        {
            yield break;
        }

        foreach (var rp in hitCreature.ragdoll.parts.ToArray().Reverse())
        {
            yield return new WaitForEndOfFrame();

            if (Vector3.Distance(rp.transform.position, point) < data.radius / 2 && Slice(rp))
            {
                rp.TrySlice();
                rp.physicBody.rigidBody.AddForce((rp.physicBody.rigidBody.position - point).normalized * data.force * 2);
            }
            else
            {
                rp.physicBody.rigidBody.AddForce((rp.physicBody.rigidBody.position - point).normalized * data.force * 10);
            }
        }
    }

    public static bool CheckExplosionCreatureHit(Creature c, Vector3 origin)
    {
        return c.ragdoll.parts.Any(b => !Physics.Raycast(b.transform.position, origin - b.transform.position, Vector3.Distance(b.transform.position, origin) - 0.1f, LayerMask.GetMask("Default")));
    }

    public static float EvaluateDamage(float perFifty, Creature c)
    {
        var perFiftyDamage = perFifty * Settings.firearmDamageMultiplier;
        var aspect = perFiftyDamage / 50;
        var damageToBeDone = Mathf.Clamp(c.maxHealth, 50f, 100f) * aspect;

        return damageToBeDone;
    }

    public static bool WouldCreatureBeKilled(float perFifty, Creature c)
    {
        var damage = EvaluateDamage(perFifty, c);
        return (Mathf.Approximately(damage, c.currentHealth) || damage > c.currentHealth) && (!c.isPlayer || !Player.invincibility);
    }

    public static int GetRequiredPenetrationLevel(HitData hit, Vector3 direction, Item handler)
    {
        var hitMaterialHash = -1;
        var colliderGroup = hit.Collider.GetComponentInParent<ColliderGroup>();

        if (colliderGroup)
        {
            handler.mainCollisionHandler.MeshRaycast(colliderGroup, hit.Point, hit.Normal, direction, ref hitMaterialHash);
        }
        if (hitMaterialHash == -1)
        {
            hitMaterialHash = Animator.StringToHash(hit.Collider.material.name);
        }
        TryGetMaterial(hitMaterialHash, out var matDat);
        return (int)RequiredPenetrationPowerData.GetRequiredLevel(matDat.id);
    }

    public static int GetRequiredPenetrationLevel(Collider collider)
    {
        if (!collider.material)
        {
            return 0;
        }

        var hitMaterialHash = Animator.StringToHash(collider.material.name);
        TryGetMaterial(hitMaterialHash, out var matDat);
        return (int)RequiredPenetrationPowerData.GetRequiredLevel(matDat.id);
    }

    public static bool TryGetMaterial(int targetPhysicMaterialHash, out MaterialData targetMaterial)
    {
        targetMaterial = null;
        var list = Catalog.GetDataList(Category.Material);
        var count = list.Count;
        for (var i = 0; i < count; i++)
        {
            var materialData = (MaterialData)list[i];

            if (materialData.physicMaterialHash == targetPhysicMaterialHash)
            {
                targetMaterial = materialData;
            }

            if (targetMaterial is not null)
            {
                return true;
            }
        }

        return false;
    }
}