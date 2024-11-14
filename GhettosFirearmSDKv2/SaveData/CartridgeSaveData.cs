using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class CartridgeSaveData : ContentCustomData
{
    public string ItemId;
    public bool IsFired;

    public CartridgeSaveData(string itemId, bool isFired)
    {
        ItemId = itemId;
        IsFired = isFired;
    }

    public void Apply(Cartridge cartridge)
    {
        if (IsFired)
            cartridge.SetFired();
    }
}