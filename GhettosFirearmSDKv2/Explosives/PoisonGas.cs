using UnityEngine;

namespace GhettosFirearmSDKv2.Explosives;

public class PoisonGas : Explosive
{
    private bool _active;
    private bool _ready;
    public float range;
    public float duration;
    public float emissionDuration;
    public float timestamp;
    public float damagePerSecond;
    public AudioSource loop;
    public ParticleSystem particle;
    private CapsuleCollider _zone;

    private void Awake()
    {
        if (item)
        {
            item.DisallowDespawn = true;
        }
    }

    public override void ActualDetonate()
    {
        _active = true;
        var obj = new GameObject("PoisonGas_Zone");
        var amObj = new GameObject(damagePerSecond.ToString());
        amObj.transform.SetParent(obj.transform);
        amObj.transform.localPosition = Vector3.zero;
        obj.layer = 28;
        _zone = obj.AddComponent<CapsuleCollider>();
        _zone.isTrigger = true;
        obj.transform.parent = transform;
        _zone.radius = range;
        obj.transform.localPosition = Vector3.zero;
        loop.Play();
        particle.Play();
        timestamp = Time.time;
        if (gameObject.GetComponentInParent<Rigidbody>() is { } rb)
        {
            rb.velocity = Vector3.zero;
        }
        _ready = true;
        base.ActualDetonate();
    }

    private void Update()
    {
        if (!detonated || !_ready)
        {
            return;
        }

        if (Time.time >= timestamp + emissionDuration && loop.isPlaying)
        {
            loop.Stop();
            _zone.gameObject.transform.SetParent(null);
        }

        if (!_active)
        {
            return;
        }

        if (Time.time >= timestamp + duration)
        {
            _active = false;
            Destroy(_zone.gameObject);
            if (item)
            {
                item.DisallowDespawn = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}