using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class StateTogglerWithAnimation : MonoBehaviour
{
    public Interactable.Action toggleAction;
    public Handle handle;
    public Item item;
    public Animation animationPlayer;
    public int currentState;
    public string toState1Anim;
    public string toState2Anim;
    public AudioSource[] toState1Sounds;
    public AudioSource[] toState2Sounds;
    public Animator animator;

    private void Start()
    {
        item.OnHeldActionEvent += Item_OnHeldActionEvent;
    }

    private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle2, Interactable.Action action)
    {
        if (handle == handle2 && action == toggleAction)
        {
            TryToggle();
        }
    }

    public void TryToggle()
    {
        if (animationPlayer?.isPlaying ?? false)
        {
            return;
        }
        if (currentState == 1)
        {
            animationPlayer?.Play(toState2Anim);
            animator?.Play(toState2Anim);
            Util.PlayRandomAudioSource(toState2Sounds);
            currentState = 2;
        }
        else if (currentState == 2)
        {
            animationPlayer?.Play(toState1Anim);
            animator?.Play(toState1Anim);
            Util.PlayRandomAudioSource(toState1Sounds);
            currentState = 1;
        }
    }
}