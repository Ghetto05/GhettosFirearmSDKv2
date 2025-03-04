using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class BoltReleaseButton : MonoBehaviour
{
    public FirearmBase firearm;
    public bool caught;
    public Transform button;
    public Transform uncaughtPosition;
    public Transform caughtPosition;
    public Collider release;

    private void Start()
    {
        firearm.OnCollisionEvent += Firearm_OnCollisionEvent;
    }

    private void Firearm_OnCollisionEvent(Collision collision)
    {
        OnCollisionEnter(collision);
    }

    private void Update()
    {
        if (button != null)
        {
            if (caught)
            {
                button.localPosition = caughtPosition.localPosition;
                button.localRotation = caughtPosition.localRotation;
            }
            else
            {
                button.localPosition = uncaughtPosition.localPosition;
                button.localRotation = uncaughtPosition.localRotation;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (release == null) return;
        if (collision.contacts[0].thisCollider == release && collision.contacts[0].otherCollider.GetComponentInParent<Player>() != null)
        {
            OnReleaseEvent?.Invoke(true);
        }
    }

    public delegate void OnReleaseDelegate(bool forced = false);
    public event OnReleaseDelegate OnReleaseEvent;
}