using System;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class SlidingStock : MonoBehaviour
{
    public GameObject manager;
    private Util.InitializationData _initializationData;

    public GhettoHandle handle;
    public Transform axis;
    public Transform forwardEnd;
    public Transform rearwardEnd;
    public bool usePositions;
    public Transform[] positions;
    public AudioSource[] unlockSounds;
    public AudioSource[] lockSounds;

    [NonSerialized]
    public bool Unlocked;

    private ConfigurableJoint _joint;
    private Rigidbody _baseRigidbody;
    private Rigidbody _stockRigidbody;
    private CapsuleCollider _stabilizer;

    private const string SaveDataID = "SlidingStockPosition_";
    private SaveNodeValueFloat _positionSaveData;

    private void Start()
    {
        StartCoroutine(Util.RequestInitialization(manager, Initialize));
    }

    private void Initialize(Util.InitializationData data)
    {
        _baseRigidbody = new GameObject().AddComponent<Rigidbody>();
        _baseRigidbody.isKinematic = true;
        _stockRigidbody = new GameObject().AddComponent<Rigidbody>();
        _stockRigidbody.isKinematic = true;
        _stabilizer = _stockRigidbody.gameObject.AddComponent<CapsuleCollider>();
        _stabilizer.radius = 0.2f;
        _initializationData = data;
        _initializationData.InteractionProvider.OnHeldAction += OnHeldAction;
        _initializationData.InteractionProvider.OnTeardown += Teardown;
        _initializationData.Manager.OnAttachmentAdded += OnAttachmentAdded;
        _positionSaveData = _initializationData.Node.GetOrAddValue(SaveDataID + name, new SaveNodeValueFloat { Value = 0f });
        axis.localPosition = Vector3.Lerp(forwardEnd.localPosition, rearwardEnd.localPosition, _positionSaveData.Value);
        OnAttachmentAdded(null, null);
    }

    private void OnAttachmentAdded(Attachment attachment, AttachmentPoint attachmentPoint)
    {
        if (handle)
        {
            handle.customRigidBody = _stockRigidbody;
        }

        foreach (var h in axis.GetComponentsInChildren<GhettoHandle>().Where(x => x.tags.HasFlag(GhettoHandle.Tags.SlidingStockHandle)))
        {
            h.customRigidBody = _stockRigidbody;
        }
    }

    private void OnHeldAction(IInteractionProvider.HeldActionData e)
    {
        if (e.Handle != handle && !axis.GetComponentsInChildren<GhettoHandle>()
                .Any(x => x.tags.HasFlag(GhettoHandle.Tags.SlidingStockHandle)))
        {
            return;
        }

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (e.Action)
        {
            case Interactable.Action.UseStart:
                e.Handled = true;
                Unlock();
                e.Handle.UnGrabbed += StockHandleUnGrabbed;
                break;
            case Interactable.Action.UseStop:
                e.Handled = true;
                Lock();
                break;
        }
    }

    private void StockHandleUnGrabbed(RagdollHand r, Handle h, EventTime e)
    {
        if (e == EventTime.OnStart)
        {
            Lock();
        }
    }

    private void Teardown()
    {
        _initializationData.InteractionProvider.OnHeldAction -= OnHeldAction;
        _initializationData.InteractionProvider.OnTeardown -= Teardown;
        _initializationData.Manager.OnAttachmentAdded -= OnAttachmentAdded;
    }

    private void Lock()
    {
        Unlocked = false;
        Util.PlayRandomAudioSource(lockSounds);
        _stockRigidbody.isKinematic = true;
        Destroy(_joint);
        _positionSaveData.Value = GetPosition(axis);

        if (!usePositions)
        {
            return;
        }

        var pos = positions.OrderBy(x => Vector3.Distance(forwardEnd.localPosition, x.localPosition)).FirstOrDefault();
        axis.localPosition = pos?.localPosition ?? Vector3.zero;
        _positionSaveData.Value = GetPosition(axis);
    }

    private void Unlock()
    {
        Util.PlayRandomAudioSource(unlockSounds);
        CreateJoint();
        _stockRigidbody.isKinematic = false;
        Unlocked = true;
    }

    private void CreateJoint()
    {
        _joint = _baseRigidbody.gameObject.AddComponent<ConfigurableJoint>();
        _joint.connectedBody = _stockRigidbody;
        // _joint.massScale = 0.00001f;
        _joint.anchor = BoltBase.GrandparentLocalPosition(_stockRigidbody.transform, _baseRigidbody.transform);
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Vector3.zero;
        var limit = new SoftJointLimit
        {
            limit = Vector3.Distance(forwardEnd.position, rearwardEnd.position) / 2
        };
        _joint.linearLimit = limit;
        _joint.xMotion = ConfigurableJointMotion.Locked;
        _joint.yMotion = ConfigurableJointMotion.Locked;
        _joint.zMotion = ConfigurableJointMotion.Limited;
        _joint.angularXMotion = ConfigurableJointMotion.Locked;
        _joint.angularYMotion = ConfigurableJointMotion.Locked;
        _joint.angularZMotion = ConfigurableJointMotion.Locked;
    }

    private void FixedUpdate()
    {
        if (!_joint || !_stabilizer || _stockRigidbody)
        {
            return;
        }
        
        _stabilizer.gameObject.layer = LayerMask.NameToLayer("FPVHide");
        axis.localPosition = Vector3.Lerp(forwardEnd.localPosition, rearwardEnd.localPosition, GetPosition(_stockRigidbody.transform));
    }

    private float GetPosition(Transform target)
    {
        return Vector3.Distance(forwardEnd.localPosition, target.localPosition) /
               Vector3.Distance(forwardEnd.localPosition, rearwardEnd.localPosition);
    }
}