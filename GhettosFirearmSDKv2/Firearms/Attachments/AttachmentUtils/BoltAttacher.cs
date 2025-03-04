using System;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class BoltAttacher : MonoBehaviour
{
    public FirearmBase firearm;
    public Attachment attachment;
    public Transform boltChild;
    public Transform boltRbChild;
    public List<GhettoHandle> additionalBoltHandles;
    public bool disableDefaultBoltHandles;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    private void InvokedStart()
    {
        if (attachment.attachmentPoint.ConnectedManager is not FirearmBase f)
            return;
        firearm = f;
        attachment.OnDetachEvent += AttachmentOnOnDetachEvent;
        if (GetParent() is { } p)
        {
            boltChild?.SetParent(p);
        }
        if (GetParentRb() is { } rb)
        {
            boltRbChild?.SetParent(rb);
        }
        SetHandles();
    }

    private void AttachmentOnOnDetachEvent(bool despawnDetach)
    {
        if (disableDefaultBoltHandles)
            ResetHandles();
        try
        {
            Destroy(boltChild?.gameObject);
            Destroy(boltRbChild?.gameObject);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private Transform GetParent()
    {
        var bolt = firearm.bolt;
        if (bolt == null)
            return null;

        var type = bolt.GetType();
        if (type == typeof(BoltSemiautomatic))
            return ((BoltSemiautomatic)bolt).bolt;
        if (type == typeof(PumpAction))
            return ((PumpAction)bolt).bolt;

        return null;
    }

    private Transform GetParentRb()
    {
        var bolt = firearm.bolt;
        if (bolt == null)
            return null;

        var type = bolt.GetType();
        if (type == typeof(BoltSemiautomatic))
            return ((BoltSemiautomatic)bolt).rigidBody.transform;
        if (type == typeof(PumpAction))
            return ((PumpAction)bolt).rb.transform;

        return null;
    }

    private void SetHandles()
    {
        var bolt = firearm.bolt;
        if (bolt == null)
            return;

        var type = bolt.GetType();
        if (type == typeof(BoltSemiautomatic))
            SetAutomaticHandles((BoltSemiautomatic)bolt);
        if (type == typeof(PumpAction))
            SetPumpHandles((PumpAction)bolt);
    }

    private void ResetHandles()
    {
        var bolt = firearm.bolt;
        if (bolt == null)
            return;

        var type = bolt.GetType();
        if (type == typeof(BoltSemiautomatic))
            ResetAutomaticHandles((BoltSemiautomatic)bolt);
        if (type == typeof(PumpAction))
            ResetPumpHandles((PumpAction)bolt);
    }

    private void SetAutomaticHandles(BoltSemiautomatic bolt)
    {
        foreach (var h in additionalBoltHandles)
        {
            h.customRigidBody = bolt.rigidBody;
            h.type = GhettoHandle.HandleType.Bolt;
        }
        bolt.UpdateBoltHandles();
            
        if (disableDefaultBoltHandles)
            foreach (var handle in bolt.boltHandles)
            {
                handle.SetTouch(false);
            }
    }
        
    private void ResetAutomaticHandles(BoltSemiautomatic bolt)
    {
        foreach (var handle in bolt.boltHandles)
        {
            handle.SetTouch(true);
        }
    }

    private void SetPumpHandles(PumpAction bolt)
    {
        foreach (var h in additionalBoltHandles)
        {
            h.customRigidBody = bolt.rb;
            h.type = GhettoHandle.HandleType.Bolt;
        }
        bolt.RefreshBoltHandles();
            
        if (disableDefaultBoltHandles)
            foreach (var handle in bolt.boltHandles)
            {
                handle.SetTouch(false);
            }
    }
        
    private void ResetPumpHandles(PumpAction bolt)
    {
        foreach (var handle in bolt.boltHandles)
        {
            handle.SetTouch(true);
        }
    }
}