using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class FirearmSaveData : ContentCustomData
    {
        public AttachmentTreeNode firearmNode;

        public void ApplyToFirearm(Firearm firearm)
        {
            foreach (var node in firearmNode.childs)
            {
                var point = firearm.GetSlotFromId(node.slot);
                Catalog.GetData<AttachmentData>(Util.GetSubstituteId(node.attachmentId, $"[Firearm save data - slot {node.slot} on {firearm.item?.data?.displayName} | {firearm.item?.itemId}]"))?.SpawnAndAttach(point, null, node, true);
            }
        }

        public static AttachmentTreeNode GetNode(FirearmBase firearm)
        {
            if (firearm.GetType() == typeof(Firearm))
            {
                var f = (Firearm)firearm;
                return f.saveData.firearmNode;
            }
            else
            {
                var f = (AttachmentFirearm)firearm;
                return f.attachment.Node;
            }
        }

        public class AttachmentTreeNode
        {
            public string slot;
            public int slotPosition;
            public string attachmentId;
            public List<AttachmentTreeNode> childs;
            public List<SaveNodeValue> values;

            public AttachmentTreeNode()
            {
                childs = new List<AttachmentTreeNode>();
                values = new List<SaveNodeValue>();
            }

            public bool TryGetValue<T>(string id, out T value) where T : SaveNodeValue
            {
                foreach (var v in values)
                {
                    if (v != null && v.id.Equals(id))
                    {
                        value = v as T;
                        return true;
                    }
                }
                value = default(T);
                return false;
            }

            public T GetOrAddValue<T>(string id, T newObject) where T : SaveNodeValue
            {
                SaveNodeValue value = GetOrAddValue(id, newObject, out var addedNew);
                return value as T;
            }
            
            public T GetOrAddValue<T>(string id, T newObject, out bool addedNew) where T : SaveNodeValue
            {
                SaveNodeValue value;
                if (TryGetValue(id, out value))
                {
                    addedNew = false;
                    return value as T;
                }
                else
                {
                    addedNew = true;
                    value = newObject;
                    value.id = id;
                    values.Add(value as T);
                }
                return value as T;
            }

            public void RemoveValue(string id)
            {
                SaveNodeValue vtd = null;
                foreach (var v in values)
                {
                    if (v != null && v.id.Equals(id))
                    {
                        vtd = v;
                    }
                }
                if (vtd != null) values.Remove(vtd);
            }
        }
    }
}
