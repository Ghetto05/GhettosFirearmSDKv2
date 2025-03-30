using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class TacticalDevice : MonoBehaviour
{
    public int channel = 1;
    public Item item;
    public Attachment attachment;
    protected Item ActualItem;
    public bool physicalSwitch;

    protected bool TacSwitchActive
    {
        get
        {
            var switches = ActualItem.GetComponentsInChildren<PressureSwitch>();
            return !switches.Any() || switches.Any(x => x.Active(channel) && (!x.exclusiveDevice || x.exclusiveDevice == this));
        }
    }
}