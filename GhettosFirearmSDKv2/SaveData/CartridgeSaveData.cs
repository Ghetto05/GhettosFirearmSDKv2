using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class CartridgeSaveData : ContentCustomData
{
    public string ItemId;
    public bool IsFired;
    public bool Failed;
    public List<ContentCustomData> CustomData;

    public CartridgeSaveData(string itemId, bool? isFired, bool? failed, List<ContentCustomData> customData)
    {
        ItemId = itemId;
        IsFired = isFired ?? false;
        Failed = failed ?? false;
        CustomData = customData?.CloneJson() ?? [];
    }

    public void Apply(Cartridge cartridge)
    {
        if (Failed)
        {
            cartridge.Failed = true;
        }
        if (IsFired)
        {
            cartridge.SetFired();
        }
        cartridge.item.OverrideCustomData(CustomData.CloneJson());
    }

    public static implicit operator CartridgeSaveData(string item)
    {
        return new CartridgeSaveData(item, false, false, null);
    }
}