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

    private SaveNodeValueInt _triggerChannelSaveData;
    private SaveNodeValueInt _altUseChannelSaveData;
    private SaveNodeValueBool _useAltUseSaveData;

    protected void OnInvokedStart()
    {
        _triggerChannelSaveData = GetNode().GetOrAddValue("PressureSwitchChannel_Trigger", new SaveNodeValueInt { Value = triggerChannel });
        _altUseChannelSaveData = GetNode().GetOrAddValue("PressureSwitchChannel_AlternateUse", new SaveNodeValueInt { Value = alternateUseChannel });
        _useAltUseSaveData = GetNode().GetOrAddValue("PressureSwitchChannel_UseAlternateUse", new SaveNodeValueBool() { Value = useAltUse });
    }

    public bool Active(int channel)
    {
        return ((channel == triggerChannel || masterSwitch) && TriggerState) || ((channel == alternateUseChannel || masterSwitch) && AlternateUseState);
    }

    public void SetTriggerChannel(int channel)
    {
        _triggerChannelSaveData.Value = channel;
        triggerChannel = channel;
    }

    public void SetAltUseChannel(int channel)
    {
        _altUseChannelSaveData.Value = channel;
        alternateUseChannel = channel;
    }

    public void SetUseAltUse(bool use)
    {
        _useAltUseSaveData.Value = use;
        useAltUse = use;
    }

    public virtual FirearmSaveData.AttachmentTreeNode GetNode()
    {
        return null;
    }
}