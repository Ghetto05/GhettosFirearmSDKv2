using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2;

public class AttachmentData : CustomData
{
    public string DisplayName;
    public string Type;
    public string PrefabAddress;
    public string IconAddress;
    public string CategoryName = "Default";
    public int RailLength = -1;
    public int ForwardClearance;
    public int RearwardClearance;
    public string[] Types = [];

    public string GetID()
    {
        if (string.IsNullOrWhiteSpace(CategoryName)) return "Default";

        return CategoryName;
    }

    public static List<AttachmentData> AllOfType(string requestedType, ICollection<string> alternateTypes)
    {
        return Catalog.GetDataList<AttachmentData>()
                      .Where(d => 
                          d.Type?.Equals(requestedType) == true ||
                          d.Types?.Contains(requestedType) == true ||
                          alternateTypes?.Contains(d.Type) == true ||
                          alternateTypes?.Any(x => d.Types.Contains(x)) == true)
                      .OrderBy(d => d.CategoryName)
                      .ThenBy(d => d.DisplayName)
                      .ToList();
    }

    public void SpawnAndAttach(AttachmentPoint point, int? railPosition = null, FirearmSaveData.AttachmentTreeNode thisNode = null, bool initialSetup = false)
    {
        SpawnAndAttach(point, _ => { }, railPosition, thisNode, initialSetup);
    }

    public void SpawnAndAttach(AttachmentPoint point, Action<Attachment> callback, int? railPosition = null, FirearmSaveData.AttachmentTreeNode thisNode = null, bool initialSetup = false)
    {
        if (point == null)
        {
            if (Settings.debugMode)
                Debug.LogError("Tried to attach attachment to no point!");
            return;
        }
            
        var target = !point.usesRail || RailLength == -1 ?
            point.transform :
            point.railSlots != null ?
                thisNode != null ?
                    point.railSlots[thisNode.SlotPosition] :
                    railPosition != null ?
                        point.railSlots[railPosition.Value] :
                        point.railSlots[0] :
                point.transform;

        if (point.usesRail && RailLength == -1)
        {
            foreach (var attachment in point.currentAttachments.ToList())
            {
                attachment.Detach();
            }
        }

        if (point.usesRail && target == point.transform && thisNode != null)
            Debug.LogError($"Couldn't use rail points on point '{point.name}' on attachment '{id}'!");
            
        Addressables.InstantiateAsync(PrefabAddress, target.position, target.rotation, target, false).Completed += (handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var attachment = handle.Result.GetComponent<Attachment>();
                attachment.AssetLoadHandle = handle;
                point.currentAttachments.Add(attachment);
                attachment.Data = this;
                attachment.attachmentPoint = point;
                if (thisNode == null)
                {
                    attachment.Node = new FirearmSaveData.AttachmentTreeNode();
                    attachment.Node.AttachmentId = id;
                    attachment.Node.Slot = point.id;
                    attachment.Node.SlotPosition = point.usesRail ? point.railSlots.IndexOf(target) : 0;
                }
                else
                    attachment.Node = thisNode;
                if (point.attachment && !point.attachment.Node.Childs.Contains(thisNode))
                    point.attachment.Node.Childs.Add(attachment.Node);
                else if (thisNode == null)
                    point.ConnectedManager.SaveData.FirearmNode.Childs.Add(attachment.Node);
                attachment.Initialize(callback, thisNode, initialSetup);
                point.InvokeAttachmentAdded(attachment);
            }
            else
            {
                Debug.LogWarning("Unable to instantiate attachment " + id + " from address " + PrefabAddress);
                Addressables.ReleaseInstance(handle);
            }
        });
    }
}