using System;
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
            if (attachmentPoint != null && attachmentPoint.parentFirearm != null && attachmentPoint.parentFirearm.item != null)
                Hide(!attachmentPoint.parentFirearm.item.renderers[0].enabled);
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
            addedByInitialSetup = initialSetup;
            if (thisNode != null) Node = thisNode;
            _renderers = new List<Renderer>();
            _decals = new List<RevealDecal>();
            foreach (var ren in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!_renderers.Contains(ren)) _renderers.Add(ren);
                if (!nonLightVolumeRenderers.Contains(ren) && !attachmentPoint.parentFirearm.item.renderers.Contains(ren)) attachmentPoint.parentFirearm.item.renderers.Add(ren);
            }
            foreach (var dec in gameObject.GetComponentsInChildren<RevealDecal>(true))
            {
                if (!_decals.Contains(dec)) _decals.Add(dec);
                if (!attachmentPoint.parentFirearm.item.revealDecals.Contains(dec)) attachmentPoint.parentFirearm.item.revealDecals.Add(dec);
            }
            try { attachmentPoint.parentFirearm.item.lightVolumeReceiver.SetRenderers(attachmentPoint.parentFirearm.item.renderers); } catch { Debug.Log($"Setting renderers failed on {gameObject.name}"); }

            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var ap in attachmentPoints.Where(x => x))
            {
                ap.parentFirearm = attachmentPoint.parentFirearm;
                ap.attachment = this;
            }
            attachmentPoint.parentFirearm.UpdateAttachments(initialSetup);
            attachmentPoint.parentFirearm.item.OnDespawnEvent += Item_OnDespawnEvent;

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
                attachmentPoint.parentFirearm.item.colliderGroups.Add(colliderGroup);
                attachmentPoint.parentFirearm.item.RefreshCollision();
                foreach (var c in colliderGroup.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
                }
            }
            foreach (var colll in alternateGroups.Where(x => x))
            {
                colll.Load(Catalog.GetData<ColliderGroupData>(alternateGroupsIds[alternateGroups.IndexOf(colll)]));
                attachmentPoint.parentFirearm.item.colliderGroups.Add(colll);
                attachmentPoint.parentFirearm.item.RefreshCollision();
                foreach (var c in colll.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
                }
            }
            foreach (var han in handles.Where(x => x))
            {
                han.item = attachmentPoint.parentFirearm.item;
                han.physicBody.rigidBody = attachmentPoint.parentFirearm.item.physicBody.rigidBody;
                attachmentPoint.parentFirearm.item.handles.Add(han);
                if (attachmentPoint.parentFirearm.item.holder != null) han.SetTouch(false);
            }
            foreach (var han in additionalTriggerHandles.Where(x => x))
            {
                attachmentPoint.parentFirearm.additionalTriggerHandles.Add(han);
            }
            if (damagers != null)
            {
                foreach (var dmg in damagers)
                {
                    if (dmg != null && !string.IsNullOrWhiteSpace(damagerIds[damagers.IndexOf(dmg)]))
                    {
                        if (damagerIds[damagers.IndexOf(dmg)].Equals("Mace1H"))
                            damagerIds[damagers.IndexOf(dmg)] = "Ghetto05.FirearmSDKv2.Damagers.NearlyNoDamage";
                        dmg.Load(Catalog.GetData<DamagerData>(damagerIds[damagers.IndexOf(dmg)]), attachmentPoint.parentFirearm.item.mainCollisionHandler);
                        attachmentPoint.parentFirearm.item.mainCollisionHandler.damagers.Add(dmg);
                    }
                }
            }
            attachmentPoint.parentFirearm.item.OnHeldActionEvent += InvokeHeldAction;
            OnDelayedAttachEvent?.Invoke();
            attachmentPoint.parentFirearm.InvokeAttachmentAdded(this, attachmentPoint);
            initialized = true;

            attachmentPoint.SetDependantObjectVisibility();
            attachmentPoint.parentFirearm.item.UpdateReveal();
            callback?.Invoke(this);
        }

        private void ApplyNode()
        {
            foreach (var n in Node.Childs)
            {
                var point = GetSlotFromId(n.Slot);
                Catalog.GetData<AttachmentData>(Util.GetSubstituteId(n.AttachmentId, $"[Point {point?.id} on {point?.parentFirearm?.item?.itemId}]")).SpawnAndAttach(point, null, n, addedByInitialSetup);
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
            if (colliderGroup == null || colliderGroup.colliders == null || attachmentPoint == null || attachmentPoint.parentFirearm == null) return;
            foreach (var c in colliderGroup.colliders)
            {
                c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
            }
        }

        public void Detach(bool despawnDetach = false)
        {
            if (attachmentPoint != null && attachmentPoint.parentFirearm != null) attachmentPoint.parentFirearm.item.OnHeldActionEvent -= InvokeHeldAction;
            OnDetachEvent?.Invoke(despawnDetach);
            if (despawnDetach) return;
            if (attachmentPoint.attachment != null) attachmentPoint.attachment.Node.Childs.Remove(Node);
            else if (attachmentPoint.parentFirearm != null) attachmentPoint.parentFirearm.SaveData.FirearmNode.Childs.Remove(Node);
            foreach (var eve in onDetachEvents)
            {
                eve.Invoke();
            }
            var firearm = attachmentPoint.parentFirearm;
            firearm.InvokeAttachmentRemoved(this, attachmentPoint);
            foreach (var han in additionalTriggerHandles.Where(x => x))
            {
                firearm.additionalTriggerHandles.Remove(han);
            }
            attachmentPoint.currentAttachments.Remove(this);
            attachmentPoint.SetDependantObjectVisibility();
            var attachments = attachmentPoints.Where(x => x).SelectMany(x => x.currentAttachments).ToArray();
            for (var i = 0; i < attachments.Length; i++)
            {
                attachments[i].Detach();
            }
            foreach (var han in handles.Where(x => x))
            {
                firearm.item.handles.Remove(han);
                if (han.touchCollider != null && han.touchCollider.gameObject != null)
                    Destroy(han.touchCollider.gameObject);
            }
            foreach (var d in damagers.Where(x => x))
            {
                firearm.item.mainCollisionHandler.damagers.Remove(d);
            }
            firearm.UpdateAttachments();
            firearm.item.colliderGroups.Remove(colliderGroup);
            foreach (var colll in alternateGroups.Where(x => x))
            {
                firearm.item.colliderGroups.Remove(colll);
            }
            foreach (var ren in _renderers.Where(x => x))
            {
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.renderers.Remove(ren);
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.lightVolumeReceiver.renderers.Remove(ren);
            }
            foreach (var dec in _decals.Where(x => x))
            {
                firearm.item.revealDecals.Remove(dec);
            }
            try { firearm.item.lightVolumeReceiver.SetRenderers(firearm.item.renderers); } catch { Debug.Log($"Setting renderers failed on {gameObject.name}"); }

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
