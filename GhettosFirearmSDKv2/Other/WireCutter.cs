using ThunderRoad;
using UnityEngine;

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
        private float _lastClipTime;

        private void Start()
        {
            item.OnHeldActionEvent += delegate(RagdollHand _, Handle _, Interactable.Action action) 
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
