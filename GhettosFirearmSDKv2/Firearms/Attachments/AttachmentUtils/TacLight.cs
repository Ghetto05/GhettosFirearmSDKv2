using UnityEngine;

namespace GhettosFirearmSDKv2;

public class TacLight : TacticalDevice
{
    public GameObject lights;

    public void SetActive()
    {
        physicalSwitch = true;
    }

    public void SetNotActive()
    {
        physicalSwitch = false;
    }

    private void Update()
    {
        lights.SetActive(TacSwitchActive && physicalSwitch && (!ActualItem || !ActualItem.holder));
    }
}