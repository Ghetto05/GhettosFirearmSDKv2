using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class AttachmentData : CustomData
    {
        public string displayName;
        public string type;
        public string prefabAddress;
        public string iconAddress;
        public string categoryName = "Default";
        public int railLength = 1;
        public int forwardClearance;
        public int rearwardClearance;

        public string GetID()
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return "Default";
            else return categoryName;
        }

        public static List<AttachmentData> AllOfType(string requestedType)
        {
            return Catalog.GetDataList<AttachmentData>().Where(d => d.type.Equals(requestedType)).OrderBy(d => d.displayName).ToList();
        }

        public void SpawnAndAttach(AttachmentPoint point, FirearmSaveData.AttachmentTreeNode thisNode = null, bool initialSetup = false)
        {
            if (point == null)
            {
                if (Settings.debugMode)
                    Debug.LogError("Tried to attach attachment to no point!");
                return;
            }

            var target = !point.usesRail ? point.transform : point.railSlots != null ? thisNode != null ? point.railSlots[thisNode.slotPosition] : point.railSlots[(point.railSlots.Count - 1) / 2] : point.transform;
            if (point.usesRail && target == point.transform&& thisNode != null)
                Debug.LogError($"Couldn't use rail points on point '{point.name}' on attachment '{id}'!");
            Addressables.InstantiateAsync(prefabAddress, target.position, target.rotation, target, false).Completed += (handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Attachment attachment = handle.Result.GetComponent<Attachment>();
                    attachment.AssetLoadHandle = handle;
                    point.currentAttachments.Add(attachment);
                    attachment.Data = this;
                    attachment.attachmentPoint = point;
                    if (thisNode == null)
                    {
                        attachment.Node = new FirearmSaveData.AttachmentTreeNode();
                        attachment.Node.attachmentId = id;
                        attachment.Node.slot = point.id;
                        attachment.Node.slotPosition = point.usesRail ? point.railSlots.IndexOf(target) : 0;
                    }
                    else
                        attachment.Node = thisNode;
                    if (point.attachment != null && thisNode == null)
                        point.attachment.Node.childs.Add(attachment.Node);
                    else if (thisNode == null)
                        point.parentFirearm.saveData.firearmNode.childs.Add(attachment.Node);
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
