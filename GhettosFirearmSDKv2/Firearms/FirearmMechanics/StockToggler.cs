using System;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class StockToggler : MonoBehaviour
{
    public GameObject attachmentManager;
    public Attachment connectedAttachment;

    public AudioSource toggleSound;
    public Handle toggleHandle;
    public Transform pivot;
    public Transform[] positions;
    public int currentIndex;
    public bool useAsSeparateObjects;
    private SaveNodeValueInt _stockPosition;
    private IAttachmentManager _attachmentManager;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
        if (attachmentManager)
        {
            _attachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
        }
        if (connectedAttachment)
        {
            _attachmentManager = connectedAttachment.attachmentPoint.ConnectedManager;
        }
    }

    public void InvokedStart()
    {
        if (attachmentManager)
        {
            _attachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
            OnDelayedAttach();
        }
        else if (connectedAttachment)
        {
            if (!connectedAttachment.initialized)
            {
                connectedAttachment.OnDelayedAttachEvent += OnDelayedAttach;
            }
            else
            {
                OnDelayedAttach();
            }
        }
    }

    private void OnDelayedAttach()
    {
        _attachmentManager.OnHeldAction += OnAction;
        _stockPosition = connectedAttachment.Node.GetOrAddValue("StockPosition" + name, new SaveNodeValueInt());
        currentIndex = _stockPosition.Value;
        ApplyPosition(_stockPosition.Value, false);
    }

    private void OnAction(IAttachmentManager.HeldActionData e)
    {
        Debug.Log(e.ToString());
        if (e.Handle == toggleHandle && e.Action == Interactable.Action.UseStart)
        {
            if (currentIndex + 1 == positions.Length)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex++;
            }
            ApplyPosition(currentIndex);
            e.Handled = true;
        }
        else if (e.Handle == toggleHandle && e.Action == Interactable.Action.AlternateUseStart)
        {
            if (currentIndex - 1 == -1)
            {
                currentIndex = positions.Length - 1;
            }
            else
            {
                currentIndex--;
            }
            ApplyPosition(currentIndex);
            e.Handled = true;
        }
        _stockPosition.Value = currentIndex;
    }

    public void ApplyPosition(int index, bool playSound = true)
    {
        try
        {
            if (toggleSound && playSound)
            {
                toggleSound.Play();
            }
            if (!useAsSeparateObjects)
            {
                pivot.localPosition = positions[index].localPosition;
                pivot.localEulerAngles = positions[index].localEulerAngles;
            }
            else
            {
                for (var i = 0; i < positions.Length; i++)
                {
                    positions[i].gameObject.SetActive(i == index);
                }
            }

            OnToggleEvent?.Invoke(index, playSound);

            if (toggleHandle.handlers.Any())
            {
                IEnumerable<Tuple<RagdollHand, HandlePose, float>> handlers = toggleHandle.handlers.Select(h => new Tuple<RagdollHand, HandlePose, float>(h, h.gripInfo.orientation, h.gripInfo.axisPosition)).ToList();
                toggleHandle.Release();
                foreach (var pair in handlers)
                {
                    pair.Item1!.Grab(toggleHandle, pair.Item2, pair.Item3);
                }
            }
        }
        catch (Exception)
        {
            if (connectedAttachment)
            {
                Debug.Log($"FAILED TO APPLY STOCK POSITION! Attachment {connectedAttachment.name} on firearm {connectedAttachment.attachmentPoint.ConnectedManager.Transform.name}: Index {index}, list is {positions.Length} entries long!");
            }
            else if (_attachmentManager is not null)
            {
                Debug.Log($"FAILED TO APPLY STOCK POSITION! Firearm {_attachmentManager.Transform.name}: Index {index}, list is {positions.Length} entries long!");
            }
        }
    }

    public delegate void OnToggle(int newIndex, bool playSound);

    public event OnToggle OnToggleEvent;
}