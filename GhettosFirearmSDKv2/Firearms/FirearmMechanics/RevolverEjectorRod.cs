using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class RevolverEjectorRod : MonoBehaviour
    {
        public Revolver revolver;
        public Rigidbody rigidBody;
        public Transform axis;
        public Transform root;
        public Transform ejectPoint;
        private ConfigurableJoint joint;
        private bool ejectedSinceLastOpen = false;

        private void Start()
        {
            InitializeJoint();
            revolver.firearm.OnAttachmentAddedEvent += Firearm_OnAttachmentAddedEvent;
            revolver.OnClose += Revolver_onClose;
            revolver.OnOpen += Revolver_onOpen;
            revolver.useGravityEject = false;
            rigidBody.gameObject.AddComponent<CollisionRelay>().onCollisionEnterEvent += revolver.OnCollisionEvent;
            Util.IgnoreCollision(axis.gameObject, revolver.firearm.gameObject, true);
            Revolver_onClose();
        }

        private void Firearm_OnAttachmentAddedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            Util.IgnoreCollision(axis.gameObject, revolver.firearm.gameObject, true);
        }

        private void Revolver_onOpen()
        {
            Vector3 vec = BoltBase.GrandparentLocalPosition(ejectPoint, revolver.rotateBody.transform);
            joint.anchor = new Vector3(vec.x, vec.y, vec.z + ((root.localPosition.z - ejectPoint.localPosition.z) / 2));
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = Vector3.Distance(ejectPoint.position, root.position) / 2;
            joint.linearLimit = limit;

            axis.SetParent(rigidBody.transform);
            axis.localPosition = Vector3.zero;
            axis.localEulerAngles = Vector3.zero;

            ejectedSinceLastOpen = false;
        }

        private void Revolver_onClose()
        {
            Vector3 vec = BoltBase.GrandparentLocalPosition(root, revolver.rotateBody.transform);
            joint.anchor = new Vector3(vec.x, vec.y, vec.z);
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = 0f;
            joint.linearLimit = limit;

            axis.SetParent(root);
            axis.localPosition = Vector3.zero;
            axis.localEulerAngles = Vector3.zero;
        }

        public void InitializeJoint()
        {
            if (joint == null)
            {
                joint = revolver.rotateBody.gameObject.AddComponent<ConfigurableJoint>();
                rigidBody.transform.SetLocalPositionAndRotation(root.localPosition, root.localRotation);
                joint.connectedBody = rigidBody;
                joint.massScale = 0.00001f;

                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero;
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Limited;
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
                rigidBody.transform.localPosition = root.localPosition;
                rigidBody.transform.localRotation = root.localRotation;
            }
        }

        private void FixedUpdate()
        {
            if (Vector3.Distance(rigidBody.position, ejectPoint.position) <= 0.001f && !ejectedSinceLastOpen)
            {
                ejectedSinceLastOpen = true;
                revolver.EjectCasings();
            }
        }
    }
}
