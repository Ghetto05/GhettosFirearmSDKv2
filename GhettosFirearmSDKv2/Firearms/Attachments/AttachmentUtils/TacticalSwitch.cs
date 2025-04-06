using UnityEngine;

namespace GhettosFirearmSDKv2;

public class TacticalSwitch : MonoBehaviour
{
    public bool masterSwitch;
    public bool dualMode;
    public bool useAltUse;
    public int triggerChannel = 1;
    public int alternateUseChannel = 2;
    public TacticalDevice exclusiveDevice;

    protected bool TriggerState;
    protected bool AlternateUseState;

    public bool Active(int channel)
    {
        return ((channel == triggerChannel || masterSwitch) && TriggerState) || ((channel == alternateUseChannel || masterSwitch) && AlternateUseState);
    }
}