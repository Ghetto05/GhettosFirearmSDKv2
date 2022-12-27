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

}
}
