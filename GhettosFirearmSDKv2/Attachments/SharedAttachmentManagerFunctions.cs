using System.Collections.Generic;
using System.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2.Attachments;

public static class SharedAttachmentManagerFunctions
{
    public static void UpdateAttachments(IAttachmentManager manager)
    {
        manager.CurrentAttachments = [];
        AddAttachments(manager, manager.AttachmentPoints);
    }
    
    private static void AddAttachments(IAttachmentManager manager, List<AttachmentPoint> points)
    {
        foreach (var point in points.Where(x => x && x.currentAttachments.Any()))
        {
            manager.CurrentAttachments.AddRange(point.currentAttachments);
            AddAttachments(manager, point.currentAttachments.SelectMany(x => x.attachmentPoints).ToList());
        }
    }
    
    public static AttachmentPoint GetSlotFromId(IAttachmentManager manager, string id)
    {
        return manager.AttachmentPoints.FirstOrDefault(x => x.id.Equals(id));
    }

    public static void LoadAndApplyData(IAttachmentManager manager)
    {
        if (!manager.Item.TryGetCustomData(out FirearmSaveData data))
        {
            manager.SaveData = new FirearmSaveData
            {
                FirearmNode = new FirearmSaveData.AttachmentTreeNode()
            };
            manager.Item.AddCustomData(manager.SaveData);
        }
        else
        {
            manager.SaveData = data;
        }
        
        foreach (var ap in manager.AttachmentPoints)
        {
            ap.parentManager = manager;
        }

        manager.SaveData.ApplyToFirearm(manager);
    }

    public static void UpdateAllLightVolumeReceivers(IAttachmentManager manager, LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
    {
        foreach (var lvr in manager.Transform.GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != manager.Item.lightVolumeReceiver))
        {
            Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
        }
    }
}