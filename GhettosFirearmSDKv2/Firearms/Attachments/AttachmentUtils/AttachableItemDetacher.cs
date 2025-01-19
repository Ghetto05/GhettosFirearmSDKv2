using System.Collections.Generic;
using GhettosFirearmSDKv2.Explosives;
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
            if (!attachment)
                Debug.LogError($"Attachment for AttachableItemDetacher on {GetComponentInParent<Attachment>()?.name} is not assigned!");
            attachment.OnHeldActionEvent += Attachment_OnHeldActionEvent;
        }

        private void Attachment_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (detachHandles.Contains(handle) && action == Interactable.Action.AlternateUseStart)
            {
                var oldItem = attachment.attachmentPoint.parentFirearm.item;
                var node = attachment.Node.CloneJson();
                Util.SpawnItem(itemId, "Attachable Item Detach",item =>
                {
                    Util.IgnoreCollision(item.gameObject, oldItem.gameObject, true);
                    Util.DelayIgnoreCollision(item.gameObject, oldItem.gameObject, false, 1f, item);
                    ragdollHand.Grab(item.GetMainHandle(ragdollHand.side));
                    if (item.GetComponent<Firearm>() is { } firearm)
                    {
                        firearm.SaveData = new FirearmSaveData();
                        firearm.SaveData.FirearmNode = node;
                        firearm.GetComponent<Item>().AddCustomData(firearm.SaveData);
                    }
                    item.SetOwner(oldItem.owner);
                }, ragdollHand.grip.position, ragdollHand.grip.rotation);

                var s = Util.PlayRandomAudioSource(detachSounds);
                if (s != null)
                {
                    s.transform.SetParent(ragdollHand.transform);
                    ragdollHand.StartCoroutine(Explosive.DelayedDestroy(s.gameObject, s.clip.length + 1f));
                }

                attachment.handles.ForEach(h => h.Release());
                attachment.Detach();
            }
        }
    }
}
