using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ThunderRoad;
using ThunderRoad.Reveal;
using System.Collections;
using GhettosFirearmSDKv2.Explosives;
using System;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2
{
    public class FireMethods : MonoBehaviour
    {
        public static void Fire(Item gun, Transform muzzle, ProjectileData data, out List<Vector3> hitpoints, out List<Vector3> trajectories, out List<Creature> hitCreatures, float damageMultiplier, bool useAISpread)
        {
            hitpoints = new List<Vector3>();
            trajectories = new List<Vector3>();
            hitCreatures = new List<Creature>();
            try
            {
                if (data.isHitscan)
                {
                    hitCreatures = FireHitscanV2(muzzle, data, gun, out hitpoints, out trajectories, damageMultiplier, useAISpread);
                }
                else
                {
                    FireItem(muzzle, data, gun);
                }
            }
            catch (Exception)
            { }
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

        // public static List<Creature> FireHitscan(Transform muzzle, ProjectileData data, Item item, out List<Vector3> returnedHitpoints, out List<Vector3> returnedTrajectories, float damageMultiplier)
        // {
        //     returnedHitpoints = new List<Vector3>();
        //     returnedTrajectories = new List<Vector3>();
        //     List<Creature> crs = new List<Creature>();
        //     for (int i = 0; i < data.projectileCount; i++)
        //     {
        //         Transform tempMuz = new GameObject().transform;
        //         tempMuz.parent = muzzle;
        //         tempMuz.localPosition = Vector3.zero;
        //         tempMuz.localEulerAngles = new Vector3(Random.Range(-data.projectileSpread, data.projectileSpread), Random.Range(-data.projectileSpread, data.projectileSpread), 0);
        //         Creature cr = Hitscan(tempMuz, data, item, out Vector3 hit, damageMultiplier);
        //         returnedHitpoints.Add(hit);
        //         returnedTrajectories.Add(tempMuz.forward);
        //         Destroy(tempMuz.gameObject);
        //         if (cr != null) crs.Add(cr);
        //     }
        //     return crs;
        // }

        // private static Creature Hitscan(Transform muzzle, ProjectileData data, Item gunItem, out Vector3 hitpoint, float damageMultiplier)
        // {
        //     FirearmsScore.local.shotsFired++;
        //
        //     #region physics toggle
        //     foreach (RaycastHit hit1 in Physics.RaycastAll(muzzle.position, muzzle.forward, Mathf.Infinity, LayerMask.GetMask("BodyLocomotion")))
        //     {
        //         if (hit1.collider.gameObject.GetComponentInParent<Creature>() is Creature cr)
        //         {
        //             if (cr)
        //             {
        //                 foreach (RagdollPart part in cr.ragdoll.parts)
        //                 {
        //                     part.gameObject.SetActive(true);
        //                 }
        //                 if (cr.equipment != null)
        //                 {
        //                     if (cr.equipment.GetHeldItem(Side.Left) != null)
        //                     {
        //                         cr.equipment.GetHeldItem(Side.Left).SetColliders(true);
        //                     }
        //                     if (cr.equipment.GetHeldItem(Side.Right) != null)
        //                     {
        //                         cr.equipment.GetHeldItem(Side.Right).SetColliders(true);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //     #endregion physics toggle
        //
        //     #region penetration level set
        //     int layer;
        //     if (data.penetrationPower >= ProjectileData.PenetrationLevels.Items)
        //     {
        //         layer = LayerMask.GetMask("NPC", "Ragdoll", "Default", "Avatar", "PlayerHandAndFoot");
        //     }
        //     else if (data.penetrationPower >= ProjectileData.PenetrationLevels.World)
        //     {
        //         layer = LayerMask.GetMask("NPC", "Ragdoll", "Avatar", "PlayerHandAndFoot");
        //     }
        //     else
        //     {
        //         layer = LayerMask.GetMask("NPC", "Ragdoll", "Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject", "Avatar", "PlayerHandAndFoot");
        //         //layer = LayerMask.NameToLayer("Default");
        //     }
        //     #endregion penetration level set
        //
        //     if (Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit hit, data.projectileRange, layer))
        //     {
        //         #region explosive
        //         if (data.isExplosive)
        //         {
        //             HitscanExplosion(hit.point, data.explosiveData, gunItem, out List<Creature> hitCrs, out List<Item> hitItems);
        //             if (data.explosiveEffect != null)
        //             {
        //                 data.explosiveEffect.gameObject.transform.SetParent(null);
        //                 data.explosiveEffect.transform.position = hit.point;
        //                 Player.local.StartCoroutine(Explosive.delayedDestroy(data.explosiveEffect.gameObject, data.explosiveEffect.main.duration + 9f));
        //                 data.explosiveEffect.Play();
        //
        //                 AudioSource audio = Util.GetRandomFromList(data.explosiveSoundEffects);
        //                 audio.gameObject.transform.SetParent(null);
        //                 audio.transform.position = hit.point;
        //                 audio.Play();
        //                 Player.local.StartCoroutine(Explosive.delayedDestroy(audio.gameObject, audio.clip.length + 1f));
        //             }
        //         }
        //         #endregion explosive
        //
        //         hitpoint = hit.point;
        //         if (hit.rigidbody != null)
        //         {
        //             #region shootables
        //             if (hit.rigidbody.gameObject.TryGetComponent(out Shootable sb))
        //             {
        //                 sb.Shoot(data.penetrationPower);
        //             }
        //             #endregion shootables
        //
        //             #region creature hit
        //             if (hit.collider.gameObject.GetComponentInParent<Ragdoll>() is Ragdoll rag)
        //             {
        //                 Creature cr = rag.creature;
        //                 RagdollPart ragdollPart = hit.collider.gameObject.GetComponentInParent<RagdollPart>();
        //                 FirearmsScore.local.shotsHit++;
        //
        //                 bool penetrated = GetRequiredPenetrationLevel(hit, muzzle.forward, gunItem) <= (int)data.penetrationPower;
        //
        //                 #region impact effect
        //                 if (data.hasBodyImpactEffect)
        //                 {
        //                     //Effect
        //                     EffectInstance ei = Catalog.GetData<EffectData>(penetrated ? "BulletImpactFlesh_Ghetto05_FirearmSDKv2" : "BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal), hit.collider.transform);
        //                     ei.SetIntensity(100f);
        //                     ei.Play();
        //                 }
        //                 if (data.drawsImpactDecal && penetrated) DrawDecal(ragdollPart, hit, data.customImpactDecalId);
        //                 #endregion impact effect
        //
        //                 #region Damage level determination
        //                 float damageModifier = 1;
        //                 switch (ragdollPart.type)
        //                 {
        //                     case RagdollPart.Type.Head: //damage = infinity, remove voice, push(3)
        //                         {
        //                             FirearmsScore.local.headshots++;
        //                             if (penetrated && data.lethalHeadshot) damageModifier = Mathf.Infinity;
        //                             else damageModifier = 2;
        //                             if (penetrated && WouldCreatureBeKilled(data.damagePerProjectile * damageModifier, cr) && !cr.isPlayer) cr.brain.instance.GetModule<BrainModuleSpeak>().Unload();
        //                             if (penetrated && data.slicesBodyParts) cr.ragdoll.GetPart(RagdollPart.Type.Head).TrySlice();
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 3);
        //                         }
        //                         break;
        //                     case RagdollPart.Type.Neck: //damage = infinity, push(1)
        //                         {
        //                             if (penetrated && data.lethalHeadshot) damageModifier = Mathf.Infinity;
        //                             else damageModifier = 2;
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
        //                         }
        //                         break;
        //                     case RagdollPart.Type.Torso: //damage = damage, push(2)
        //                         {
        //                             if (penetrated && FirearmsSettings.incapitateOnTorsoShot > 0 && data.enoughToIncapitate && !cr.isKilled && !cr.isPlayer)
        //                             {
        //                                 gunItem.StartCoroutine(TemporaryKnockout(FirearmsSettings.incapitateOnTorsoShot, 0, cr));
        //                             }
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 2);
        //                         }
        //                         break;
        //                     case RagdollPart.Type.LeftArm: //damage = damage/3, release weapon, push(1)
        //                         {
        //                             damageModifier = 0.3f;
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
        //                             if (!cr.isKilled && !cr.isPlayer) cr.handRight.TryRelease();
        //                         }
        //                         break;
        //                     case RagdollPart.Type.RightArm: //damage = damage/3, release weapon, push(1)
        //                         {
        //                             damageModifier = 0.3f;
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
        //                             if (!cr.isKilled && !cr.isPlayer) cr.handRight.TryRelease();
        //                         }
        //                         break;
        //                     case RagdollPart.Type.LeftFoot: //damage = damage/4, destabilize, push(3)
        //                         {
        //                             damageModifier = 0.25f;
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
        //                             if (!cr.isKilled && !cr.isPlayer) cr.ragdoll.SetState(Ragdoll.State.Destabilized);
        //                         }
        //                         break;
        //                     case RagdollPart.Type.RightFoot: //damage = damage/4, destabilize, push(3)
        //                         {
        //                             damageModifier = 0.25f;
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
        //                             if (!cr.isKilled && !cr.isPlayer) cr.ragdoll.SetState(Ragdoll.State.Destabilized);
        //                         }
        //                         break;
        //                     case RagdollPart.Type.LeftHand: //damage = damage/4, release weapon, push(1)
        //                         {
        //                             damageModifier = 0.25f;
        //                             if (!cr.isKilled && !cr.isPlayer) cr.handLeft.TryRelease();
        //                         }
        //                         break;
        //                     case RagdollPart.Type.RightHand: //damage = damage/4, release weapon, push(1)
        //                         {
        //                             damageModifier = 0.25f;
        //                             if (!cr.isKilled && !cr.isPlayer) cr.handRight.TryRelease();
        //                         }
        //                         break;
        //                     case RagdollPart.Type.LeftLeg: //damage = damage/3, destabilize, push(3)
        //                         {
        //                             damageModifier = 0.3f;
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
        //                             if (!cr.isKilled && !cr.isPlayer && damageModifier < cr.currentHealth)
        //                                 cr.ragdoll.SetState(Ragdoll.State.Destabilized);
        //                         }
        //                         break;
        //                     case RagdollPart.Type.RightLeg: //damage = damage/3, destabilize, push(3)
        //                         {
        //                             damageModifier = 0.3f;
        //                             cr.TryPush(Creature.PushType.Hit, muzzle.forward, 1);
        //                             if (!cr.isKilled && !cr.isPlayer && damageModifier < cr.currentHealth)
        //                                 cr.ragdoll.SetState(Ragdoll.State.Destabilized);
        //                         }
        //                         break;
        //                 }
        //                 if (penetrated && data.slicesBodyParts && !cr.isPlayer && Slice(ragdollPart)) ragdollPart.TrySlice();
        //                 if (!penetrated) damageModifier /= 4;
        //                 #endregion Damage level determination
        //
        //                 #region Damaging
        //                 CollisionInstance coll = new CollisionInstance(new DamageStruct(FirearmsSettings.bulletsAreBlunt ? DamageType.Blunt : DamageType.Pierce, data.damagePerProjectile));
        //                 coll.damageStruct.damage = EvaluateDamage(data.damagePerProjectile * damageModifier, cr);
        //                 coll.damageStruct.damageType = FirearmsSettings.bulletsAreBlunt? DamageType.Blunt : DamageType.Pierce;
        //                 coll.sourceMaterial = Catalog.GetData<MaterialData>("Blade");
        //                 coll.targetMaterial = Catalog.GetData<MaterialData>("Flesh");
        //                 coll.targetColliderGroup = ragdollPart.colliderGroup;
        //                 coll.sourceColliderGroup = gunItem.colliderGroups[0];
        //                 coll.contactPoint = hit.point;
        //                 coll.contactNormal = hit.normal;
        //                 coll.impactVelocity = muzzle.forward * 200;
        //
        //                 Transform penPoint = new GameObject().transform;
        //                 penPoint.position = hit.point;
        //                 penPoint.rotation = Quaternion.LookRotation(hit.normal);
        //                 penPoint.parent = hit.transform;
        //                 coll.damageStruct.penetration = DamageStruct.Penetration.Hit;
        //                 coll.damageStruct.penetrationPoint = penPoint;
        //                 coll.damageStruct.penetrationDepth = 10;
        //                 coll.damageStruct.hitRagdollPart = ragdollPart;
        //                 coll.intensity = EvaluateDamage(data.damagePerProjectile * damageModifier * damageMultiplier, cr);
        //                 coll.pressureRelativeVelocity = muzzle.forward * 200;
        //
        //                 try { cr.Damage(coll); } catch (Exception) {}
        //                 #endregion Damaging
        //
        //                 #region Additional Effects
        //                 //Taser
        //                 if (data.isElectrifying) cr.TryElectrocute(data.tasingForce, data.tasingDuration, true, false, Catalog.GetData<EffectData>("ImbueLightningRagdoll"));
        //
        //                 //Force knockout
        //                 if ((data.forceDestabilize || data.isElectrifying) && !cr.isPlayer && !cr.isKilled) cr.ragdoll.SetState(Ragdoll.State.Destabilized);
        //
        //                 //RB Push
        //                 if (cr.isKilled && !cr.isPlayer && hit.rigidbody != null)
        //                 {
        //                     hit.rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
        //                     //cr.locomotion.rb.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
        //                 }
        //
        //                 //Stun
        //                 else if (!cr.isPlayer)
        //                 {
        //                     if (data.forceIncapitate ) cr.brain.AddNoStandUpModifier(gunItem);
        //                     else if (data.knocksOutTemporarily)
        //                     {
        //                         gunItem.StartCoroutine(TemporaryKnockout(data.temporaryKnockoutTime, data.kockoutDelay, cr));
        //                     }
        //                 }
        //
        //                 BrainModuleHitReaction hitReaction = cr.brain.instance.GetModule<BrainModuleHitReaction>();
        //                 hitReaction.SetStagger(hitReaction.staggerMedium);
        //                 #endregion Additional Effects
        //
        //                 return cr;
        //             }
        //             #endregion creature hit
        //
        //             #region non-creature hit
        //             else
        //             {
        //                 if (data.hasImpactEffect)
        //                 {
        //                     EffectInstance ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal), hit.collider.transform);
        //                     ei.SetIntensity(100f);
        //                     ei.Play();
        //                 }
        //                 if (hit.collider.GetComponentInParent<Item>() is Item hitItem)
        //                 {
        //                     hit.rigidbody.AddForce(muzzle.forward * (data.forcePerProjectile / 10), ForceMode.Impulse);
        //                     if (hitItem.GetComponentInChildren<Breakable>() is Breakable b) b.Break();
        //                 }
        //                 else
        //                 {
        //                     try { hit.rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse); } catch (Exception) { }
        //                 }
        //             }
        //             #endregion non-creature hit
        //         }
        //         else
        //         {
        //             if (data.hasImpactEffect)
        //             {
        //                 EffectInstance ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal), hit.collider.transform);
        //                 ei.SetIntensity(100f);
        //                 ei.Play();
        //             }
        //         }
        //     }
        //     else hitpoint = Vector3.zero;
        //     return null;
        // }

        public static List<Creature> FireHitscanV2(Transform muzzle, ProjectileData data, Item item, out List<Vector3> returnedEndpoints, out List<Vector3> returnedTrajectories, float damageMultiplier, bool useAISpread)
        {
            returnedEndpoints = new List<Vector3>();
            returnedTrajectories = new List<Vector3>();
            List<Creature> crs = new List<Creature>();
            try
            {
                for (int i = 0; i < data.projectileCount; i++)
                {
                    Transform tempMuz = new GameObject().transform;
                    tempMuz.parent = muzzle;
                    tempMuz.localPosition = Vector3.zero;
                    if (!useAISpread || data.projectileCount > 1)
                        tempMuz.localEulerAngles = new Vector3(Random.Range(-data.projectileSpread, data.projectileSpread), Random.Range(-data.projectileSpread, data.projectileSpread), 0);
                    else
                        tempMuz.localEulerAngles = new Vector3(Random.Range(-FirearmsSettings.aiFirearmSpread, FirearmsSettings.aiFirearmSpread), Random.Range(-FirearmsSettings.aiFirearmSpread, FirearmsSettings.aiFirearmSpread), 0);
                    List<Creature> cr = HitscanV2(tempMuz, data, item, out Vector3 endpoint, damageMultiplier);
                    returnedEndpoints.Add(endpoint);
                    returnedTrajectories.Add(tempMuz.forward);
                    Destroy(tempMuz.gameObject);
                    crs.AddRange(cr);
                }
            }
            catch (Exception)
            {
            }
            return crs;
        }

        private static List<Creature> HitscanV2(Transform muzzle, ProjectileData data, Item gunItem, out Vector3 endpoint, float damageMultiplier)
        {
            FirearmsScore.local.shotsFired++;

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
                        if (cr.equipment != null)
                        {
                            if (cr.equipment.GetHeldItem(Side.Left) != null)
                            {
                                cr.equipment.GetHeldItem(Side.Left).SetColliders(true);
                            }
                            if (cr.equipment.GetHeldItem(Side.Right) != null)
                            {
                                cr.equipment.GetHeldItem(Side.Right).SetColliders(true);
                            }
                        }
                    }
                }
            }
            #endregion physics toggle

            List<Creature> hitCreatures = new List<Creature>();
            List<Item> hitItems = new List<Item>();
            int layer = LayerMask.GetMask("NPC", "Ragdoll", "Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject", "Avatar", "PlayerHandAndFoot");

            List<RaycastHit> hits = Physics.RaycastAll(muzzle.position, muzzle.forward, data.projectileRange, layer).ToList();
            hits = hits.OrderBy(h => Vector3.Distance(h.point, muzzle.position)).ToList();
            int power = (int)data.penetrationPower;

            #region no hits
            if (hits.Count == 0)
            {
                endpoint = Vector3.zero;
                return hitCreatures;
            }
            #endregion no hits

            List<RaycastHit> successfullHits = new List<RaycastHit>();

            #region explosive
            if (data.isExplosive)
            {
                RaycastHit hit = hits[0];
                HitscanExplosion(hit.point, data.explosiveData, gunItem, out List<Creature> hitCrs, out List<Item> hitItms);
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
            #endregion explosive

            bool processing = true;
            foreach (RaycastHit hit in hits)
            {
                if (processing)
                {
                    try
                    {
                        Creature c = ProcessHit(muzzle, hit, successfullHits, data, damageMultiplier, hitCreatures, gunItem, out bool lowerDamageLevel, out bool cancel, ref power);
                        if (lowerDamageLevel)
                        {
                            if (power == (int)ProjectileData.PenetrationLevels.None || power == (int)ProjectileData.PenetrationLevels.Leather) processing = false;
                            else power -= 2;
                        }
                        if (cancel) processing = false;
                        if (c != null) hitCreatures.Add(c);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            if (successfullHits.Count > 0) endpoint = successfullHits.Last().point;
            else endpoint = Vector3.zero;

            return hitCreatures;
        }

        public static Creature ProcessHit(Transform muzzle, RaycastHit hit, List<RaycastHit> successfullHits, ProjectileData data, float damageMultiplier, List<Creature> hitCreatures, Item gunItem, out bool lowerDamageLevel, out bool cancel, ref int penetrationPower)
        {
            if (hit.collider.GetComponentInParent<Shootable>() is Shootable shootable) shootable.Shoot((ProjectileData.PenetrationLevels)penetrationPower);

            #region static non creature hit
            if (hit.rigidbody == null)
            {
                if (data.hasImpactEffect)
                {
                    EffectInstance ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal));
                    ei.SetIntensity(100f);
                    ei.Play();
                }

                successfullHits.Add(hit);
                lowerDamageLevel = true;
                cancel = GetRequiredPenetrationLevel(hit.collider) > (int)penetrationPower;
                return null;
            }
            #endregion static non creature hit

            #region creature hit
            if (hit.collider.gameObject.GetComponentInParent<Ragdoll>() is Ragdoll rag)
            {
                if (!hitCreatures.Contains(rag.creature))
                {
                    hitCreatures.Add(rag.creature);

                    Creature cr = rag.creature;
                    RagdollPart ragdollPart = hit.collider.gameObject.GetComponentInParent<RagdollPart>();
                    FirearmsScore.local.shotsHit++;

                    bool penetrated = GetRequiredPenetrationLevel(hit, muzzle.forward, gunItem) <= (int)penetrationPower;

                    #region Impact effect
                    if (data.hasBodyImpactEffect)
                    {
                        //Effect
                        EffectInstance ei = Catalog.GetData<EffectData>(penetrated ? "BulletImpactFlesh_Ghetto05_FirearmSDKv2" : "BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal), hit.collider.transform);
                        ei.SetIntensity(100f);
                        ei.Play();
                    }
                    if (data.drawsImpactDecal && penetrated) DrawDecal(ragdollPart, hit, data.customImpactDecalId);
                    #endregion Impact effect
                    
                    if (data.drawsImpactDecal)
                        BloodSplatter(hit.point, muzzle.forward, data.forcePerProjectile, data.projectileCount, penetrationPower, penetrated);

                    #region Damage level determination
                    float damageModifier = 1;
                    switch (ragdollPart.type)
                    {
                        case RagdollPart.Type.Head: //damage = infinity, remove voice, push(3)
                            {
                                FirearmsScore.local.headshots++;
                                if (penetrated && data.lethalHeadshot)
                                    damageModifier = Mathf.Infinity;
                                else
                                    damageModifier = 2;
                                if (penetrated && data.lethalHeadshot && WouldCreatureBeKilled(data.damagePerProjectile * damageModifier, cr) && !cr.isPlayer)
                                {
                                    cr.brain.instance.GetModule<BrainModuleSpeak>().Unload();
                                    cr.brain.instance.tree.Reset();
                                    cr.StartCoroutine(DelayedStopAnimating(cr));
                                }

                                if (penetrated && data.slicesBodyParts)
                                    ragdollPart.TrySlice();
                            }
                            break;
                        case RagdollPart.Type.Neck: //damage = infinity, push(1)
                            {
                                if (penetrated && data.lethalHeadshot) damageModifier = Mathf.Infinity;
                                else damageModifier = 2;
                            }
                            break;
                        case RagdollPart.Type.Torso: //damage = damage, push(2)
                            {
                                if (penetrated && FirearmsSettings.incapitateOnTorsoShot > 0f && data.enoughToIncapitate && !cr.isKilled && !cr.isPlayer)
                                {
                                    gunItem.StartCoroutine(TemporaryKnockout(FirearmsSettings.incapitateOnTorsoShot, 0, cr));
                                }
                            }
                            break;
                        case RagdollPart.Type.LeftArm: //damage = damage/3, release weapon, push(1)
                            {
                                damageModifier = 0.3f;
                                if (!cr.isKilled && !cr.isPlayer) cr.handLeft.TryRelease();
                            }
                            break;
                        case RagdollPart.Type.RightArm: //damage = damage/3, release weapon, push(1)
                            {
                                damageModifier = 0.3f;
                                if (!cr.isKilled && !cr.isPlayer) cr.handRight.TryRelease();
                            }
                            break;
                        case RagdollPart.Type.LeftFoot: //damage = damage/4, destabilize, push(3)
                            {
                                damageModifier = 0.25f;
                                if (!cr.isKilled && !cr.isPlayer) cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                            }
                            break;
                        case RagdollPart.Type.RightFoot: //damage = damage/4, destabilize, push(3)
                            {
                                damageModifier = 0.25f;
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
                                damageModifier = 0.3f;
                                if (!cr.isKilled && !cr.isPlayer)
                                    cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                            }
                            break;
                        case RagdollPart.Type.RightLeg: //damage = damage/3, destabilize, push(3)
                            {
                                damageModifier = 0.3f;
                                if (!cr.isKilled && !cr.isPlayer)
                                    cr.ragdoll.SetState(Ragdoll.State.Destabilized);
                            }
                            break;
                    }
                    cr.TryPush(Creature.PushType.Hit, muzzle.forward, 0);
                    if (penetrated && data.slicesBodyParts && !cr.isPlayer && Slice(ragdollPart))
                        ragdollPart.TrySlice();
                    if (!penetrated)
                        damageModifier /= 4;
                    #endregion Damage level determination

                    #region Damaging
                    CollisionInstance coll = new CollisionInstance(new DamageStruct(FirearmsSettings.bulletsAreBlunt ? DamageType.Blunt : DamageType.Pierce, data.damagePerProjectile));
                    coll.damageStruct.damage = EvaluateDamage(data.damagePerProjectile * damageModifier, cr);
                    coll.damageStruct.damageType = FirearmsSettings.bulletsAreBlunt ? DamageType.Blunt : DamageType.Pierce;
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

                    try { cr.Damage(coll); } catch (Exception) { }
                    #endregion Damaging

                    #region Additional Effects
                    //Taser
                    if (data.isElectrifying) cr.TryElectrocute(data.tasingForce, data.tasingDuration, true, false, data.playTasingEffect? Catalog.GetData<EffectData>("ImbueLightningRagdoll") : null);

                    //Force knockout
                    if ((data.forceDestabilize || data.isElectrifying) && !cr.isPlayer && !cr.isKilled) cr.ragdoll.SetState(Ragdoll.State.Destabilized);

                    //RB Push
                    if (cr.isKilled && !cr.isPlayer && hit.rigidbody != null)
                    {
                        hit.rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
                        //cr.locomotion.rb.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse);
                    }

                    //Stun
                    else if (!cr.isPlayer)
                    {
                        if (data.forceIncapitate) cr.brain.AddNoStandUpModifier(gunItem);
                        else if (data.knocksOutTemporarily)
                        {
                            gunItem.StartCoroutine(TemporaryKnockout(data.temporaryKnockoutTime, data.kockoutDelay, cr));
                        }
                    }

                    BrainModuleHitReaction hitReaction = cr.brain.instance.GetModule<BrainModuleHitReaction>();
                    hitReaction.SetStagger(hitReaction.staggerMedium);
                    #endregion Additional Effects

                    cancel = !penetrated;
                    lowerDamageLevel = true;
                    return cr;
                }
                else
                {
                    lowerDamageLevel = false;
                    cancel = false;
                    return null;
                }
            }
            #endregion creature hit
            #region non creature hit
            else
            {
                if (data.hasImpactEffect)
                {
                    EffectInstance ei = Catalog.GetData<EffectData>("BulletImpactGround_Ghetto05_FirearmSDKv2").Spawn(hit.point, Quaternion.LookRotation(hit.normal));
                    ei.SetIntensity(100f);
                    ei.Play();
                }
                if (hit.collider.GetComponentInParent<Item>() is Item hitItem)
                {
                    hit.rigidbody.AddForce(muzzle.forward * (data.forcePerProjectile / 10), ForceMode.Impulse);
                    try
                    {
                        foreach (Breakable b in hitItem.GetComponentsInChildren<Breakable>())
                        {
                            b.Break();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    try { hit.rigidbody.AddForce(muzzle.forward * data.forcePerProjectile, ForceMode.Impulse); } catch (Exception) { }
                }

                lowerDamageLevel = true;
                cancel = GetRequiredPenetrationLevel(hit.collider) > (int)penetrationPower;
                return null;
            }
            #endregion non creature hit
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
            return !FirearmsSettings.disableGore && (part.sliceAllowed || part.name.Equals("Spine")) && !part.ragdoll.creature.isPlayer;
        }

        private static void DrawDecal(RagdollPart rp, RaycastHit hit, string customDecal, bool isGore = true)
        {
            if (FirearmsSettings.disableGore && isGore) return;

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
            GameManager.local.StartCoroutine(RevealMaskProjection.ProjectAsync(rev.position + rev.forward * rem.offsetDistance, -rev.forward, rev.up, rem.depth, rem.maxSize, rem.textureContainer.GetRandomTexture(), rem.maxChannelMultiplier, rmcs, rem.revealData, null));
        }

        private static void BloodSplatter(Vector3 origin, Vector3 direction, float force, int projectileCount, int penetrationPower, bool penetratedArmor)
        {
            if (FirearmsSettings.disableGore || FirearmsSettings.disableBloodSpatters || penetrationPower < 2 || !penetratedArmor)
                return;
            int layer = LayerMask.GetMask("Default", "DroppedItem", "MovingItem", "PlayerLocomotionObject");
            if (Physics.Raycast(origin, direction, out RaycastHit hit, force * projectileCount / 30, layer, QueryTriggerInteraction.Ignore))
            {
                GameObject go = new GameObject("temp_" + Random.Range(0, 10000));
                go.transform.position = hit.point;
                go.transform.rotation = Quaternion.LookRotation(hit.normal);
                Util.RandomizeZRotation(go.transform);
                EffectInstance ei = Catalog.GetData<EffectData>("DropBlood").Spawn(hit.point, go.transform.rotation, null, null, false);
                ei.SetIntensity(100f);

                EffectDecal particle = (EffectDecal)ei.effects[0];
                particle.baseLifeTime = particle.baseLifeTime * 20f * FirearmsSettings.bloodSplatterLifetimeMultiplier;
                particle.emissionLifeTime = particle.emissionLifeTime * 20 * FirearmsSettings.bloodSplatterLifetimeMultiplier;
                particle.size = particle.size * force / 40 * projectileCount * FirearmsSettings.bloodSplatterSizeMultiplier;
                
                ei.Play();
            }
        }

        public static void FireItem(Transform muzzle, ProjectileData data, Item item)
        {
            Vector3 fireDir = muzzle.forward;
            Vector3 firePoint = muzzle.position;
            Quaternion fireRotation = muzzle.rotation;
            Util.SpawnItem(data.projectileItemId, $"[Cartridge of {data.projectileItemId}]", thisSpawnedItem =>
            {
                item.StartCoroutine(FireItemCoroutine(thisSpawnedItem, item, firePoint, fireRotation, fireDir, data.muzzleVelocity));
                if (data.destroyTime != 0f)
                    thisSpawnedItem.Despawn(data.destroyTime);
            }, firePoint, fireRotation);
        }

        private static IEnumerator FireItemCoroutine(Item projectilItem, Item gunItem, Vector3 pos, Quaternion rot, Vector3 dir, float velocity)
        {
            projectilItem.physicBody.rigidBody.isKinematic = true;
            Util.IgnoreCollision(projectilItem.gameObject, gunItem.gameObject, true);
            Util.IgnoreCollision(projectilItem.gameObject, Player.local.gameObject, true);
            projectilItem.transform.rotation = rot;
            projectilItem.transform.position = pos;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            projectilItem.physicBody.rigidBody.isKinematic = false;
            projectilItem.Throw();
            projectilItem.physicBody.rigidBody.velocity = dir * velocity;
        }

        public static IEnumerator TemporaryKnockout(float duration, float delay, Creature creature)
        {
            GameObject handler = new GameObject($"TempKnockoutHandler_{Random.Range(0, 9999)}");
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
                if (CheckExplosionCreatureHit(hitCreature, point))
                {
                    CollisionInstance coll = new CollisionInstance(new DamageStruct(DamageType.Pierce, EvaluateDamage(data.damage, hitCreature)));
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
                    try { hitCreature.Damage(coll); } catch (Exception) { }

                    hitCreature.locomotion.rb.AddExplosionForce(data.force, point, data.radius, data.upwardsModifier);
                    if (hitCreature.isKilled) hitCreature.StartCoroutine(ExplodeCreature(point, data, hitCreature));
                }
            }

            foreach (Shootable hitShootable in hitShootables)
            {
                hitShootable.Shoot(ProjectileData.PenetrationLevels.Kevlar);
            }

            foreach (Item hitItem in hitItems)
            {
                hitItem.physicBody.rigidBody.AddExplosionForce(data.force, point, data.radius * 3, data.upwardsModifier);
            }

            if (!string.IsNullOrWhiteSpace(data.effectId))
            {
                EffectInstance ei = Catalog.GetData<EffectData>(data.effectId).Spawn(point, Quaternion.Euler(0, 0, 0));
                ei.Play();
            }
        }

        private static IEnumerator ExplodeCreature(Vector3 point, ExplosiveData data, Creature hitCreature)
        {
            if (!hitCreature.isPlayer && FirearmsSettings.explosionsDismember && !FirearmsSettings.disableGore)
            {
                foreach (RagdollPart rp in hitCreature.ragdoll.parts.ToArray().Reverse())
                {
                    yield return new WaitForEndOfFrame();
                    if (Vector3.Distance(rp.transform.position, point) < (data.radius / 2) && Slice(rp))
                    {
                        rp.TrySlice();
                        rp.physicBody.rigidBody.AddForce((rp.physicBody.rigidBody.position - point).normalized * data.force * 2);
                    }
                    else rp.physicBody.rigidBody.AddForce((rp.physicBody.rigidBody.position - point).normalized * data.force * 10);
                }
            }
            yield break;
        }

        public static bool CheckExplosionCreatureHit(Creature c, Vector3 origin)
        {
            foreach (RagdollPart b in c.ragdoll.parts)
            {
                if (!Physics.Raycast(b.transform.position, origin - b.transform.position, Vector3.Distance(b.transform.position, origin) - 0.1f, LayerMask.GetMask("Default"))) return true;
            }

            return false;
        }

        public static float EvaluateDamage(float perfifty, Creature c)
        {
            float perFiftyDamage = perfifty * FirearmsSettings.damageMultiplier;
            float aspect = perFiftyDamage / 50;
            float damageToBeDone = Mathf.Clamp(c.maxHealth, 50f, 100f) * aspect;

            return damageToBeDone;
        }

        public static bool WouldCreatureBeKilled(float perfifty, Creature c)
        {
            return EvaluateDamage(perfifty, c) >= c.currentHealth;
        }

        public static int GetRequiredPenetrationLevel(RaycastHit hit, Vector3 direction, Item handler)
        {
            int hitMaterialHash = -1;
            ColliderGroup colliderGroup = hit.collider.GetComponentInParent<ColliderGroup>();

            if (colliderGroup != null) handler.mainCollisionHandler.MeshRaycast(colliderGroup, hit.point, hit.normal, direction, ref hitMaterialHash);
            if (hitMaterialHash == -1) hitMaterialHash = Animator.StringToHash(hit.collider.material.name);
            TryGetMaterial(hitMaterialHash, out MaterialData matDat);
            return (int)RequiredPenetrationPowerData.GetRequiredLevel(matDat.id);
        }

        public static int GetRequiredPenetrationLevel(Collider collider)
        {
            if (collider.material == null) return 0;
            int hitMaterialHash = Animator.StringToHash(collider.material.name);
            TryGetMaterial(hitMaterialHash, out MaterialData matDat);
            return (int)RequiredPenetrationPowerData.GetRequiredLevel(matDat.id);
        }

        public static bool TryGetMaterial(int targetPhysicMaterialHash, out MaterialData targetMaterial)
        {
            targetMaterial = null;
            List<CatalogData> list = Catalog.GetDataList(Category.Material);
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