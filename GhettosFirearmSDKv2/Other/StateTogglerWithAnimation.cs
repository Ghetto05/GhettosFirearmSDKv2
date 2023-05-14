using System;
using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class StateTogglerWithAnimation : MonoBehaviour
    {
        public enum Actions
        {
            TriggerPull,
            TriggerRelease,
            AlternateUsePress,
            AlternateUseRelease
        }

        public Actions toggleAction;

        public Interactable.Action toggleActionBAS;
        public Handle handle;
        public Item item;
        public Animation animationPlayer;
        public int currentState;
        public string toState1Anim;
        public string toState2Anim;
        public AudioSource[] toState1Sounds;
        public AudioSource[] toState2Sounds;

        void Start()
        {
            if (toggleAction == Actions.TriggerPull)
            {
                toggleActionBAS = Interactable.Action.UseStart;
            }
            else if (toggleAction == Actions.TriggerRelease)
            {
                toggleActionBAS = Interactable.Action.UseStop;
            }
            else if (toggleAction == Actions.AlternateUsePress)
            {
                toggleActionBAS = Interactable.Action.AlternateUseStart;
            }
            else if (toggleAction == Actions.AlternateUseRelease)
            {
                toggleActionBAS = Interactable.Action.AlternateUseStop;
            }

            item.OnHeldActionEvent += Item_OnHeldActionEvent;
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle2, Interactable.Action action)
        {
            if (handle == handle2 && action == toggleActionBAS)
            {
                TryToggle();
            }
        }

        public void TryToggle()
        {
            if (animationPlayer.isPlaying) return;
            if (currentState == 1)
            {
                animationPlayer.Play(toState2Anim);
                Util.PlayRandomAudioSource(toState2Sounds);
                currentState = 2;
            }
            else if (currentState == 2)
            {
                animationPlayer.Play(toState1Anim);
                Util.PlayRandomAudioSource(toState1Sounds);
                currentState = 1;
            }
        }
    }
}
