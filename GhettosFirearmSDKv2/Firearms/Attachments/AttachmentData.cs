using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class AttachmentData : CustomData
    {
        public string displayName;
        public string type;
        public string prefabAddress;
        public string iconAddress;
        public string categoryName = "Default";

        public string GetID()
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return "Default";
            else return categoryName;
        }

        public static List<AttachmentData> AllOfType(string requestedType)
        {
            List<AttachmentData> dataList = new List<AttachmentData>();

            foreach (AttachmentData d in Catalog.GetDataList<AttachmentData>())
            {
                if (d.type.Equals(requestedType))
                {
                    dataList.Add(d);
                }
            }

            return dataList;
        }

        public void SpawnAndAttach(AttachmentPoint point, FirearmSaveData.AttachmentTreeNode thisNode = null, bool initialSetup = false)
        {
            Addressables.InstantiateAsync(prefabAddress, point.transform.position, point.transform.rotation, point.transform, false).Completed += (handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Attachment attachment = handle.Result.GetComponent<Attachment>();
                    point.currentAttachment = attachment;
                    attachment.data = this;
                    attachment.attachmentPoint = point;
                    if (thisNode == null)
                    {
                        attachment.node = new FirearmSaveData.AttachmentTreeNode();
                        attachment.node.attachmentId = id;
                        attachment.node.slot = point.id;
                    }
                    else attachment.node = thisNode;
                    if (point.attachment != null && thisNode == null) point.attachment.node.childs.Add(attachment.node);
                    else if (thisNode == null) point.parentFirearm.saveData.firearmNode.childs.Add(attachment.node);
                    attachment.Initialize(thisNode, initialSetup);
                    point.InvokeAttachmentAdded(attachment);
                }
                else
                {
                    Debug.LogWarning("Unable to instantiate attachment " + id + " from address " + prefabAddress);
                    Addressables.ReleaseInstance(handle);
                }
            });
        }
    }
}
