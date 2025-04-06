using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Chemicals;

public class NpcChemicalsModule : MonoBehaviour
{
    private Creature _creature;

    //---BOOLS---
    private bool _inCSgas;
    private bool _inSmoke;
    private bool _inPoisonGas = true;

    //---EFFECTS---
    private float _horFov;
    private float _verFov;
    private BrainModuleDetection _det;

    public List<GameObject> gasMasks;

    private void Awake()
    {
        _creature = gameObject.GetComponent<Creature>();
        gasMasks = new List<GameObject>();
        _det = _creature.brain.instance.GetModule<BrainModuleDetection>();
        _horFov = _det.sightDetectionHorizontalFov;
        _verFov = _det.sightDetectionVerticalFov;
    }

    private void Update()
    {
        if (!PlayerEffectsAndChemicalsModule.local)
        {
            return;
        }

        var foundCSgas = false;
        var foundSmoke = false;
        var foundPoisonGas = false;
        var highestPoisonGasDamage = 0f;
        var position = _creature.animator.GetBoneTransform(HumanBodyBones.Head) ? _creature.animator.GetBoneTransform(HumanBodyBones.Head).position : _creature.brain.transform.position;

        var hits = Physics.OverlapSphere(position, 0.1f);
        foreach (var c in hits)
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
                var d = float.Parse(c.transform.GetChild(0).name);
                if (d > highestPoisonGasDamage)
                {
                    highestPoisonGasDamage = d;
                }
            }
        }

        if (foundSmoke && !_inSmoke)
        {
            EnterSmoke();
        }
        else if (!foundSmoke && _inSmoke)
        {
            ExitSmoke();
        }

        if (foundCSgas && !_inCSgas)
        {
            EnterCSgas();
        }
        else if (!foundCSgas && _inCSgas)
        {
            ExitCSgas();
        }

        if (foundPoisonGas && !_inPoisonGas)
        {
            EnterPoisonGas();
        }
        else if (!foundPoisonGas && _inPoisonGas)
        {
            ExitPoisonGas();
        }

        UpdateCSgas();
        UpdateSmoke();
        UpdatePoisonGas(highestPoisonGasDamage * Time.deltaTime);
    }

    private void UpdateSmoke()
    {
        if (!_inSmoke)
        {
            return;
        }

        if (_det is not null)
        {
            _det.sightDetectionHorizontalFov = 0f;
            _det.sightDetectionVerticalFov = 0f;
        }
        _creature.brain.currentTarget = null;
        _creature.brain.SetState(Brain.State.Alert);
    }

    private void EnterSmoke()
    {
        _inSmoke = true;
    }

    private void ExitSmoke()
    {
        _inSmoke = false;
        if (_det is not null)
        {
            _det.sightDetectionHorizontalFov = _horFov;
            _det.sightDetectionVerticalFov = _verFov;
        }
    }

    private void UpdateCSgas()
    {
        if (!_inCSgas)
        {
            return;
        }

        if (_det is not null)
        {
            _det.sightDetectionHorizontalFov = 0f;
            _det.sightDetectionVerticalFov = 0f;
        }
        _creature.brain.currentTarget = null;
        _creature.brain.SetState(Brain.State.Idle);
    }

    private void EnterCSgas()
    {
        _inCSgas = true;
        _creature.brain.AddNoStandUpModifier(this);
        _creature.ragdoll.SetState(Ragdoll.State.Destabilized);
    }

    private void ExitCSgas()
    {
        _inCSgas = false;
        if (_det is not null)
        {
            _det.sightDetectionHorizontalFov = _horFov;
            _det.sightDetectionVerticalFov = _verFov;
        }
        _creature.brain.RemoveNoStandUpModifier(this);
    }

    private void UpdatePoisonGas(float damage)
    {
        if (!_inPoisonGas)
        {
            return;
        }
        _creature.Damage(new CollisionInstance(new DamageStruct(DamageType.Energy, damage)));
    }

    private void EnterPoisonGas()
    {
        _inPoisonGas = true;
    }

    private void ExitPoisonGas()
    {
        _inPoisonGas = false;
    }
}