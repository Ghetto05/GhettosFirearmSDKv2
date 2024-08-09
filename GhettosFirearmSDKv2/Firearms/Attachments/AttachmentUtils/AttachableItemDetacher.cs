using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AttachableItemDetacher : MonoBehaviour
    {
        public Attachment attachment;
        public List<Handle> detachHandles;
        public List<AudioSource> detachSounds;
        public string itemId;

        private void Start()
        {
            if (attachment == null)
                Debug.LogError($"Attachment for AttachableItemDetacher on {GetComponentInParent<Attachment>()?.name} is not assigned!");
            attachment.OnHeldActionEvent += Attachment_OnHeldActionEvent;
        }

        private void Attachment_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (detachHandles.Contains(handle) && action == Interactable.Action.AlternateUseStart)
            {
                Item oldItem = attachment.attachmentPoint.parentFirearm.item;
                Util.SpawnItem(itemId, "Attachable Item Detach",item =>
                {
                    Util.IgnoreCollision(item.gameObject, oldItem.gameObject, true);
                    Util.DelayIgnoreCollision(item.gameObject, oldItem.gameObject, false, 1f, item);
                    ragdollHand.Grab(item.GetMainHandle(ragdollHand.side));
                }, ragdollHand.grip.position, ragdollHand.grip.rotation);

                AudioSource s = Util.PlayRandomAudioSource(detachSounds);
                if (s != null)
                {
                    s.transform.SetParent(ragdollHand.transform);
                    ragdollHand.StartCoroutine(Explosives.Explosive.delayedDestroy(s.gameObject, s.clip.length + 1f));
                }

                attachment.handles.ForEach(h => h.Release());
                attachment.Detach();
            }
        }
    }
}
