using System;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MortarFuze : MonoBehaviour
{
    public enum Modes
    {
        Impact,
        Proximity,
        Delay
    }

    public GameObject manager;
    public Explosive explosive;

    public Collider[] impactColliders;
    public Transform proximitySource;
    public MortarFuzeMode[] modes;
    public Transform selector;
    public Transform[] selectorPositions;
    public Handle selectorHandle;
    public AudioSource[] selectorSounds;

    public float minimumArmingSpeed;
    public float armingDistance;

    private Util.InitializationData _initializationData;
    private SaveNodeValueInt _modeSaveData;
    private float _armingDistanceTravelled;
    private bool _armed;
    private float? _armingStartTime;

    private Guid _debugId;
    private void Start()
    {
        _debugId = Guid.NewGuid();
        StartCoroutine(Util.RequestInitialization(manager, Initialization));
    }

    private void Initialization(Util.InitializationData e)
    {
        _initializationData = e;
        _initializationData.InteractionProvider.OnTeardown += OnTeardown;
        _initializationData.InteractionProvider.OnHeldAction += OnHeldAction;
        _initializationData.InteractionProvider.OnCollision += OnCollision;
        _modeSaveData = _initializationData.Manager.SaveData.FirearmNode.GetOrAddValue("MortarFuzeMode",
            new SaveNodeValueInt(),
            out var addedNew);
        if (!addedNew)
        {
            CycleMode(_modeSaveData.Value, true);
        }
    }

    private void OnTeardown()
    {
        _initializationData.InteractionProvider.OnTeardown -= OnTeardown;
        _initializationData.InteractionProvider.OnHeldAction -= OnHeldAction;
        _initializationData.InteractionProvider.OnCollision -= OnCollision;
    }

    private void OnCollision(Collision collision)
    {
        if (!_armed ||
            !collision.contacts.Any(x => impactColliders.Contains(x.thisCollider)))
        {
            return;
        }

        explosive?.Detonate(modes[_modeSaveData.Value].mode == Modes.Impact ? modes[_modeSaveData.Value].parameter : 0);
    }

    private void OnHeldAction(IInteractionProvider.HeldActionData e)
    {
        if (e.Action == Interactable.Action.UseStart && e.Handle == selectorHandle)
        {
            CycleMode();
        }
    }

    private void CycleMode(int? target = null, bool silent = false)
    {
        if (target == null)
        {
            var c = _modeSaveData.Value;
            c = c + 1 == modes.Length ? 0 : c + 1;
            _modeSaveData.Value = c;
        }

        var t = selectorPositions[_modeSaveData.Value];
        selector.SetLocalPositionAndRotation(t.localPosition, t.localRotation);
        if (!silent)
        {
            Util.PlayRandomAudioSource(selectorSounds);
        }
    }

    private void FixedUpdate()
    {
        Debug.Log($"({_debugId}) Armed: {_armed} Arming: {explosive.item.physicBody.velocity.magnitude > minimumArmingSpeed}");

        if (!_armed)
        {
            if (explosive.item.physicBody.velocity.magnitude > minimumArmingSpeed)
            {
                if (_armingStartTime == null)
                {
                    _armingStartTime = Time.time;
                }
                _armingDistanceTravelled = explosive.item.physicBody.velocity.magnitude * (Time.time - _armingStartTime.Value);
                Debug.Log($"({_debugId}) Distance: {_armingDistanceTravelled}");
            }
            else
            {
                _armingStartTime = null;
            }
        }

        if (!_armed && _armingDistanceTravelled >= armingDistance)
        {
            Arm();
        }

        if (_armed && modes[_modeSaveData.Value].mode == Modes.Proximity &&
            Physics.Raycast(proximitySource.position, proximitySource.forward, modes[_modeSaveData.Value].parameter, FireMethods.FireLayerMask))
        {
            explosive.Detonate();
        }
    }

    private void Arm()
    {
        _armed = true;
        if (modes[_modeSaveData.Value].mode != Modes.Delay)
        {
            return;
        }

        explosive.Invoke(nameof(Explosive.Detonate), modes[_modeSaveData.Value].parameter);
    }
}