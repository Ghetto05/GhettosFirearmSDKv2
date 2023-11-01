using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Locks/Gate lock")]
    public class GateLock : Lock
    {
        public Item item;
        public Attachment attachment;
        public List<Handle> handles;

        public Transform gate;
        public Transform locked;
        public Transform unlocked;

        public List<AudioSource> openSounds;
        public List<AudioSource> closeSounds;

        bool state = true;

        public override bool GetState()
        {
            return state;
        }

        private void Start()
        {
            if (item != null) item.OnHeldActionEvent += OnHeldActionEvent;
            else if (attachment != null) attachment.OnHeldActionEvent += OnHeldActionEvent;
            Toggle(true);
        }

        private void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handles.Contains(handle) && action == Interactable.Action.AlternateUseStart) Toggle();
        }

        public void Toggle(bool initial = false)
        {
            if (!initial) state = !state;
            Transform t = !state ? locked : unlocked;
            gate.SetLocalPositionAndRotation(t.localPosition, t.localRotation);
            if (!initial) Util.PlayRandomAudioSource(state ? openSounds : closeSounds);
        }
    }
}
