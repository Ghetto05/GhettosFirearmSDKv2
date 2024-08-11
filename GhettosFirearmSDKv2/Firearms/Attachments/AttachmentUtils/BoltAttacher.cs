using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class BoltAttacher : MonoBehaviour
    {
        public Attachment attachment;
        public Transform boltChild;
        public Transform boltRBChild;
        public List<GhettoHandle> additionalBoltHandles;
        public bool disableDefaultBoltHandles;

        private void Start()
        {
            Debug.Log("Loading converter");
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        private void InvokedStart()
        {
            attachment.OnDetachEvent += AttachmentOnOnDetachEvent;
            if (GetParent() is { } p)
            {
                boltChild?.SetParent(p);
            }
            if (GetParentRB() is { } rb)
            {
                boltRBChild?.SetParent(rb);
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
                Destroy(boltRBChild?.gameObject);
            }
            catch (Exception)
            { }
        }

        private Transform GetParent()
        {
            BoltBase bolt = attachment.attachmentPoint.parentFirearm.bolt;
            if (bolt == null)
                return null;

            Type type = bolt.GetType();
            if (type == typeof(BoltSemiautomatic))
                return ((BoltSemiautomatic)bolt).bolt;
            if (type == typeof(PumpAction))
                return ((PumpAction)bolt).bolt;

            return null;
        }

        private Transform GetParentRB()
        {
            BoltBase bolt = attachment.attachmentPoint.parentFirearm.bolt;
            if (bolt == null)
                return null;

            Type type = bolt.GetType();
            if (type == typeof(BoltSemiautomatic))
                return ((BoltSemiautomatic)bolt).rigidBody.transform;
            if (type == typeof(PumpAction))
                return ((PumpAction)bolt).rb.transform;

            return null;
        }

        private void SetHandles()
        {
            BoltBase bolt = attachment.attachmentPoint.parentFirearm.bolt;
            if (bolt == null)
                return;

            Type type = bolt.GetType();
            if (type == typeof(BoltSemiautomatic))
                SetAutomaticHandles((BoltSemiautomatic)bolt);
            if (type == typeof(PumpAction))
                SetPumpHandles((PumpAction)bolt);
        }

        private void ResetHandles()
        {
            BoltBase bolt = attachment.attachmentPoint.parentFirearm.bolt;
            if (bolt == null)
                return;

            Type type = bolt.GetType();
            if (type == typeof(BoltSemiautomatic))
                ResetAutomaticHandles((BoltSemiautomatic)bolt);
            if (type == typeof(PumpAction))
                ResetPumpHandles((PumpAction)bolt);
        }

        private void SetAutomaticHandles(BoltSemiautomatic bolt)
        {
            foreach (GhettoHandle h in additionalBoltHandles)
            {
                h.customRigidBody = bolt.rigidBody;
                h.type = GhettoHandle.HandleType.Bolt;
            }
            bolt.UpdateBoltHandles();
            
            if (disableDefaultBoltHandles)
                foreach (Handle handle in bolt.boltHandles)
                {
                    handle.SetTouch(false);
                }
        }
        
        private void ResetAutomaticHandles(BoltSemiautomatic bolt)
        {
            foreach (Handle handle in bolt.boltHandles)
            {
                handle.SetTouch(true);
            }
        }

        private void SetPumpHandles(PumpAction bolt)
        {
            foreach (GhettoHandle h in additionalBoltHandles)
            {
                h.customRigidBody = bolt.rb;
                h.type = GhettoHandle.HandleType.Bolt;
            }
            bolt.RefreshBoltHandles();
            
            if (disableDefaultBoltHandles)
                foreach (Handle handle in bolt.boltHandles)
                {
                    handle.SetTouch(false);
                }
        }
        
        private void ResetPumpHandles(PumpAction bolt)
        {
            foreach (Handle handle in bolt.boltHandles)
            {
                handle.SetTouch(true);
            }
        }
    }
}