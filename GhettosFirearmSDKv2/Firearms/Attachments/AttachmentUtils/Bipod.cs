using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Attachments/Systems/Bipod")]
public class Bipod : MonoBehaviour
{
    public Item item;
    public Attachment attachment;
    public Transform axis;
    public List<Transform> positions;
    public Handle toggleHandle;
    public AudioSource toggleSound;
    public int index;
    public Bipod[] requiredBipods;
    public Bipod[] requiredInactiveBipods;

    private void Start()
    {
        if (item != null)
            item.OnHeldActionEvent += OnHeldActionEvent;
        else if (attachment != null)
            attachment.OnHeldActionEvent += OnHeldActionEvent;
        ApplyPosition(false);
    }

    private void FixedUpdate()
    {
        if (toggleHandle.item.holder != null)
            return;
            
        if (requiredBipods.Any())
        {
            toggleHandle.SetTouch(requiredBipods.All(b => b.index != 0));
        }
            
        if (requiredInactiveBipods.Any())
        {
            toggleHandle.SetTouch(!requiredInactiveBipods.Any(b => b.index == 1));
        }
    }

    private void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (handle == toggleHandle && action == Interactable.Action.UseStart)
            ToggleUp();
        else if (handle == toggleHandle && action == Interactable.Action.AlternateUseStart)
            ToggleDown();
    }

    public void ToggleUp()
    {
        if (index + 1 == positions.Count)
            index = 0;
        else
            index++;
        ApplyPosition();
    }

    public void ToggleDown()
    {
        if (index - 1 == -1)
            index = positions.Count - 1;
        else
            index--;
        ApplyPosition();
    }

    public void ApplyPosition(bool playSound = true)
    {
        if (playSound)
            toggleSound?.Play();
        axis.localPosition = positions[index].localPosition;
        axis.localEulerAngles = positions[index].localEulerAngles;
        ApplyPositionEvent?.Invoke();
    }

    public delegate void OnApplyPosition();
    public event OnApplyPosition ApplyPositionEvent;
}