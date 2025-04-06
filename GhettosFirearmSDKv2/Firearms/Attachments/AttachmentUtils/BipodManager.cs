using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Attachments/Systems/Bipod Manager")]
public class BipodManager : MonoBehaviour
{
    public Firearm firearm;
    public AttachmentManager attachmentManager;
    public IAttachmentManager ConnectedManager;
    public Attachment attachment;

    public List<Bipod> bipods;
    public List<Transform> groundFollowers;
    public float linearRecoilModifier;
    public float muzzleRiseModifier;
    private bool _lastFrameOverriden;
    private bool _active;

    private void Start()
    {
        if (firearm)
        {
            ConnectedManager = firearm;
        }
        if (attachmentManager)
        {
            ConnectedManager = attachmentManager;
        }

        if (ConnectedManager is not null && attachment)
        {
            attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
        }
        else if (ConnectedManager is not null)
        {
            _active = true;
        }
    }

    private void Attachment_OnDelayedAttachEvent()
    {
        ConnectedManager = attachment.attachmentPoint.ConnectedManager;
        _active = true;
    }

    private void FixedUpdate()
    {
        if (!_active)
        {
            return;
        }
        var extended = !bipods.Any(x => x.index == 0);
        if (groundFollowers.Any(t => !Physics.Raycast(t.position, t.forward, 0.1f, LayerMask.GetMask("Default"))))
        {
            extended = false;
        }

        if (ConnectedManager is Firearm f)
        {
            switch (extended)
            {
                case true when !_lastFrameOverriden:
                    f.AddRecoilModifier(linearRecoilModifier, muzzleRiseModifier, this);
                    break;

                case false when _lastFrameOverriden:
                    f.RemoveRecoilModifier(this);
                    break;
            }
        }

        _lastFrameOverriden = extended;
    }
}