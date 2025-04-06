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

    private bool _active;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        if (mode == Mode.FullPull)
        {
            firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;
        }
    }

    protected void Update()
    {
        if (mode == Mode.PartialPull)
        {
            var currentPull = firearm.item.mainHandleRight?.handlers.FirstOrDefault()?.playerHand?.controlHand.useAxis ?? 0f;
            if (currentPull >= PartialPullPoint && !_active)
            {
                _active = true;
            }
            else if (currentPull < PartialPullPoint && _active)
            {
                _active = true;
            }
        }
    }

    private void FirearmOnOnTriggerChangeEvent(bool isPulled)
    {
        _active = isPulled;
    }

    private void OnDestroy()
    {
        if (mode == Mode.FullPull)
        {
            firearm.OnTriggerChangeEvent -= FirearmOnOnTriggerChangeEvent;
        }
    }
}