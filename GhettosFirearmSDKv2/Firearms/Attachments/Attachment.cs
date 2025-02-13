﻿using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2
{
    public class Attachment : MonoBehaviour
    {
        public AsyncOperationHandle<GameObject>? AssetLoadHandle;
        
        public FirearmSaveData.AttachmentTreeNode Node;
        public List<Handle> additionalTriggerHandles;
        public AttachmentPoint attachmentPoint;
        public List<AttachmentPoint> attachmentPoints;
        public List<Handle> handles;
        public AttachmentData Data;
        public ColliderGroup colliderGroup;
        public List<ColliderGroup> alternateGroups;
        public List<string> alternateGroupsIds;
        [Space]
        public bool isSuppressing;
        public bool multiplyDamage;
        public float damageMultiplier;
        [FormerlySerializedAs("ColliderGroupId")]
        public string colliderGroupId = "PropMetal";
        public bool overridesMuzzleFlash;
        public ParticleSystem newFlash;
        public Transform minimumMuzzlePosition;
        public List<Damager> damagers;
        public List<string> damagerIds;
        public List<Renderer> nonLightVolumeRenderers;

        [FormerlySerializedAs("OnAttachEvents")]
        public List<UnityEvent> onAttachEvents;
        [FormerlySerializedAs("OnDetachEvents")]
        public List<UnityEvent> onDetachEvents;

        private List<Renderer> _renderers;
        private List<RevealDecal> _decals;

        public bool initialized;
        public bool addedByInitialSetup;

        private void Update()
        {
            if (attachmentPoint != null && attachmentPoint.parentManager != null && attachmentPoint.parentManager.Item != null)
                Hide(!attachmentPoint.parentManager.Item.renderers[0].enabled);
        }

        public void Hide(bool hidden)
        {
            if (_renderers == null) return;
            foreach (var ren in _renderers)
            {
                ren.enabled = !hidden;
            }
        }

        public void Initialize(Action<Attachment> callback, FirearmSaveData.AttachmentTreeNode thisNode = null, bool initialSetup = false)
        {
            Firearm firearm = null;
            if (attachmentPoint.parentManager is Firearm f)
                firearm = f;
            addedByInitialSetup = initialSetup;
            if (thisNode != null) Node = thisNode;
            _renderers = new List<Renderer>();
            _decals = new List<RevealDecal>();
            foreach (var ren in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!_renderers.Contains(ren)) _renderers.Add(ren);
                if (!nonLightVolumeRenderers.Contains(ren) && !attachmentPoint.parentManager.Item.renderers.Contains(ren)) attachmentPoint.parentManager.Item.renderers.Add(ren);
            }
            foreach (var dec in gameObject.GetComponentsInChildren<RevealDecal>(true))
            {
                if (!_decals.Contains(dec)) _decals.Add(dec);
                if (!attachmentPoint.parentManager.Item.revealDecals.Contains(dec)) attachmentPoint.parentManager.Item.revealDecals.Add(dec);
            }
            try { attachmentPoint.parentManager.Item.lightVolumeReceiver.SetRenderers(attachmentPoint.parentManager.Item.renderers); } catch { Debug.Log($"Setting renderers failed on {gameObject.name}"); }

            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var ap in attachmentPoints.Where(x => x))
            {
                ap.parentManager = attachmentPoint.parentManager;
                ap.attachment = this;
            }
            attachmentPoint.parentManager.UpdateAttachments();
            attachmentPoint.parentManager.Item.OnDespawnEvent += Item_OnDespawnEvent;

            if (Settings.debugMode)
            {
                foreach (var h in gameObject.GetComponentsInChildren<Handle>())
                {
                    if (h.GetType() != typeof(GhettoHandle)) Debug.LogWarning("Handle " + h.gameObject.name + " on attachment " + gameObject.name + " is not of type GhettoHandle!");
                    if (!handles.Contains(h)) Debug.LogWarning("Handle " + h.gameObject.name + " is not in the handle list of the attachment " + gameObject.name + "!");
                }
            }
            if (thisNode != null) ApplyNode();
            foreach (var eve in onAttachEvents)
            {
                eve.Invoke();
            }

            if (colliderGroup != null)
            {
                colliderGroup.Load(Catalog.GetData<ColliderGroupData>(colliderGroupId));
                attachmentPoint.parentManager.Item.colliderGroups.Add(colliderGroup);
                attachmentPoint.parentManager.Item.RefreshCollision();
                foreach (var c in colliderGroup.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentManager.Item.currentPhysicsLayer;
                }
            }
            foreach (var colll in alternateGroups.Where(x => x))
            {
                colll.Load(Catalog.GetData<ColliderGroupData>(alternateGroupsIds[alternateGroups.IndexOf(colll)]));
                attachmentPoint.parentManager.Item.colliderGroups.Add(colll);
                attachmentPoint.parentManager.Item.RefreshCollision();
                foreach (var c in colll.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentManager.Item.currentPhysicsLayer;
                }
            }
            foreach (var han in handles.Where(x => x))
            {
                han.item = attachmentPoint.parentManager.Item;
                han.physicBody.rigidBody = attachmentPoint.parentManager.Item.physicBody.rigidBody;
                attachmentPoint.parentManager.Item.handles.Add(han);
                if (attachmentPoint.parentManager.Item.holder != null) han.SetTouch(false);
            } 
            firearm?.additionalTriggerHandles.AddRange(additionalTriggerHandles.Where(x => x));
            if (damagers != null)
            {
                foreach (var dmg in damagers)
                {
                    if (dmg != null && !string.IsNullOrWhiteSpace(damagerIds[damagers.IndexOf(dmg)]))
                    {
                        if (damagerIds[damagers.IndexOf(dmg)].Equals("Mace1H"))
                            damagerIds[damagers.IndexOf(dmg)] = "Ghetto05.FirearmSDKv2.Damagers.NearlyNoDamage";
                        dmg.Load(Catalog.GetData<DamagerData>(damagerIds[damagers.IndexOf(dmg)]), attachmentPoint.parentManager.Item.mainCollisionHandler);
                        attachmentPoint.parentManager.Item.mainCollisionHandler.damagers.Add(dmg);
                    }
                }
            }
            attachmentPoint.parentManager.Item.OnHeldActionEvent += InvokeHeldAction;
            OnDelayedAttachEvent?.Invoke();
            firearm?.InvokeAttachmentAdded(this, attachmentPoint);
            initialized = true;

            attachmentPoint.SetDependantObjectVisibility();
            attachmentPoint.parentManager.Item.UpdateReveal();
            callback?.Invoke(this);
        }

        private void ApplyNode()
        {
            foreach (var n in Node.Childs)
            {
                var point = GetSlotFromId(n.Slot);
                Catalog.GetData<AttachmentData>(Util.GetSubstituteId(n.AttachmentId, $"[Point {point?.id} on {point?.parentManager?.Item?.itemId}]")).SpawnAndAttach(point, null, n, addedByInitialSetup);
            }
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) Detach(true);
        }

        private void InvokeHeldAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handles.Contains(handle)) OnHeldActionEvent?.Invoke(ragdollHand, handle, action);
        }

        private void FixedUpdate()
        {
            if (colliderGroup == null || colliderGroup.colliders == null || attachmentPoint == null || attachmentPoint.parentManager == null) return;
            foreach (var c in colliderGroup.colliders)
            {
                c.gameObject.layer = attachmentPoint.parentManager.Item.currentPhysicsLayer;
            }
        }

        public void Detach(bool despawnDetach = false)
        {
            if (attachmentPoint != null && attachmentPoint.parentManager != null) attachmentPoint.parentManager.Item.OnHeldActionEvent -= InvokeHeldAction;
            OnDetachEvent?.Invoke(despawnDetach);
            if (despawnDetach || attachmentPoint.parentManager == null) return;
            if (attachmentPoint.attachment != null) attachmentPoint.attachment.Node.Childs.Remove(Node);
            else if (attachmentPoint.parentManager != null) attachmentPoint.parentManager.SaveData.FirearmNode.Childs.Remove(Node);
            foreach (var eve in onDetachEvents)
            {
                eve.Invoke();
            }
            var manager = attachmentPoint.parentManager;
            Firearm firearm = null;
            if (manager is Firearm f)
                firearm = f;
            manager.InvokeAttachmentRemoved(this, attachmentPoint);
            if (firearm)
            {
                foreach (var han in additionalTriggerHandles.Where(x => x))
                {
                    firearm.additionalTriggerHandles.Remove(han);
                }
            }
            attachmentPoint.currentAttachments.Remove(this);
            attachmentPoint.SetDependantObjectVisibility();
            var attachments = attachmentPoints.Where(x => x).SelectMany(x => x.currentAttachments).ToArray();
            foreach (var t in attachments)
            {
                t.Detach();
            }
            foreach (var han in handles.Where(x => x))
            {
                manager.Item.handles.Remove(han);
                if (han.touchCollider != null && han.touchCollider.gameObject != null)
                    Destroy(han.touchCollider.gameObject);
            }
            foreach (var d in damagers.Where(x => x))
            {
                manager.Item.mainCollisionHandler.damagers.Remove(d);
            }
            manager.UpdateAttachments();
            manager.Item.colliderGroups.Remove(colliderGroup);
            foreach (var colll in alternateGroups.Where(x => x))
            {
                manager.Item.colliderGroups.Remove(colll);
            }
            foreach (var ren in _renderers.Where(x => x))
            {
                if (!nonLightVolumeRenderers.Contains(ren)) manager.Item.renderers.Remove(ren);
                if (!nonLightVolumeRenderers.Contains(ren)) manager.Item.lightVolumeReceiver.renderers.Remove(ren);
            }
            foreach (var dec in _decals.Where(x => x))
            {
                manager.Item.revealDecals.Remove(dec);
            }
            try { manager.Item.lightVolumeReceiver.SetRenderers(manager.Item.renderers); } catch { Debug.Log($"Setting renderers failed on {gameObject.name}"); }

            if (this == null || gameObject == null) return;

            if (AssetLoadHandle != null)
                Addressables.ReleaseInstance(AssetLoadHandle.Value);
            
            Destroy(gameObject);
        }
        
        public void MoveOnRail(bool forwards)
        {
            if (!attachmentPoint.usesRail)
                return;

            if ((forwards && RailPosition + Data.RailLength >= attachmentPoint.railSlots.Count) || (!forwards && RailPosition == 0))
                return;

            if (forwards)
                Node.SlotPosition++;
            else
                Node.SlotPosition--;
            
            UpdatePosition();
        }

        public int RailPosition
        {
            get { return Node.SlotPosition; }
        }

        private void UpdatePosition()
        {
            if (!attachmentPoint.usesRail)
                return;
            
            transform.SetParent(attachmentPoint.railSlots[Node.SlotPosition]);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        public AttachmentPoint GetSlotFromId(string id)
        {
            foreach (var point in attachmentPoints)
            {
                if (point.id.Equals(id)) return point;
            }
            return null;
        }

        public delegate void OnDelayedAttach();
        public event OnDelayedAttach OnDelayedAttachEvent;

        public delegate void OnDetach(bool despawnDetach);
        public event OnDetach OnDetachEvent;

        public delegate void OnHeldAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action);
        public event OnHeldAction OnHeldActionEvent;
    }
}
