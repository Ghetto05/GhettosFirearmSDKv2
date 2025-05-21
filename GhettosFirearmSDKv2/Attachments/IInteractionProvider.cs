using ThunderRoad;

namespace GhettosFirearmSDKv2.Attachments;

public interface IInteractionProvider
{
    public delegate void Teardown();
    public event Teardown OnTeardown;
    
    public delegate void Collision(UnityEngine.Collision collision);
    public event Collision OnCollision;

    public delegate void HeldAction(HeldActionData e);
    public event HeldAction OnHeldAction;
    public event HeldAction OnUnhandledHeldAction;

    public class HeldActionData
    {
        public HeldActionData(RagdollHand handler, Handle handle, Interactable.Action action)
        {
            Handler = handler;
            Handle = handle;
            Action = action;
        }

        public readonly RagdollHand Handler;
        public readonly Handle Handle;
        public readonly Interactable.Action Action;
        public bool Handled;

        public override string ToString()
        {
            return $"Action: {Action} Handle: {Handle.name}";
        }
    }
}