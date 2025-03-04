using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class RequiredPenetrationPowerData : CustomData
{
    public string MaterialID;
    public string PenetrationPower;
    public ProjectileData.PenetrationLevels Level;

    public static ProjectileData.PenetrationLevels GetRequiredLevel(string materialId)
    {
        foreach (var rppd in Catalog.GetDataList<RequiredPenetrationPowerData>())
        {
            if (rppd.MaterialID.Equals(materialId))
            {
                return rppd.Level;
            }
        }
        return ProjectileData.PenetrationLevels.None;
    }

    public override void OnCatalogRefresh()
    {
        if (PenetrationPower.Equals("None")) Level = ProjectileData.PenetrationLevels.None;
        else if (PenetrationPower.Equals("Leather")) Level = ProjectileData.PenetrationLevels.Leather;
        else if (PenetrationPower.Equals("Plate")) Level = ProjectileData.PenetrationLevels.Plate;
        else if (PenetrationPower.Equals("Items")) Level = ProjectileData.PenetrationLevels.Items;
        else if (PenetrationPower.Equals("Kevlar")) Level = ProjectileData.PenetrationLevels.Kevlar;
        else if (PenetrationPower.Equals("World")) Level = ProjectileData.PenetrationLevels.World;
        Debug.Log($"[Firearm SDK v2] Adding entry for physics material {MaterialID}: required power {(int)Level} which is equals to {Level}");

        base.OnCatalogRefresh();
    }

    public override int GetCurrentVersion()
    {
        return 1;
    }
}