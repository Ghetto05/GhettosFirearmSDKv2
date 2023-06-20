using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Chemicals
{
    public class NPCChemicalsModule : MonoBehaviour
    {
        Creature creature;

        //---BOOLS---
        bool inCSgas = false;
        bool inSmoke = false;
        bool inPoisonGas = true;

        //---EFFECTS---
        float horFov;
        float verFov;
        BrainModuleDetection det;

        public List<GameObject> gasMasks;

        void Awake()
        {
            creature = gameObject.GetComponent<Creature>();
            gasMasks = new List<GameObject>();
            det = creature.brain.instance.GetModule<BrainModuleDetection>();
            horFov = det.sightDetectionHorizontalFov;
            verFov = det.sightDetectionVerticalFov;
        }

        void Update()
        {
            bool foundCSgas = false;
            bool foundSmoke = false;
            bool foundPoisonGas = false;
            float highestPoisonGasDamage = 0f;
            foreach (Collider c in Physics.OverlapSphere(creature.animator.GetBoneTransform(HumanBodyBones.Head) != null ? creature.animator.GetBoneTransform(HumanBodyBones.Head).position : creature.brain.transform.position, 0.1f))
            {
                if (c.gameObject.name.Equals("CSgas_Zone"))
                {
                    foundCSgas = true;
                }
                if (c.gameObject.name.Equals("Smoke_Zone") | PlayerEffectsAndChemicalsModule.local.IsInSmoke())
                {
                    foundSmoke = true;
                }
                if (c.gameObject.name.Equals("PoisonGas_Zone"))
                {
                    foundPoisonGas = true;
                    float d = float.Parse(c.transform.GetChild(0).name);
                    if (d > highestPoisonGasDamage) highestPoisonGasDamage = d;
                }
            }

            if (foundSmoke && !inSmoke) EnterSmoke();
            else if (!foundSmoke && inSmoke) ExitSmoke();

            if (foundCSgas && !inCSgas) EnterCSgas();
            else if (!foundCSgas && inCSgas) ExitCSgas();

            if (foundPoisonGas && !inPoisonGas) EnterPoisonGas();
            else if (!foundPoisonGas && inPoisonGas) ExitPoisonGas();

            UpdateCSgas();
            UpdateSmoke();
            UpdatePoisonGas(highestPoisonGasDamage * Time.deltaTime);
        }

        void UpdateSmoke()
        {
            if (!inSmoke) return;

            if (det != null)
            {
                det.sightDetectionHorizontalFov = 0f;
                det.sightDetectionVerticalFov = 0f;
            }
            creature.brain.currentTarget = null;
            creature.brain.SetState(Brain.State.Alert);
        }

        void EnterSmoke()
        {
            inSmoke = true;
        }

        void ExitSmoke()
        {
            inSmoke = false;
            if (det != null)
            {
                det.sightDetectionHorizontalFov = horFov;
                det.sightDetectionVerticalFov = verFov;
            }
        }

        void UpdateCSgas()
        {
            if (!inCSgas) return;

            if (det != null)
            {
                det.sightDetectionHorizontalFov = 0f;
                det.sightDetectionVerticalFov = 0f;
            }
            creature.brain.currentTarget = null;
            creature.brain.SetState(Brain.State.Idle);
        }

        void EnterCSgas()
        {
            inCSgas = true;
            creature.brain.AddNoStandUpModifier(this);
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
        }

        void ExitCSgas()
        {
            inCSgas = false;
            if (det != null)
            {
                det.sightDetectionHorizontalFov = horFov;
                det.sightDetectionVerticalFov = verFov;
            }
            creature.brain.RemoveNoStandUpModifier(this);
        }

        void UpdatePoisonGas(float damage)
        {
            if (!inPoisonGas) return;
            creature.Damage(new CollisionInstance(new DamageStruct(DamageType.Energy, damage)));
        }

        void EnterPoisonGas()
        {
            inPoisonGas = true;
        }

        void ExitPoisonGas()
        {
            inPoisonGas = false;
        }
    }
}
