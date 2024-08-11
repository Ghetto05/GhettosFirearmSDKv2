using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class RequiredPenetrationPowerData : CustomData
    {
        public string materialID;
        public string penetrationPower;
        public ProjectileData.PenetrationLevels level;

        public static ProjectileData.PenetrationLevels GetRequiredLevel(string materialId)
        {
            foreach (var rppd in Catalog.GetDataList<RequiredPenetrationPowerData>())
            {
                if (rppd.materialID.Equals(materialId))
                {
                    return rppd.level;
                }
            }
            return ProjectileData.PenetrationLevels.None;
        }

        public override void OnCatalogRefresh()
        {
            if (penetrationPower.Equals("None")) level = ProjectileData.PenetrationLevels.None;
            else if (penetrationPower.Equals("Leather")) level = ProjectileData.PenetrationLevels.Leather;
            else if (penetrationPower.Equals("Plate")) level = ProjectileData.PenetrationLevels.Plate;
            else if (penetrationPower.Equals("Items")) level = ProjectileData.PenetrationLevels.Items;
            else if (penetrationPower.Equals("Kevlar")) level = ProjectileData.PenetrationLevels.Kevlar;
            else if (penetrationPower.Equals("World")) level = ProjectileData.PenetrationLevels.World;
            Debug.Log($"[Firearm SDK v2] Adding entry for physics material {materialID}: required power {(int)level} which is equals to {level}");

            base.OnCatalogRefresh();
        }

        public override int GetCurrentVersion()
        {
            return 1;
        }
    }
}
