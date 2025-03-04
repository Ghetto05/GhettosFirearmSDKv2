using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class LeverAction : MonoBehaviour
{
    private const float Threshold = 3f;

    public BoltSemiautomatic bolt;
        
    public Rigidbody rb;
    public Transform lever;
    public Transform leverColliders;
    public Transform start;
    public Transform end;

    public float minAngle;
    public float maxAngle;
    private float _targetAngle;

    public AudioSource[] openSounds;
    public AudioSource[] closeSounds;

    public Handle grip;
    public Handle leverHandle;

    private HingeJoint _joint;

    private BoltBase.BoltState _state;
    private bool _reachedEnd;

    public void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
        leverHandle.SetTouch(false);
    }
        
    private void InvokedStart()
    {
        _targetAngle = Quaternion.Angle(start.localRotation, end.localRotation);
        Lock(true);
        bolt.overrideHeldState = true;
        bolt.firearm.OnAltActionEvent += OnAltAction;
        bolt.OnFireLogicFinishedEvent += OnFireLogicFinished;
        bolt.firearm.item.OnGrabEvent += OnGrab;
        bolt.firearm.item.OnUngrabEvent += OnUnGrab;
    }

    private void OnUnGrab(Handle handle, RagdollHand ragdollHand, bool throwing)
    {
            
    }

    private void OnGrab(Handle handle, RagdollHand ragdollHand)
    {
        if (handle == leverHandle)
        {
            //InitializeHandJoint(ragdollHand);
        }
    }

    private void OnFireLogicFinished()
    {
        if (_state != BoltBase.BoltState.Locked)
            return;
        //Unlock();
        Invoke(nameof(Unlock), 0.05f);
    }

    private void OnAltAction(bool longPress)
    {
        if (longPress || _state != BoltBase.BoltState.Locked)
            return;
        Unlock();
    }

    private void FixedUpdate()
    {
        leverColliders.parent = _state == BoltBase.BoltState.Locked ? lever : rb.transform;
        leverColliders.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (_state == BoltBase.BoltState.Locked)
        {
            bolt.bolt.localPosition = bolt.startPoint.localPosition;
            return;
        }

        var target = Mathf.Clamp(Util.NormalizeAngle(rb.transform.localEulerAngles.x), minAngle, maxAngle);
        lever.localEulerAngles = new Vector3(target, 0, 0);

        bolt.rigidBody.transform.localPosition = Vector3.Lerp(bolt.startPoint.localPosition, bolt.endPoint.localPosition, Time());
        bolt.bolt.transform.localPosition = Vector3.Lerp(bolt.startPoint.localPosition, bolt.endPoint.localPosition, Time());
            
        if (LimitLeverChanged(out var limit))
        {
            var lockAngle = Util.NormalizeAngle(lever.localEulerAngles.x);
            _joint.limits = new JointLimits { max = limit ? lockAngle + 0.001f : maxAngle, min = limit ? lockAngle : minAngle };
        }
            
        if (Quaternion.Angle(lever.localRotation, start.localRotation) <= Threshold && _state == BoltBase.BoltState.Moving && _reachedEnd)
        {
            Lock();
            Util.PlayRandomAudioSource(closeSounds);
        }

        if (Quaternion.Angle(lever.localRotation, end.localRotation) <= Threshold && !_reachedEnd)
        {
            _reachedEnd = true;
            Util.PlayRandomAudioSource(openSounds);
        }
    }

    private bool LimitLeverChanged(out bool limit)
    {
        var held = leverHandle.handlers.Count > 0;
        var limited = Mathf.Abs(Mathf.Abs(_joint.limits.min) - Mathf.Abs(_joint.limits.max)) < 0.001f;
        var notLimited = Mathf.Abs(Mathf.Abs(_joint.limits.max) - Mathf.Abs(maxAngle)) < 0.001f && Mathf.Abs(Mathf.Abs(_joint.limits.min) - Mathf.Abs(minAngle)) < 0.001f;

        limit = !held;
        return (held && limited) || (!held && notLimited);
    }

    private float Time()
    {
        var currentAngle = Quaternion.Angle(start.localRotation, lever.localRotation);
        return Mathf.Clamp01(currentAngle / _targetAngle);
    }

    public void Lock(bool forced = false)
    {
        if (_state == BoltBase.BoltState.Locked && !forced)
            return;
        if (_joint != null)
            Destroy(_joint);
        rb.isKinematic = true;
        _state = BoltBase.BoltState.Locked;
        lever.localRotation = start.localRotation;
        rb.transform.localRotation = start.localRotation;
        //joint.limits = new JointLimits { max = 0, min = 0 };
        bolt.heldState = false;
        bolt.rigidBody.transform.position = bolt.startPoint.position;
        bolt.bolt.position = bolt.startPoint.position;
        bolt.laststate = bolt.state;
        bolt.state = BoltBase.BoltState.Locked;

        //DestroyHandleJoint();
        leverHandle.SetTouch(false);
        grip.SetTouch(true);
        var handlers = leverHandle.handlers.ToList();
        leverHandle.Release();
        foreach (var ragdollHand in handlers)
        {
            ragdollHand.Grab(grip);
        }

        bolt.firearm.item.disableSnap = false;
    }

    public void Unlock()
    {
        if (_state != BoltBase.BoltState.Locked)
            return;

        rb.isKinematic = false;
        InitializeJoint();
        _joint.limits = new JointLimits { max = maxAngle, min = minAngle };
        _reachedEnd = false;
        _state = BoltBase.BoltState.Moving;
        bolt.heldState = true;

        leverHandle.SetTouch(true);
        grip.SetTouch(false);
        var handlers = grip.handlers.ToList();
        grip.Release();
        foreach (var ragdollHand in handlers)
        {
            ragdollHand.Grab(leverHandle);
            //InitializeHandJoint(ragdollHand);
        }
        if (bolt.firearm.triggerState)
            bolt.firearm.ChangeTrigger(false);
            
        bolt.firearm.item.disableSnap = true;
    }

    private void InitializeJoint()
    {
        if (_joint == null)
            _joint = bolt.firearm.gameObject.AddComponent<HingeJoint>();
        _joint.axis = Vector3.left;
        rb.transform.position = start.position;
        rb.transform.rotation = start.rotation;
        _joint.connectedBody = rb;
        //joint.massScale = 0.00001f;
        _joint.anchor = BoltBase.GrandparentLocalPosition(rb.transform, bolt.firearm.item.transform);
        _joint.useLimits = true;
        _joint.limits = new JointLimits { max = maxAngle, min = minAngle }; //{ min = 0, max = 0 };
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Vector3.zero;
    }
}