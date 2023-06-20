using System;
using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class Attachment : MonoBehaviour
    {
        public FirearmSaveData.AttachmentTreeNode node;
        public List<Handle> additionalTriggerHandles;
        public AttachmentPoint attachmentPoint;
        public List<AttachmentPoint> attachmentPoints;
        public List<Handle> handles;
        public AttachmentData data;
        public ColliderGroup colliderGroup;
        public List<ColliderGroup> alternateGroups;
        public List<string> alternateGroupsIds;
        [Space]
        public bool isSuppressing;
        public bool multiplyDamage;
        public float damageMultiplier;
        public Texture2D icon;
        public string ColliderGroupId = "PropMetal";
        public bool overridesMuzzleFlash;
        public ParticleSystem newFlash;
        public Transform minimumMuzzlePosition;
        public List<Damager> damagers;
        public List<string> damagerIds;
        public List<Renderer> nonLightVolumeRenderers;

        public List<UnityEvent> OnAttachEvents;
        public List<UnityEvent> OnDetachEvents;

        private List<Renderer> renderers;

        public bool initialized = false;

        private void Update()
        {
            if (attachmentPoint != null && attachmentPoint.parentFirearm != null && attachmentPoint.parentFirearm.item != null) Hide(!attachmentPoint.parentFirearm.item.renderers[0].enabled);
        }

        public void Hide(bool hidden)
        {
            if (renderers == null) return;
            foreach (Renderer ren in renderers)
            {
                ren.enabled = !hidden;
            }
        }

        public void Initialize(FirearmSaveData.AttachmentTreeNode thisNode = null, bool initialSetup = false)
        {
            if (thisNode != null) node = thisNode;
            renderers = new List<Renderer>();
            foreach (Renderer ren in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderers.Contains(ren)) renderers.Add(ren);
                if (!nonLightVolumeRenderers.Contains(ren) && !attachmentPoint.parentFirearm.item.renderers.Contains(ren)) attachmentPoint.parentFirearm.item.renderers.Add(ren);
            }
            try { attachmentPoint.parentFirearm.item.lightVolumeReceiver.SetRenderers(attachmentPoint.parentFirearm.item.renderers); } catch { Debug.Log($"Setting renderers failed on {gameObject.name}"); };
            transform.parent = attachmentPoint.transform;
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            foreach (AttachmentPoint ap in attachmentPoints)
            {
                ap.parentFirearm = attachmentPoint.parentFirearm;
                ap.attachment = this;
            }
            attachmentPoint.parentFirearm.UpdateAttachments(initialSetup);
            attachmentPoint.parentFirearm.item.OnDespawnEvent += Item_OnDespawnEvent;
            Catalog.LoadAssetAsync<Texture2D>(data.iconAddress, tex =>
            {
                icon = tex;
            }, "Attachment_" + data.id);
            if (thisNode != null) ApplyNode();
            foreach (UnityEvent eve in OnAttachEvents)
            {
                eve.Invoke();
            }

            if (colliderGroup != null)
            {
                colliderGroup.Load(Catalog.GetData<ColliderGroupData>(ColliderGroupId));
                attachmentPoint.parentFirearm.item.colliderGroups.Add(colliderGroup);
                attachmentPoint.parentFirearm.item.RefreshCollision();
                foreach (Collider c in colliderGroup.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
                }
            }
            foreach (ColliderGroup colll in alternateGroups)
            {
                colll.Load(Catalog.GetData<ColliderGroupData>(alternateGroupsIds[alternateGroups.IndexOf(colll)]));
                attachmentPoint.parentFirearm.item.colliderGroups.Add(colll);
                attachmentPoint.parentFirearm.item.RefreshCollision();
                foreach (Collider c in colll.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
                }
            }
            foreach (Handle han in handles)
            {
                han.item = attachmentPoint.parentFirearm.item;
                han.physicBody.rigidBody = attachmentPoint.parentFirearm.item.physicBody.rigidBody;
                attachmentPoint.parentFirearm.item.handles.Add(han);
                if (attachmentPoint.parentFirearm.item.holder != null) han.SetTouch(false);
            }
            foreach (Handle han in additionalTriggerHandles)
            {
                attachmentPoint.parentFirearm.additionalTriggerHandles.Add(han);
            }
            if (damagers != null)
            {
                foreach (Damager dmg in damagers)
                {
                    if (dmg != null && !string.IsNullOrWhiteSpace(damagerIds[damagers.IndexOf(dmg)]))
                    {
                        dmg.Load(Catalog.GetData<DamagerData>(damagerIds[damagers.IndexOf(dmg)]), attachmentPoint.parentFirearm.item.mainCollisionHandler);
                        attachmentPoint.parentFirearm.item.mainCollisionHandler.damagers.Add(dmg);
                    }
                }
            }
            attachmentPoint.parentFirearm.item.OnHeldActionEvent += InvokeHeldAction;
            OnDelayedAttachEvent?.Invoke();
            attachmentPoint.parentFirearm.InvokeAttachmentAdded(this, attachmentPoint);
            initialized = true;

            if (FirearmsSettings.debugMode)
            {
                foreach (Handle h in gameObject.GetComponentsInChildren<Handle>())
                {
                    if (h.GetType() != typeof(GhettoHandle)) Debug.LogWarning("Handle " + h.gameObject.name + " on attachment " + gameObject.name + " is not of type GhettoHandle!");
                    if (!handles.Contains(h)) Debug.LogWarning("Handle " + h.gameObject.name + " is not in the handle list of the attachment " + gameObject.name + "!");
                }
            }
        }

        private void ApplyNode()
        {
            foreach (FirearmSaveData.AttachmentTreeNode n in node.childs)
            {
                AttachmentPoint point = GetSlotFromId(n.slot);
                Catalog.GetData<AttachmentData>(n.attachmentId).SpawnAndAttach(point, n);
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
            foreach (Collider c in colliderGroup.colliders)
            {
                c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
            }
        }

        public void Detach(bool despawnDetach = false)
        {
            if (attachmentPoint != null && attachmentPoint.parentFirearm != null) attachmentPoint.parentFirearm.item.OnHeldActionEvent -= InvokeHeldAction;
            OnDetachEvent?.Invoke(despawnDetach);
            if (despawnDetach) return;
            if (attachmentPoint.attachment != null) attachmentPoint.attachment.node.childs.Remove(node);
            else if (attachmentPoint.parentFirearm != null) attachmentPoint.parentFirearm.saveData.firearmNode.childs.Remove(node);
            foreach (UnityEvent eve in OnDetachEvents)
            {
                eve.Invoke();
            }
            Firearm firearm = attachmentPoint.parentFirearm;
            firearm.InvokeAttachmentRemoved(this, attachmentPoint);
            foreach (Handle han in additionalTriggerHandles)
            {
                firearm.additionalTriggerHandles.Remove(han);
            }
            attachmentPoint.currentAttachment = null;
            foreach (AttachmentPoint point in attachmentPoints)
            {
                if (point.currentAttachment != null) point.currentAttachment.Detach();
            }
            foreach (Handle han in handles)
            {
                firearm.item.handles.Remove(han);
                if (han.touchCollider != null && han.touchCollider.gameObject != null) Destroy(han.touchCollider.gameObject);
            }
            foreach (Damager d in damagers)
            {
                firearm.item.mainCollisionHandler.damagers.Remove(d);
            }
            firearm.UpdateAttachments();
            firearm.item.colliderGroups.Remove(colliderGroup);
            foreach (ColliderGroup colll in alternateGroups)
            {
                firearm.item.colliderGroups.Remove(colll);
            }
            foreach (Renderer ren in renderers)
            {
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.renderers.Remove(ren);
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.lightVolumeReceiver.renderers.Remove(ren);
            }
            try { firearm.item.lightVolumeReceiver.SetRenderers(firearm.item.renderers); } catch { Debug.Log($"Setting renderers dfailed on {gameObject.name}"); };
            if (this == null || gameObject == null) return;
            Destroy(gameObject);
        }

        public AttachmentPoint GetSlotFromId(string id)
        {
            foreach (AttachmentPoint point in attachmentPoints)
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
