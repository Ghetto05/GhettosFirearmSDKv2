using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PumpAutomaticAdapter : MonoBehaviour
    {
        public enum SwitchType
        {
            BoltRelease,
            FireModeSwitch,
            AutoBoltTrigger,
            AutoBoltAlternate,
            PumpTrigger,
            PumpAlternate
        }

        public Item item;
        public SwitchType switchType;
        public PumpAction pumpAction;
        public BoltSemiautomatic automaticBolt;
        public Transform switchRoot;
        public Transform automaticPosition;
        public Transform pumpPosition;
        public AudioSource[] switchSounds;
        public bool pumpActionEngaged;
        private bool _hasBoltCatch;

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime * 2);
            pumpAction.fireOnTriggerPress = false;
        }

        private void InvokedStart()
        {
            item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
            item.OnDespawnEvent += ItemOnOnDespawnEvent;
            automaticBolt.firearm.OnAltActionEvent += FirearmOnOnAltActionEvent;
            _hasBoltCatch = automaticBolt.hasBoltcatch;
            if (pumpActionEngaged)
                Toggle(true);
        }

        private void ItemOnOnDespawnEvent(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
            {
                item.OnHeldActionEvent -= ItemOnOnHeldActionEvent;
                item.OnDespawnEvent -= ItemOnOnDespawnEvent;
                automaticBolt.firearm.OnAltActionEvent -= FirearmOnOnAltActionEvent;
            }
        }

        private void FirearmOnOnAltActionEvent(bool longPress)
        {
            if (switchType == SwitchType.BoltRelease && !longPress)
                Toggle();
            if (switchType == SwitchType.FireModeSwitch && longPress)
                Toggle();
        }

        private void ItemOnOnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            bool pumpHandle = pumpAction.boltHandles.Contains(handle);
            bool autoHandle = automaticBolt.boltHandles.Contains(handle);
            
            if (switchType == SwitchType.AutoBoltTrigger && autoHandle && action == Interactable.Action.UseStart)
                Toggle();
            if (switchType == SwitchType.AutoBoltAlternate && autoHandle && action == Interactable.Action.AlternateUseStart)
                Toggle();
            if (switchType == SwitchType.PumpTrigger && pumpHandle && action == Interactable.Action.UseStart)
                Toggle();
            if (switchType == SwitchType.PumpAlternate && pumpHandle && action == Interactable.Action.AlternateUseStart)
                Toggle();
        }

        public void Toggle(bool silent = false)
        {
            if (pumpAction.state != BoltBase.BoltState.Locked)
                return;
            
            pumpActionEngaged = !pumpActionEngaged;
            pumpAction.fireOnTriggerPress = pumpActionEngaged;

            pumpAction.Lock(true);
            foreach (Handle handle in automaticBolt.boltHandles)
            {
                handle.Release();
                handle.SetTouch(!pumpActionEngaged);
            }

            automaticBolt.hasBoltcatch = _hasBoltCatch && !pumpActionEngaged;
            automaticBolt.overrideHeldState = pumpActionEngaged;
            automaticBolt.heldState = true;
            
            if (!silent)
                Util.PlayRandomAudioSource(switchSounds);
            if (switchRoot != null)
            {
                Transform t = pumpActionEngaged ? pumpPosition : automaticPosition;
                switchRoot.SetPositionAndRotation(t.position, t.rotation);
            }
        }

        private void FixedUpdate()
        {
            if (!pumpActionEngaged)
                return;
            
            if (pumpActionEngaged && pumpAction.state == BoltBase.BoltState.Locked)
            {
                automaticBolt.bolt.localPosition = automaticBolt.startPoint.localPosition;
                automaticBolt.rigidBody.transform.localPosition = automaticBolt.startPoint.localPosition;
            }
            else
            {
                automaticBolt.bolt.localPosition = Vector3.Lerp(automaticBolt.startPoint.localPosition, automaticBolt.endPoint.localPosition, pumpAction.cyclePercentage);
                automaticBolt.rigidBody.transform.localPosition = Vector3.Lerp(automaticBolt.startPoint.localPosition, automaticBolt.endPoint.localPosition, pumpAction.cyclePercentage);
            }
        }
    }
}
