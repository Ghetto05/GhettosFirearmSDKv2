using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class WireCutter : MonoBehaviour
    {
        public Item item;
        public Animator animator;
        public string clipAnimation;
        public float clipAnimationLength;
        public Transform clipRoot;
        public float clipRange = 0.02f;
        public AudioSource[] clipSounds;
        private float _lastClipTime = 0f;

        private void Start()
        {
            item.OnHeldActionEvent += delegate(RagdollHand hand, Handle handle, Interactable.Action action) 
            { 
                if (action == Interactable.Action.UseStart)
                    Clip();
            };
        }

        private void Clip()
        {
            if (Time.time - _lastClipTime < clipAnimationLength)
                return;
            _lastClipTime = Time.time;
            
            Util.PlayRandomAudioSource(clipSounds);
            animator.Play(clipAnimation);
            
            WireCutterCuttable.CutFound(clipRoot.position, clipRange);
        }
    }
}
