using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
public Transform ejectPoint;
public Transform ejectDir;
public float ejectForce;

public override TryFire()
{
Cartridge car = firearm.magazineWell.TryLoadRound();
if (car == null) return;
FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, car.data, out List<Vector3> hitpoints, out List<Vector3> trajectories)
firearm.PlayFireSound();
firearm.ApplyRecoil();
car.Fire(hitpoints, trajectories, firearm.actualHitscanMuzzle);
}
}
