using GhettosFirearmSDKv2.Explosives;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class ImpactDetonator : Explosive
{
    public Explosive explosive;
    public Collider[] triggers;
    public float delay;
    public bool startAtAwake;
    public float minimumArmingTime;
    public float minimumImpactForce;
    public float selfDestructDelay;
    private bool _armed;
    private Vector3 _startPoint;
    private float _clearance = 0.3f;

    private float _startTime;

    private void Awake()
    {
        if (startAtAwake) StartArming();
        _startPoint = transform.position;
    }

    public override void ActualDetonate()
    {
        StartArming();
        base.ActualDetonate();
    }

    public void StartArming()
    {
        _startTime = Time.time;
        _armed = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsArmed() && TriggerColliderHit(collision))
        {
            if (explosive != null)
            {
                explosive.Detonate(delay);
                explosive.impactNormal = collision.contacts[0].normal;
            }
        }
    }

    public bool IsArmed()
    {
        return _armed && Time.time - _startTime >= minimumArmingTime && Vector3.Distance(_startPoint, transform.position) > _clearance;
    }

    private bool TriggerColliderHit(Collision collision)
    {
        foreach (var c in triggers)
        {
            if (Util.CheckForCollisionWithThisCollider(collision, c)) return true;
        }
        return false;
    }

    private void Update()
    {
        if (selfDestructDelay > 0.2f && Time.time > _startTime + selfDestructDelay)
        {
            explosive.Detonate(delay);
            explosive.impactNormal = transform.forward;
        }
    }
}