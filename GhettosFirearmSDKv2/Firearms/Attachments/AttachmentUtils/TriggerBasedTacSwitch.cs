using System.Linq;

namespace GhettosFirearmSDKv2;

public class TriggerBasedTacSwitch : TacticalSwitch
{
    public enum Mode
    {
        FullPull,
        PartialPull
    }

    private const float PartialPullPoint = 0.2f;

    public Firearm firearm;
    public Mode mode;
}