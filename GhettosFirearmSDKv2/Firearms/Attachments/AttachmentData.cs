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

        public void SpawnAndAttach(AttachmentPoint point, SaveData.AttachmentTree.Node thisNode = null, bool initialSetup = false)
        {
            Addressables.InstantiateAsync(prefabAddress, point.transform.position, point.transform.rotation, point.transform, false).Completed += (System.Action<AsyncOperationHandle<GameObject>>)(handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Attachment attachment = handle.Result.GetComponent<Attachment>();
                    point.currentAttachment = attachment;
                    attachment.data = this;
                    attachment.attachmentPoint = point;
                    attachment.Initialize(thisNode, initialSetup);
                    point.InvokeAttachmentAdded(attachment);
                }
                else
                {
                    Debug.LogWarning((object)("Unable to instantiate attachment " + id + " from address " + this.prefabAddress));
                    Addressables.ReleaseInstance(handle);
                }
            });
        }
    }
}
