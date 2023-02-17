using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ThunderRoad;
using RainyReignGames.RevealMask;
using System.Collections;
using GhettosFirearmSDKv2.Explosives;

namespace GhettosFirearmSDKv2
{
    public class FireMethods : MonoBehaviour
    {
        public static void Fire(Item gun, Transform muzzle, ProjectileData data, out List<Vector3> hitpoints, out List<Vector3> trajectories, float damageMultiplier)
        {
            if (data.isHitscan)
            {
                FireHitscan(muzzle, data, gun, out hitpoints, out trajectories, damageMultiplier);
            }
            else
            {
                FireItem(muzzle, data, gun);
                hitpoints = null;
                trajectories = null;
            }
        }

        public static void ApplyRecoil(Transform transform, Rigidbody rb, float force, float upwardsModifier, float firearmRecoilModifier, List<FirearmBase.RecoilModifier> modifiers)
        {
            float upMod = 1f;
            float linMod = 1f;

            if (modifiers != null)
            {
                foreach (FirearmBase.RecoilModifier mod in modifiers)
                {
                    upMod *= mod.muzzleRiseModifier;
                    linMod *= mod.modifier;
                }
            }

            rb.AddForce(-transform.forward * (force * linMod) * firearmRecoilModifier, ForceMode.Impulse);
            rb.AddRelativeTorque(Vector3.right * ((force * upMod) * upwardsModifier) * firearmRecoilModifier, ForceMode.Impulse);
        }

        public static List<Creature> FireHitscan(Transform muzzle, ProjectileData data, Item item, out List<Vector3> returnedHitpoints, out List<Vector3> returnedTrajectories, float damageMultiplier)
        {
            returnedHitpoints = new List<Vector3>();
            returnedTrajectories = new List<Vector3>();
            List<Creature> crs = new List<Creature>();
            for (int i = 0; i < data.projectileCount; i++)
            {
                Transform tempMuz = new GameObject().transform;
                tempMuz.parent = muzzle;
                tempMuz.localPosition = Vector3.zero;
                tempMuz.localEulerAngles = new Vector3(UnityEngine.Random.Range(-data.projectileSpread, data.projectileSpread), UnityEngine.Random.Range(-data.projectileSpread, data.projectileSpread), 0);
                Creature cr = Hitscan(tempMuz, data, item, out Vector3 hit, damageMultiplier);
                returnedHitpoints.Add(hit);
                returnedTrajectories.Add(tempMuz.forward);
                Destroy(tempMuz.gameObject);
                if (cr != null) crs.Add(cr);
            }
            return crs;
        }

        private static Creature Hitscan(Transform muzzle, ProjectileData data, Item gunItem, out Vector3 hitpoint, float damageMultiplier)
        {
            Settings_LevelModule.localScore.shotsFired++;

            #region physics toggle
            foreach (RaycastHit hit1 in Physics.RaycastAll(muzzle.position, muzzle.forward, Mathf.Infinity, LayerMask.GetMask("BodyLocomotion")))
            {
                if (hit1.collider.gameObject.GetComponentInParent<Creature>() is Creature cr)
                {
                    if (cr)
                    {
                        foreach (RagdollPart part in cr.ragdoll.parts)
                        {
                            part.gameObject.SetActive(true);
                        }
                        if (cr.equipment.GetHeldItem(Side.Left) is Item leftHeld)
                        {
                            leftHeld.SetPhysicsState(true);
                        }
                        if (cr.equipment.GetHeldItem(Side.Right) is Item rightHeld)
                        {
                            rightHeld.SetPhysicsState(true);
                        }
                    }
                }
            }
            #endregion physics toggle

            #region penetration level set
            int layer;
            if (data.penetrationPower >= ProjectileData.PenetrationLevels.Items)
            {
                layer = LayerMask.GetMask("NPC", "Ragdoll", "Default", "Avatar", "PlayerHandAndFoot");
            }
            else if (data.penetrationPower >= ProjectileData.PenetrationLevels.World)
            {
                layer = LayerMask.GetMask("NPC", "Ragdoll", "Avatar", "PlayerHandAndFoot");
            }
            else
            {
                layer = LayerMask.GetMask("NPC", "Ragdoll", "Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject", "Avatar", "PlayerHandAndFoot");
                //layer = LayerMask.NameToLayer("Default");
            }
            #endregion penetration level set

            if (Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit hit, data.projectileRange, layer))
            {
                if (data.isExplosive)
                {
                    HitscanExplosion(hit.point, data.explosiveData, gunItem, out List<Creature> hitCrs, out List<Item> hitItems);
                    if (data.explosiveEffect != null)
                    {
                        data.explosiveEffect.gameObject.transform.SetParent(null);
                        data.explosiveEffect.transform.position = hit.point;
                        Player.local.StartCoroutine(Explosive.delayedDestroy(data.explosiveEffect.gameObject, data.explosiveEffect.main.duration + 9f));
                        data.explosiveEffect.Play();

                        AudioSource audio = Util.GetRandomFromList(data.explosiveSoundEffects);
                        audio.gameObject.transform.SetParent(null);
                        audio.transform.position = hit.point;
                        audio.Play();
                        Player.local.StartCoroutine(Explosive.delayedDestroy(audio.gameObject, audio.clip.length + 1f));
                    }
                }

                hitpoint = hit.point;
                if (hit.rigidbody != null)
                {
                    if (hit.rigidbody.gameObject.TryGetComponent(out Shootable sb))
                    {
                        sb.Shoot(data.penetrationPower);
                    }
                    #region creature hit
                    if (hit.collider.gameObject.GetComponentInParent<Ragdoll>() is Ragdoll rag)
                    {
                        Creature cr = rag.creature;
                        RagdollPart ragdollPart = hit.collider.gameObject.GetComponentInParent<RagdollPart>();
                        Settings_LevelModule.localScore.shotsHit++;

                        bool penetrated = GetRequiredPenetrationLevel(hit, muzzle.forward, gunItem) <= data.penetrationPower;
                        if (data.hasBodyImpactEffect)
                        {
                            //Effect
                            EffectInstance ei = Catalog.GetData<EffectData>(penetrated ? "BulletImpactFlesh_Ghetto05_FirearmSDKv2" : "BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal), hit.collider.transform);
                            ei.SetIntensity(100f);
                            ei.Play();
                        }

                        if (data.drawsImpactDecal && penetrated) DrawDecal(ragdollPart, hit, data.customImpactDecalId);

                        #region Damage level determination
                        float damageModifier = 1;
                        switch (ragdollPart.type)
                        {
                            case RagdollPart.Type.Head: //damage = infinity, remove voice, push(3)
                                {
                                    Settings_LevelModule.localScore.headshots++;
                                    if (penetrated && data.lethalHeadshot) damageModifier = Mathf.Infinity;
                                    else damageModifier = 2;
                                    if (penetrated && WouldCreatureBeKilled(data.damagePerProjectile * damageModifier, cr) && !cr.isPlayer) cr.brain.instance.GetModule<BrainModuleSpeak>().Unload();
                                    if (penetrated && data.slicesBodyParts) cr.ragdoll.GetPart(RagdollPart.Type.Head).TrySlice();
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 3);
                                }
                                break;
                            case RagdollPart.Type.Neck: //damage = infinity, push(1)
                                {
                                    if (penetrated && data.lethalHeadshot) damageModifier = Mathf.Infinity;
                                    else damageModifier = 2;
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
                                }
                                break;
                            case RagdollPart.Type.Torso: //damage = damage, push(2)
                                {
                                    if (penetrated && Settings_LevelModule.local.incapitateOnTorsoShot && data.enoughToIncapitate && !cr.isKilled && !cr.isPlayer)
                                    {
                                        cr.brain.AddNoStandUpModifier(gunItem);
                                        cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                                    }
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 2);
                                }
                                break;
                            case RagdollPart.Type.LeftArm: //damage = damage/3, release weapon, push(1)
                                {
                                    damageModifier = 1 / 3;
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
                                    if (!cr.isKilled && !cr.isPlayer) cr.handRight.TryRelease();
                                }
                                break;
                            case RagdollPart.Type.RightArm: //damage = damage/3, release weapon, push(1)
                                {
                                    damageModifier = 1 / 3;
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
                                    if (!cr.isKilled && !cr.isPlayer) cr.handRight.TryRelease();
                                }
                                break;
                            case RagdollPart.Type.LeftFoot: //damage = damage/4, destabilize, push(3)
                                {
                                    damageModifier = 0.25f;
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
                                    if (!cr.isKilled && !cr.isPlayer) cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                                }
                                break;
                            case RagdollPart.Type.RightFoot: //damage = damage/4, destabilize, push(3)
                                {
                                    damageModifier = 0.25f;
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
                                    if (!cr.isKilled && !cr.isPlayer) cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                                }
                                break;
                            case RagdollPart.Type.LeftHand: //damage = damage/4, release weapon, push(1)
                                {
                                    damageModifier = 0.25f;
                                    if (!cr.isKilled && !cr.isPlayer) cr.handLeft.TryRelease();
                                }
                                break;
                            case RagdollPart.Type.RightHand: //damage = damage/4, release weapon, push(1)
                                {
                                    damageModifier = 0.25f;
                                    if (!cr.isKilled && !cr.isPlayer) cr.handRight.TryRelease();
                                }
                                break;
                            case RagdollPart.Type.LeftLeg: //damage = damage/3, destabilize, push(3)
                                {
                                    damageModifier = 1 / 3;
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
                                    if (!cr.isKilled && !cr.isPlayer && damageModifier < cr.currentHealth)
                                        cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                                }
                                break;
                            case RagdollPart.Type.RightLeg: //damage = damage/3, destabilize, push(3)
                                {
                                    damageModifier = 1 / 3;
                                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
                                    if (!cr.isKilled && !cr.isPlayer && damageModifier < cr.currentHealth)
                                        cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                                }
                                break;
                        }
                        if (penetrated && data.slicesBodyParts && !cr.isPlayer && ragdollPart.sliceAllowed) ragdollPart.TrySlice();
                        if (!penetrated) damageModifier /= 4;
                        #endregion Damage level determination

                        #region Damaging
                        CollisionInstance coll = new CollisionInstance(new DamageStruct(DamageType.Pierce, data.damagePerProjectile));
                        coll.damageStruct.damage = EvaluateDamage(data.damagePerProjectile * damageModifier, cr);
                        coll.damageStruct.damageType = DamageType.Pierce;
                        coll.sourceMaterial = Catalog.GetData<MaterialData>("Blade");
                        coll.targetMaterial = Catalog.GetData<MaterialData>("Flesh");
                        coll.targetColliderGroup = ragdollPart.colliderGroup;
                        coll.sourceColliderGroup = gunItem.colliderGroups[0];
                        coll.contactPoint = hit.point;
                        coll.contactNormal = hit.normal;
                        coll.impactVelocity = muzzle.forward * 200;

                        Transform penPoint = new GameObject().transform;
                        penPoint.position = hit.point;
                        penPoint.rotation = Quaternion.LookRotation(hit.normal);
                        penPoint.parent = hit.transform;
                        coll.damageStruct.penetration = DamageStruct.Penetration.Hit;
                        coll.damageStruct.penetrationPoint = penPoint;
                        coll.damageStruct.penetrationDepth = 10;
                        coll.damageStruct.hitRagdollPart = ragdollPart;
                        coll.intensity = EvaluateDamage(data.damagePerProjectile * damageModifier * damageMultiplier, cr);
                        coll.pressureRelativeVelocity = muzzle.forward * 200;

                        cr.Damage(coll);
                        #endregion Damaging

                        #region Additional Effects
                        //Taser
                        if (data.isElectrifying) cr.TryElectrocute(data.tasingForce, data.tasingDuration, true, false, Catalog.GetData<EffectData>("ImbueLightningRagdoll"));

                        //Force knockout
                        if (data.forceDestabilize && !cr.isPlayer && !cr.isKilled) cr.ragdoll.SetState(Ragdoll.State.Destabilized);

                        //RB Push
                        if (cr.isKilled && !cr.isPlayer && hit.rigidbody != null)
                        {
                            hit.rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
                            //cr.locomotion.rb.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
                        }

                        //Stun
                        else if (!cr.isPlayer)
                        {
                            if (data.forceIncapitate ) cr.brain.AddNoStandUpModifier(gunItem);
                            else if (data.knocksOutTemporarily)
                            {
                                GameObject handler = new GameObject($"TempKnockoutHandler_{Random.Range(0, 9999)}");
                                gunItem.StartCoroutine(TemporaryKnockout(data.temporaryKnockoutTime, cr, handler));
                            }
                        }

                        BrainModuleHitReaction hitReaction = cr.brain.instance.GetModule<BrainModuleHitReaction>();
                        hitReaction.SetStagger(hitReaction.staggerMedium);
                        #endregion Additional Effects

                        return cr;
                    }
                    #endregion creature hit

                    #region non-creature hit
                    else
                    {
                        if (data.hasImpactEffect)
                        {
                            EffectInstance ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal), hit.collider.transform);
                            ei.SetIntensity(100f);
                            ei.Play();
                        }
                        if (hit.collider.GetComponentInParent<Item>() is Item hitItem && hitItem.handlers.Count > 0)
                        {
                            hit.rigidbody.AddForce(muzzle.forward * (data.forcePerProjectile / 10), ForceMode.Impulse);
                        }
                        else
                        {
                            hit.rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
                        }
                    }
                    #endregion non-creature hit
                }
                else
                {
                    if (data.hasImpactEffect)
                    {
                        EffectInstance ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal), hit.collider.transform);
                        ei.SetIntensity(100f);
                        ei.Play();
                    }
                }
            }
            else hitpoint = Vector3.zero;
            return null;
        }

        private static void DrawDecal(RagdollPart rp, RaycastHit hit, string customDecal)
        {
            EffectModuleReveal rem = null;
            if (string.IsNullOrWhiteSpace(customDecal))
            {
                rem = (EffectModuleReveal)Catalog.GetData<EffectData>("HitBladeDecalFlesh").modules[3];
            }
            else
            {
                rem = (EffectModuleReveal)Catalog.GetData<EffectData>(customDecal).modules[0];
            }
            List<RevealMaterialController> rmcs = new List<RevealMaterialController>();
            foreach (Creature.RendererData r in rp.renderers.Where(renderer => rem != null && renderer.revealDecal && (renderer.revealDecal.type == RevealDecal.Type.Default &&
                    rem.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Default) ||
                    renderer.revealDecal.type == RevealDecal.Type.Body &&
                    rem.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Body) ||
                    renderer.revealDecal.type == RevealDecal.Type.Outfit &&
                    rem.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Outfit))))
            {
                rmcs.Add(r.revealDecal.revealMaterialController);
            }
            Transform rev = new GameObject().transform;
            rev.position = hit.point;
            rev.rotation = Quaternion.LookRotation(hit.normal);
            GameManager.local.StartCoroutine(RevealMaskProjection.ProjectAsync(rev.position + rev.forward * rem.offsetDistance, -rev.forward, rev.up, rem.depth, rem.maxSize, rem.maskTexture, rem.maxChannelMultiplier, rmcs, rem.revealData, null));
        }

        public static void FireItem(Transform muzzle, ProjectileData data, Item item)
        {
            Vector3 fireDir = muzzle.forward;
            Vector3 firePoint = muzzle.position;
            Quaternion fireRotation = muzzle.rotation;
            Catalog.GetData<ItemData>(data.projectileItemId, true).SpawnAsync(thisSpawnedItem =>
            {
                item.StartCoroutine(FireItemCoroutine(thisSpawnedItem, item, firePoint, fireRotation, fireDir, data.muzzleVelocity));
            }, muzzle.position, muzzle.rotation);
        }

        private static IEnumerator FireItemCoroutine(Item projectilItem, Item gunItem, Vector3 pos, Quaternion rot, Vector3 dir, float velocity)
        {
            projectilItem.rb.isKinematic = true;
            Util.IgnoreCollision(projectilItem.gameObject, gunItem.gameObject, true);
            Util.IgnoreCollision(projectilItem.gameObject, Player.local.gameObject, true);
            projectilItem.transform.rotation = rot;
            projectilItem.transform.position = pos;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            projectilItem.rb.isKinematic = false;
            projectilItem.Throw();
            projectilItem.rb.velocity = dir * velocity;
        }

        private static IEnumerator TemporaryKnockout(float duration, Creature creature, GameObject handler)
        {
            creature.brain.AddNoStandUpModifier(handler);
            yield return new WaitForSeconds(duration);
            creature.brain.RemoveNoStandUpModifier(handler);
            Destroy(handler);
        }

        public static void HitscanExplosion(Vector3 point, ExplosiveData data, Item item, out List<Creature> hitCreatures, out List<Item> hitItems)
        {
            //PHYSICS TOGGLE
            Collider[] locomotionHits = Physics.OverlapSphere(point, data.radius, LayerMask.GetMask("BodyLocomotion"));
            foreach (Collider locomotionHit in locomotionHits)
            {
                if (locomotionHit.GetComponentInParent<Creature>() != null)
                {
                    Creature cr = locomotionHit.GetComponentInParent<Creature>();
                    foreach (RagdollPart part in cr.ragdoll.parts)
                    {
                        part.gameObject.SetActive(true);
                    }
                }
            }

            hitCreatures = new List<Creature>();
            hitItems = new List<Item>();
            List<Shootable> hitShootables = new List<Shootable>();

            foreach (Collider c in Physics.OverlapSphere(point, data.radius))
            {
                if (c.GetComponentInParent<Ragdoll>() is Ragdoll hitRag && !hitCreatures.Contains(hitRag.creature))
                {
                    hitCreatures.Add(hitRag.creature);
                }
                else if (c.GetComponentInParent<Item>() is Item hitItem && !hitItems.Contains(hitItem))
                {
                    hitItems.Add(hitItem);
                }
                if (c.GetComponentInParent<Shootable>() is Shootable sb && !hitShootables.Contains(sb))
                {
                    hitShootables.Add(sb);
                }
            }

            foreach (Creature hitCreature in hitCreatures)
            {
                CollisionInstance coll = new CollisionInstance(new DamageStruct(DamageType.Pierce, data.damage * Settings_LevelModule.local.damageMultiplier));
                coll.damageStruct.damage = EvaluateDamage(data.damage, hitCreature);
                coll.damageStruct.damageType = DamageType.Pierce;
                coll.sourceMaterial = Catalog.GetData<MaterialData>("Blade");
                coll.targetMaterial = Catalog.GetData<MaterialData>("Flesh");
                coll.targetColliderGroup = hitCreature.ragdoll.parts[0].colliderGroup;
                coll.sourceColliderGroup = item.colliderGroups[0];

                coll.damageStruct.penetration = DamageStruct.Penetration.Hit;
                coll.damageStruct.penetrationDepth = 10;
                coll.damageStruct.hitRagdollPart = hitCreature.ragdoll.parts[0];
                coll.intensity = data.damage * Settings_LevelModule.local.damageMultiplier;
                hitCreature.Damage(coll);

                hitCreature.locomotion.rb.AddExplosionForce(data.force, point, data.radius, data.upwardsModifier);
                foreach (RagdollPart rp in hitCreature.ragdoll.parts)
                {
                    rp.rb.AddForce((rp.rb.position - point).normalized * data.force);
                }
            }

            foreach (Shootable hitShootable in hitShootables)
            {
                hitShootable.Shoot(ProjectileData.PenetrationLevels.Kevlar);
            }

            foreach (Item hitItem in hitItems)
            {
                hitItem.rb.AddExplosionForce(data.force * 200, point, data.radius*3, data.upwardsModifier);
            }

            if (!string.IsNullOrWhiteSpace(data.effectId))
            {
                EffectInstance ei = Catalog.GetData<EffectData>(data.effectId).Spawn(point, Quaternion.Euler(0, 0, 0));
                ei.Play();
            }
        }

        public static float EvaluateDamage(float perfifty, Creature c)
        {
            float perFiftyDamage = perfifty * Settings_LevelModule.local.damageMultiplier;
            float aspect = perFiftyDamage / 50;
            float damageToBeDone = c.maxHealth * aspect;

            return damageToBeDone;
        }

        public static bool WouldCreatureBeKilled(float perfifty, Creature c)
        {
            return EvaluateDamage(perfifty, c) >= c.currentHealth;
        }

        public static ProjectileData.PenetrationLevels GetRequiredPenetrationLevel(RaycastHit hit, Vector3 direction, Item handler)
        {
            int hitMaterialHash = -1;
            ColliderGroup colliderGroup = hit.collider.GetComponentInParent<ColliderGroup>();

            handler.mainCollisionHandler.MeshRaycast(colliderGroup, hit.point, hit.normal, direction, ref hitMaterialHash);
            if (hitMaterialHash == -1) hitMaterialHash = Animator.StringToHash(hit.collider.material.name);
            TryGetMaterial(hitMaterialHash, out MaterialData matDat);
            return RequiredPenetrationPowerData.GetRequiredLevel(matDat.id);
        }

        public static bool TryGetMaterial(int targetPhysicMaterialHash, out MaterialData targetMaterial)
        {
            targetMaterial = null;
            List<CatalogData> list = Catalog.GetDataList(Catalog.Category.Material);
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                MaterialData materialData = (MaterialData)list[i];

                if (materialData.physicMaterialHash == targetPhysicMaterialHash)
                {
                    targetMaterial = materialData;
                }
                if (targetMaterial != null) return true;
            }
            return false;
        }
    }
}
