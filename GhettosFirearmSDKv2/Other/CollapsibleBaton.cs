namespace GhettosFirearmSDKv2;

public class CollapsibleBaton : StateTogglerWithAnimation
{
    public float threshold = 23f;

    private void FixedUpdate()
    {
        if (currentState == 1 && item.physicBody.angularVelocity.magnitude > threshold)
        {
            TryToggle();
        }
    }
}