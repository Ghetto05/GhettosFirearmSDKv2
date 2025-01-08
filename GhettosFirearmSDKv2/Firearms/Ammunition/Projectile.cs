using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class Projectile : MonoBehaviour
{
    public Item item;
    public ProjectileData data;
    public List<Collider> affectors;
    public bool stick;
    public bool despawnOnContact;
    public float automaticDespawnDelay = -1;

    private bool _executed;
    private bool _stuck;

    private Item _stuckItem;
    private Creature _stuckCreature;

    private void Start()
    {
        item.mainCollisionHandler.OnCollisionStartEvent += OnCollisionStart;
    }

    private void OnCollisionStart(CollisionInstance collision)
    {
        if (!affectors.Contains(collision.sourceCollider))
            return;
        
        if (_executed)
            return;

        _executed = true;

        var penPow = 0;
        var hitCreatures = new List<Creature>();
        var killedCreatures = new List<Creature>();
        FireMethods.ProcessHit(transform, (FireMethods.HitData)collision, new List<FireMethods.HitData>(), data, 1, hitCreatures, killedCreatures, item, out _, out _, ref penPow);
        
        HitEvent?.Invoke(hitCreatures, killedCreatures);
        
        if (despawnOnContact)
            item.Despawn(0.01f);

        if (stick)
            Stick(collision.targetCollider.attachedRigidbody);
        
        if (automaticDespawnDelay >= 0)
            item.Despawn(automaticDespawnDelay);
    }

    private void Stick(Rigidbody rb)
    {
        if (_stuck)
            return;
        _stuck = true;
        item.DisallowDespawn = true;
        var joint = item.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = rb;

        if (rb?.GetComponent<Item>() is { } i)
        {
            _stuckItem = i;
            i.OnDespawnEvent += StuckItemOnDespawn;
        }

        if (rb?.GetComponent<Creature>() is { } c)
        {
            _stuckCreature = c;
            c.OnDespawnEvent += StuckCreatureOnDespawn;
        }
    }

    private void StuckItemOnDespawn(EventTime eventTime)
    {
        if (eventTime != EventTime.OnStart)
            return;

        _stuckItem.OnDespawnEvent -= StuckItemOnDespawn;
        item.Despawn();
    }

    private void StuckCreatureOnDespawn(EventTime eventTime)
    {
        if (eventTime != EventTime.OnStart)
            return;

        _stuckCreature.OnDespawnEvent -= StuckCreatureOnDespawn;
        item.Despawn();
    }

    public delegate void OnHit(List<Creature> hitCreatures, List<Creature> killedCreatures);
    public event OnHit HitEvent;
}