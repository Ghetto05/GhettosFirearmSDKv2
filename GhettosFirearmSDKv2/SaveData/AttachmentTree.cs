using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2.SaveData
{
    public class AttachmentTree : ContentCustomData
    {
        public List<Node> firearmAttachments;

        public void ApplyToFirearm(FirearmBase firearm)
        {
            if (firearmAttachments == null) return;
            foreach (Node node in firearmAttachments)
            {
                AttachmentPoint point = firearm.GetSlotFromId(node.slot);
                Catalog.GetData<AttachmentData>(node.attachmentId).SpawnAndAttach(point, node, true);
            }
        }

        public void GetFromFirearm(FirearmBase firearm)
        {
            firearmAttachments = new List<Node>();
            foreach (AttachmentPoint ap in firearm.attachmentPoints)
            {
                Node n = AddAttachmentPoint(ap);
                if (n != null) firearmAttachments.Add(n);
            }
        }

        private Node AddAttachmentPoint(AttachmentPoint attachmentPoint)
        {
            if (attachmentPoint.currentAttachment != null)
            {
                Node node = new Node();
                node.childs = new List<Node>();
                node.attachmentId = attachmentPoint.currentAttachment.data.id;
                node.slot = attachmentPoint.id;
                foreach (AttachmentPoint ap in attachmentPoint.currentAttachment.attachmentPoints)
                {
                    Node n = AddAttachmentPoint(ap);
                    if (n != null) node.childs.Add(n);
                }
                return node;
            }
            else return null;
        }

        //A node is equals to an attachment
        public class Node
        {
            public string slot;
            public string attachmentId;
            public List<Node> childs;

            public Node()
            {
                childs = new List<Node>();
            }
        }
    }
}
