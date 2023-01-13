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
        public bool damageMultiplier;
        public Texture2D icon;
        public string ColliderGroupId = "PropMetal";
        public bool overridesMuzzleFlash;
        public ParticleSystem newFlash;
        public Transform minimumMuzzlePosition;
        public List<Damager> damagers;
        public List<String> damagerIds;
        public List<Renderer> nonLightVolumeRenderers;

        public List<UnityEvent> OnAttachEvents;
        public List<UnityEvent> OnDetachEvents;

        private List<Renderer> renderers;

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

        public void Initialize(SaveData.AttachmentTree.Node thisNode = null, bool initialSetup = false)
        {
            renderers = new List<Renderer>();
            foreach (Renderer ren in this.gameObject.GetComponentsInChildren<Renderer>())
            {
                renderers.Add(ren);
                if (!nonLightVolumeRenderers.Contains(ren)) attachmentPoint.parentFirearm.item.renderers.Add(ren);
            }
            this.transform.parent = attachmentPoint.transform;
            this.transform.localPosition = Vector3.zero;
            this.transform.localEulerAngles = Vector3.zero;
            foreach (AttachmentPoint ap in attachmentPoints)
            {
                ap.parentFirearm = attachmentPoint.parentFirearm;
                if (thisNode == null) ap.SpawnDefaultAttachment();
            }
            StartCoroutine(delayed());
            attachmentPoint.parentFirearm.UpdateAttachments(initialSetup);
            attachmentPoint.parentFirearm.item.OnDespawnEvent += Item_OnDespawnEvent;
            Catalog.LoadAssetAsync<Texture2D>(data.iconAddress, tex =>
            {
                icon = tex;
            }, "Attachment_" + data.id);
            if (thisNode != null) ApplyNode(thisNode);
            try { attachmentPoint.parentFirearm.item.lightVolumeReceiver.SetRenderers(attachmentPoint.parentFirearm.item.renderers); } catch { Debug.Log($"Setting renderers dfailed on {gameObject.name}"); };
            foreach (UnityEvent eve in OnAttachEvents)
            {
                eve.Invoke();
            }
        }

        private void ApplyNode(SaveData.AttachmentTree.Node thisNode)
        {
            foreach (SaveData.AttachmentTree.Node node in thisNode.childs)
            {
                AttachmentPoint point = GetSlotFromId(node.slot);
                Catalog.GetData<AttachmentData>(node.attachmentId).SpawnAndAttach(point, node);
            }
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) Detach(true);
        }

        IEnumerator delayed()
        {
            yield return new WaitForSeconds(0.1f);
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
                han.rb = attachmentPoint.parentFirearm.item.rb;
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
            OnDetachEvent?.Invoke();
            if (despawnDetach) return;
            foreach (UnityEvent eve in OnDetachEvents)
            {
                eve.Invoke();
            }
            Firearm firearm = attachmentPoint.parentFirearm;
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
            foreach (Renderer ren in this.gameObject.GetComponentsInChildren<Renderer>())
            {
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.renderers.Remove(ren);
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.lightVolumeReceiver.renderers.Remove(ren);
            }
            firearm.item.lightVolumeReceiver.SetRenderers(firearm.item.renderers);
            firearm.InvokeAttachmentRemoved(attachmentPoint);
            if (this == null || this.gameObject == null) return;
            Destroy(this.gameObject);
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

        public delegate void OnDetach();
        public event OnDetach OnDetachEvent;

        public delegate void OnHeldAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action);
        public event OnHeldAction OnHeldActionEvent;
    }
}
