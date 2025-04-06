using System.Collections.Generic;
using System.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class AttachmentFirearm : FirearmBase
{
    public Attachment attachment;
    public Handle fireHandle;

    public override void Start()
    {
        base.Start();
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public override void InvokedStart()
    {
        if (!disableMainFireHandle)
        {
            mainFireHandle = fireHandle;
        }
        item = attachment.GetComponentInParent<AttachmentPoint>().ConnectedManager.Item;
        attachment.attachmentPoint.ConnectedManager.OnCollision += OnCollisionEnter;
        attachment.attachmentPoint.ConnectedManager.Item.mainCollisionHandler.OnCollisionStartEvent += InvokeCollisionTR;
        attachment.OnHeldActionEvent += OnHeldActionEvent;
        item.OnSnapEvent += Item_OnSnapEvent;
        item.OnUnSnapEvent += Item_OnUnSnapEvent;
        item.OnGrabEvent += Item_OnGrabEvent;
        CalculateMuzzle();

        base.InvokedStart();
    }

    public override void Update()
    {
        base.Update();
        if (fireHandle && fireHandle.handlers.Count > 0 && setUpForHandPose)
        {
            fireHandle.handlers[0].poser.SetTargetWeight(Player.local.GetHand(fireHandle.handlers[0].side).controlHand.useAxis);
        }
    }

    private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
    {
        if (handle == fireHandle && bolt)
        {
            bolt.Initialize();
        }
    }

    public override void PlayMuzzleFlash(Cartridge cartridge)
    {
        if (defaultMuzzleFlash is { } mf)
        {
            mf.Play();
            StartCoroutine(PlayMuzzleFlashLight(cartridge));
        }
    }

    public override List<Handle> AllTriggerHandles()
    {
        var hs = new List<Handle>();
        hs.AddRange(additionalTriggerHandles);
        if (!fireHandle)
        {
            return hs;
        }
        hs.Add(fireHandle);
        return hs;
    }

    public override bool IsSuppressed()
    {
        if (integrallySuppressed)
        {
            return true;
        }
        return false;
    }

    public override void CalculateMuzzle()
    {
        //Transform t = hitscanMuzzle;
        //foreach (Attachment a in attachment.attachmentPoint.parentFirearm.allAttachments)
        //{
        //    if (a.minimumMuzzlePosition && Vector3.Distance(hitscanMuzzle.position, a.minimumMuzzlePosition.position) > Vector3.Distance(hitscanMuzzle.position, t.position)) t = a.minimumMuzzlePosition;
        //}
        //actualHitscanMuzzle = t;
        actualHitscanMuzzle = hitscanMuzzle;
        base.CalculateMuzzle();
    }

    public override bool HeldByAI()
    {
        return !(attachment.attachmentPoint.ConnectedManager.Item?.handlers?.FirstOrDefault()?.creature.isPlayer ?? true);
    }
}