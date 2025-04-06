using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class FirearmSaveData : ContentCustomData
{
    public AttachmentTreeNode FirearmNode;

    public void ApplyToFirearm(IAttachmentManager manager)
    {
        foreach (var node in FirearmNode.Childs)
        {
            var point = manager.GetSlotFromId(node.Slot);
            Catalog.GetData<AttachmentData>(Util.GetSubstituteId(node.AttachmentId, $"[Firearm save data - slot {node.Slot} on {manager.Item?.data?.displayName} | {manager.Item?.itemId}]"))?.SpawnAndAttach(point, null, node, true);
        }
    }

    public static AttachmentTreeNode GetNode(FirearmBase firearm)
    {
        if (firearm.GetType() == typeof(Firearm))
        {
            var f = (Firearm)firearm;
            return f.SaveData.FirearmNode;
        }
        else
        {
            var f = (AttachmentFirearm)firearm;
            return f.attachment.Node;
        }
    }

    public class AttachmentTreeNode
    {
        public string Slot;
        public int SlotPosition;
        public string AttachmentId;
        public List<AttachmentTreeNode> Childs;
        public List<SaveNodeValue> Values;

        public AttachmentTreeNode()
        {
            Childs = new List<AttachmentTreeNode>();
            Values = new List<SaveNodeValue>();
        }

        public bool TryGetValue<T>(string id, out T value) where T : SaveNodeValue
        {
            foreach (var v in Values)
            {
                if (v is not null && v.ID.Equals(id))
                {
                    value = v as T;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public T GetOrAddValue<T>(string id, T newObject) where T : SaveNodeValue
        {
            SaveNodeValue value = GetOrAddValue(id, newObject, out _);
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

            addedNew = true;
            value = newObject;
            value.ID = id;
            Values.Add(value as T);
            return value as T;
        }

        public void RemoveValue(string id)
        {
            SaveNodeValue vtd = null;
            foreach (var v in Values)
            {
                if (v?.ID?.Equals(id) == true)
                {
                    vtd = v;
                }
            }
            if (vtd is not null)
            {
                Values.Remove(vtd);
            }
        }
    }
}