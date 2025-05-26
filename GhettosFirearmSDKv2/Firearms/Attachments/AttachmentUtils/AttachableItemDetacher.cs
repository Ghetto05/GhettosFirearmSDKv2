using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AttachableItemDetacher : MonoBehaviour
{
    public Attachment attachment;
    public List<Handle> detachHandles;
    public List<AudioSource> detachSounds;
    public string itemId;

    private void Start()
    {
        if (!attachment)
        {
            Debug.LogError($"Attachment for AttachableItemDetacher on {GetComponentInParent<Attachment>()?.name} is not assigned!");
        }
        attachment.OnHeldAction += Attachment_OnHeldActionEvent;
    }

    private void Attachment_OnHeldActionEvent(IInteractionProvider.HeldActionData heldActionData)
    {
        if (detachHandles.Contains(heldActionData.Handle) && heldActionData.Action == Interactable.Action.AlternateUseStart)
        {
            heldActionData.Handled = true;
            var oldItem = attachment.attachmentPoint.ConnectedManager.Item;
            var node = attachment.Node.CloneJson();
            Util.SpawnItem(itemId, "Attachable Item Detach", item =>
            {
                Util.IgnoreCollision(item.gameObject, oldItem.gameObject, true);
                Util.DelayIgnoreCollision(item.gameObject, oldItem.gameObject, false, 1f, item);
                heldActionData.Handler.Grab(item.GetMainHandle(heldActionData.Handler.side));
                if (item.GetComponent<IAttachmentManager>() is { } firearm)
                {
                    firearm.SaveData = new FirearmSaveData
                                       {
                                           FirearmNode = node
                                       };
                    firearm.Item.AddCustomData(firearm.SaveData);
                }
                item.SetOwner(oldItem.owner);
            }, heldActionData.Handler.grip.position, heldActionData.Handler.grip.rotation);

            var s = Util.PlayRandomAudioSource(detachSounds);
            if (s)
            {
                s.transform.SetParent(heldActionData.Handler.transform);
                heldActionData.Handler.StartCoroutine(Explosive.DelayedDestroy(s.gameObject, s.clip.length + 1f));
            }

            attachment.handles.ForEach(h => h.Release());
            attachment.Detach();
        }
    }
}