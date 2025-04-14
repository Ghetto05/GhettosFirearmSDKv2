using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Attachments/Systems/Bipod")]
public class Bipod : MonoBehaviour
{
    public GameObject attachmentManager;
    public Attachment attachment;
    public Transform axis;
    public List<Transform> positions;
    public Handle toggleHandle;
    public AudioSource toggleSound;
    public int index;
    public Bipod[] requiredBipods;
    public Bipod[] requiredInactiveBipods;
    private IAttachmentManager _attachmentManager;

    private void Start()
    {
        if (attachmentManager)
        {
            _attachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
            _attachmentManager.OnHeldAction += OnHeldActionEvent;
        }
        else if (attachment)
        {
            attachment.OnHeldAction += OnHeldActionEvent;
        }
        ApplyPosition(false);
    }

    private void FixedUpdate()
    {
        if (toggleHandle.item.holder)
        {
            return;
        }

        if (requiredBipods.Any())
        {
            toggleHandle.SetTouch(requiredBipods.All(b => b.index != 0));
        }

        if (requiredInactiveBipods.Any())
        {
            toggleHandle.SetTouch(!requiredInactiveBipods.Any(b => b.index == 1));
        }
    }

    private void OnHeldActionEvent(IAttachmentManager.HeldActionData e)
    {
        if (e.Handle == toggleHandle && e.Action == Interactable.Action.UseStart)
        {
            ToggleUp();
            e.Handled = true;
        }
        else if (e.Handle == toggleHandle && e.Action == Interactable.Action.AlternateUseStart)
        {
            ToggleDown();
            e.Handled = true;
        }
    }

    public void ToggleUp()
    {
        if (index + 1 == positions.Count)
        {
            index = 0;
        }
        else
        {
            index++;
        }
        ApplyPosition();
    }

    public void ToggleDown()
    {
        if (index - 1 == -1)
        {
            index = positions.Count - 1;
        }
        else
        {
            index--;
        }
        ApplyPosition();
    }

    public void ApplyPosition(bool playSound = true)
    {
        if (playSound)
        {
            toggleSound?.Play();
        }
        axis.localPosition = positions[index].localPosition;
        axis.localEulerAngles = positions[index].localEulerAngles;
        ApplyPositionEvent?.Invoke();
    }

    public delegate void OnApplyPosition();

    public event OnApplyPosition ApplyPositionEvent;
}