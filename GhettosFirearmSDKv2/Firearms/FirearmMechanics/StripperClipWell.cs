using UnityEngine;

namespace GhettosFirearmSDKv2;

public class StripperClipWell : MonoBehaviour
{
    public string clipType;
    public MagazineWell magazineWell;
    public Transform mountPoint;
    public Collider mountCollider;
    public BoltBase bolt;
    public BoltBase.BoltState allowedState;
    public bool alwaysAllow;
    public StripperClip currentClip;
    public AttachmentPoint[] blockingAttachmentPoints;

    private void FixedUpdate()
    {
        if (!alwaysAllow && currentClip && bolt.state != allowedState)
        {
            currentClip.RemoveFromGun();
        }
    }
}