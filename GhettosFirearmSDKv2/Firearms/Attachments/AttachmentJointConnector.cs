using System.Collections;
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
            foreach (Joint joint in joints)
            {
                joint.connectedBody = attachment.attachmentPoint.parentFirearm.item.rb;
                joint.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
    }
}
