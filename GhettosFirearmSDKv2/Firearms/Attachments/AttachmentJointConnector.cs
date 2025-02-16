using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AttachmentJointConnector : MonoBehaviour
    {
        public List<Joint> joints;
        public Attachment attachment;

        public void Connect()
        {
            foreach (var joint in joints)
            {
                joint.connectedBody = attachment.attachmentPoint.ConnectedManager.Item.physicBody.rigidBody;
                joint.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
    }
}
