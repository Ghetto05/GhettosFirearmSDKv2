using UnityEngine;

namespace GhettosFirearmSDKv2;

public class FireRateAdjuster : MonoBehaviour
{
    public Attachment attachment;
    public float newFireRate;
    private float _oldFireRate;
    private FirearmBase _firearm;

    private void Awake()
    {
        if (attachment.initialized)
            Attachment_OnDelayedAttachEvent();
        else
            attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        attachment.OnDetachEvent += Attachment_OnDetachEvent;
    }

    private void Attachment_OnDetachEvent(bool despawnDetach)
    {
        if (_firearm)
            _firearm.roundsPerMinute = _oldFireRate;
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        if (attachment.attachmentPoint.ConnectedManager is not FirearmBase f)
            return;

        _firearm = f;
        _oldFireRate = _firearm.roundsPerMinute;
        _firearm.roundsPerMinute = newFireRate;
    }
}