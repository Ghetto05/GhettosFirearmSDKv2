using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2
{
    public class StockToggler : MonoBehaviour
    {
        public AudioSource toggleSound;
        public Handle toggleHandle;
        public Transform pivot;
        public Transform[] positions;
        [FormerlySerializedAs("connectedFirearm"), SerializeField, SerializeReference]
        public Firearm connectedManager;
        public Attachment connectedAttachment;
        public int currentIndex;
        public bool useAsSeparateObjects;
        private SaveNodeValueInt _stockPosition;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            if (connectedManager != null)
            {
                connectedManager.item.OnHeldActionEvent += OnAction;
                _stockPosition = connectedManager.SaveData.FirearmNode.GetOrAddValue("StockPosition" + name, new SaveNodeValueInt());
                currentIndex = _stockPosition.Value;
                ApplyPosition(_stockPosition.Value, false);
            }
            else if (connectedAttachment != null)
            {
                if (!connectedAttachment.initialized)
                    connectedAttachment.OnDelayedAttachEvent += ConnectedAttachment_OnDelayedAttachEvent;
                else
                    ConnectedAttachment_OnDelayedAttachEvent();
            }
        }

        private void ConnectedAttachment_OnDelayedAttachEvent()
        {
            connectedAttachment.OnHeldActionEvent += OnAction;
            _stockPosition = connectedAttachment.Node.GetOrAddValue("StockPosition" + name, new SaveNodeValueInt());
            currentIndex = _stockPosition.Value;
            ApplyPosition(_stockPosition.Value, false);
        }

        private void OnAction(RagdollHand hand, Handle handle, Interactable.Action action)
        {
            if (handle == toggleHandle && action == Interactable.Action.UseStart)
            {
                if (currentIndex + 1 == positions.Length)
                    currentIndex = 0;
                else
                    currentIndex++;
                ApplyPosition(currentIndex);
            }
            else if (handle == toggleHandle && action == Interactable.Action.AlternateUseStart)
            {
                if (currentIndex - 1 == -1)
                    currentIndex = positions.Length - 1;
                else
                    currentIndex--;
                ApplyPosition(currentIndex);
            }
            _stockPosition.Value = currentIndex;
        }

        public void ApplyPosition(int index, bool playSound = true)
        {
            try
            {
                if (toggleSound != null && playSound)
                    toggleSound.Play();
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
                    IEnumerable<Tuple<RagdollHand,HandlePose,float>> handlers = toggleHandle.handlers.Select(h => new Tuple<RagdollHand, HandlePose, float>(h, h.gripInfo.orientation, h.gripInfo.axisPosition)).ToList();
                    toggleHandle.Release();
                    foreach (var pair in handlers)
                    {
                        pair.Item1!.Grab(toggleHandle, pair.Item2, pair.Item3);
                    }
                }
            }
            catch (Exception)
            {
                if (connectedAttachment != null)
                    Debug.Log($"FAILED TO APPLY STOCK POSITION! Attachment {connectedAttachment.name} on firearm {connectedAttachment.attachmentPoint.parentManager.Transform.name}: Index {index}, list is {positions.Length} entries long!");
                else if (connectedManager != null)
                    Debug.Log($"FAILED TO APPLY STOCK POSITION! Firearm {connectedManager.name}: Index {index}, list is {positions.Length} entries long!");
            }
        }

        public delegate void OnToggle(int newIndex, bool playSound);
        public event OnToggle OnToggleEvent;
    }
}
