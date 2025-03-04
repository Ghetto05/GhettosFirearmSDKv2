using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class RevolverEjectorRod : MonoBehaviour
{
    private Firearm _firearm;
    public Revolver revolver;
    public Rigidbody rigidBody;
    public Transform axis;
    public Transform root;
    public Transform ejectPoint;
    private ConfigurableJoint _joint;
    private bool _ejectedSinceLastOpen;
    private CollisionRelay _collisionRelay;

    private void Start()
    {
        InitializeJoint();
        if (revolver.firearm is Firearm f)
        {
            _firearm = f;
            _firearm.OnAttachmentAdded += Firearm_OnAttachmentAddedEvent;
        }
        revolver.OnClose += Revolver_onClose;
        revolver.OnOpen += Revolver_onOpen;
        revolver.firearm.item.OnDespawnEvent += OnDespawn;
        revolver.useGravityEject = false;
        _collisionRelay = rigidBody.gameObject.AddComponent<CollisionRelay>();
        _collisionRelay.OnCollisionEnterEvent += revolver.OnCollisionEvent;
        Util.IgnoreCollision(axis.gameObject, revolver.firearm.gameObject, true);
        Revolver_onClose();
    }

    private void OnDespawn(EventTime eventTime)
    {
        if (eventTime != EventTime.OnStart)
            return;
            
        if (_firearm)
        {
            _firearm.OnAttachmentAdded -= Firearm_OnAttachmentAddedEvent;
        }
        revolver.OnClose -= Revolver_onClose;
        revolver.OnOpen -= Revolver_onOpen;
        revolver.firearm.item.OnDespawnEvent -= OnDespawn;
        _collisionRelay.OnCollisionEnterEvent -= revolver.OnCollisionEvent;
    }

    private void Firearm_OnAttachmentAddedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
    {
        Util.IgnoreCollision(axis.gameObject, revolver.firearm.gameObject, true);
    }

    private void Revolver_onOpen()
    {
        var vec = BoltBase.GrandparentLocalPosition(ejectPoint, revolver.rotateBody.transform);
        _joint.anchor = new Vector3(vec.x, vec.y, vec.z + ((root.localPosition.z - ejectPoint.localPosition.z) / 2));
        var limit = new SoftJointLimit();
        limit.limit = Vector3.Distance(ejectPoint.position, root.position) / 2;
        _joint.linearLimit = limit;

        axis.SetParent(rigidBody.transform);
        axis.localPosition = Vector3.zero;
        axis.localEulerAngles = Vector3.zero;

        _ejectedSinceLastOpen = false;
    }

    private void Revolver_onClose()
    {
        var vec = BoltBase.GrandparentLocalPosition(root, revolver.rotateBody.transform);
        _joint.anchor = new Vector3(vec.x, vec.y, vec.z);
        var limit = new SoftJointLimit();
        limit.limit = 0f;
        _joint.linearLimit = limit;

        axis.SetParent(root);
        axis.localPosition = Vector3.zero;
        axis.localEulerAngles = Vector3.zero;
    }

    public void InitializeJoint()
    {
        if (_joint == null)
        {
            _joint = revolver.rotateBody.gameObject.AddComponent<ConfigurableJoint>();
            rigidBody.transform.SetLocalPositionAndRotation(root.localPosition, root.localRotation);
            _joint.connectedBody = rigidBody;
            _joint.massScale = 0.00001f;

            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = Vector3.zero;
            _joint.xMotion = ConfigurableJointMotion.Locked;
            _joint.yMotion = ConfigurableJointMotion.Locked;
            _joint.zMotion = ConfigurableJointMotion.Limited;
            _joint.angularXMotion = ConfigurableJointMotion.Locked;
            _joint.angularYMotion = ConfigurableJointMotion.Locked;
            _joint.angularZMotion = ConfigurableJointMotion.Locked;
            rigidBody.transform.localPosition = root.localPosition;
            rigidBody.transform.localRotation = root.localRotation;
        }
    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(rigidBody.position, ejectPoint.position) <= 0.001f && !_ejectedSinceLastOpen)
        {
            _ejectedSinceLastOpen = true;
            revolver.EjectCasings();
        }
    }
}