using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        private List<RevealDecal> decals;

        public bool initialized = false;

        private void Update()
        {
            if (attachmentPoint != null && attachmentPoint.parentFirearm != null && attachmentPoint.parentFirearm.item != null)
                Hide(!attachmentPoint.parentFirearm.item.renderers[0].enabled);
        }

        public void Hide(bool hidden)
        {
            if (renderers == null) return;
            foreach (var ren in renderers)
            {
                ren.enabled = !hidden;
            }
        }

        public void Initialize(FirearmSaveData.AttachmentTreeNode thisNode = null, bool initialSetup = false)
        {
            if (thisNode != null) Node = thisNode;
            renderers = new List<Renderer>();
            decals = new List<RevealDecal>();
            foreach (var ren in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderers.Contains(ren)) renderers.Add(ren);
                if (!nonLightVolumeRenderers.Contains(ren) && !attachmentPoint.parentFirearm.item.renderers.Contains(ren)) attachmentPoint.parentFirearm.item.renderers.Add(ren);
            }
            foreach (var dec in gameObject.GetComponentsInChildren<RevealDecal>(true))
            {
                if (!decals.Contains(dec)) decals.Add(dec);
                if (!attachmentPoint.parentFirearm.item.revealDecals.Contains(dec)) attachmentPoint.parentFirearm.item.revealDecals.Add(dec);
            }
            try { attachmentPoint.parentFirearm.item.lightVolumeReceiver.SetRenderers(attachmentPoint.parentFirearm.item.renderers); } catch { Debug.Log($"Setting renderers failed on {gameObject.name}"); };
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var ap in attachmentPoints)
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
            foreach (var eve in OnAttachEvents)
            {
                eve.Invoke();
            }

            if (colliderGroup != null)
            {
                colliderGroup.Load(Catalog.GetData<ColliderGroupData>(ColliderGroupId));
                attachmentPoint.parentFirearm.item.colliderGroups.Add(colliderGroup);
                attachmentPoint.parentFirearm.item.RefreshCollision();
                foreach (var c in colliderGroup.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
                }
            }
            foreach (var colll in alternateGroups)
            {
                colll.Load(Catalog.GetData<ColliderGroupData>(alternateGroupsIds[alternateGroups.IndexOf(colll)]));
                attachmentPoint.parentFirearm.item.colliderGroups.Add(colll);
                attachmentPoint.parentFirearm.item.RefreshCollision();
                foreach (var c in colll.colliders)
                {
                    c.gameObject.layer = attachmentPoint.parentFirearm.item.currentPhysicsLayer;
                }
            }
            foreach (var han in handles)
            {
                han.item = attachmentPoint.parentFirearm.item;
                han.physicBody.rigidBody = attachmentPoint.parentFirearm.item.physicBody.rigidBody;
                attachmentPoint.parentFirearm.item.handles.Add(han);
                if (attachmentPoint.parentFirearm.item.holder != null) han.SetTouch(false);
            }
            foreach (var han in additionalTriggerHandles)
            {
                attachmentPoint.parentFirearm.additionalTriggerHandles.Add(han);
            }
            if (damagers != null)
            {
                foreach (var dmg in damagers)
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

            attachmentPoint.parentFirearm.item.UpdateReveal();
        }

        private void ApplyNode()
        {
            foreach (var n in Node.childs)
            {
                var point = GetSlotFromId(n.slot);
                Catalog.GetData<AttachmentData>(Util.GetSubstituteId(n.attachmentId, $"[Point {point?.id} on {point?.parentFirearm?.item?.itemId}]")).SpawnAndAttach(point, null, n);
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
            if (attachmentPoint.attachment != null) attachmentPoint.attachment.Node.childs.Remove(Node);
            else if (attachmentPoint.parentFirearm != null) attachmentPoint.parentFirearm.saveData.firearmNode.childs.Remove(Node);
            foreach (var eve in OnDetachEvents)
            {
                eve.Invoke();
            }
            var firearm = attachmentPoint.parentFirearm;
            firearm.InvokeAttachmentRemoved(this, attachmentPoint);
            foreach (var han in additionalTriggerHandles)
            {
                firearm.additionalTriggerHandles.Remove(han);
            }
            attachmentPoint.currentAttachments.Remove(this);
            var attachments = attachmentPoints.SelectMany(x => x.currentAttachments).ToArray();
            for (var i = 0; i < attachments.Length; i++)
            {
                attachments[i].Detach();
            }
            foreach (var han in handles)
            {
                firearm.item.handles.Remove(han);
                if (han.touchCollider != null && han.touchCollider.gameObject != null) Destroy(han.touchCollider.gameObject);
            }
            foreach (var d in damagers)
            {
                firearm.item.mainCollisionHandler.damagers.Remove(d);
            }
            firearm.UpdateAttachments();
            firearm.item.colliderGroups.Remove(colliderGroup);
            foreach (var colll in alternateGroups)
            {
                firearm.item.colliderGroups.Remove(colll);
            }
            foreach (var ren in renderers)
            {
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.renderers.Remove(ren);
                if (!nonLightVolumeRenderers.Contains(ren)) firearm.item.lightVolumeReceiver.renderers.Remove(ren);
            }
            foreach (var dec in decals)
            {
                firearm.item.revealDecals.Remove(dec);
            }
            try { firearm.item.lightVolumeReceiver.SetRenderers(firearm.item.renderers); } catch { Debug.Log($"Setting renderers dfailed on {gameObject.name}"); };
            if (this == null || gameObject == null) return;

            if (AssetLoadHandle != null)
                Addressables.ReleaseInstance(AssetLoadHandle.Value);
            
            Destroy(gameObject);
        }
        
        public void MoveOnRail(bool forwards)
        {
            if (!attachmentPoint.usesRail)
                return;

            if ((forwards && RailPosition + Data.railLength >= attachmentPoint.railSlots.Count) || (!forwards && RailPosition == 0))
                return;

            if (forwards)
                Node.slotPosition++;
            else
                Node.slotPosition--;
            
            UpdatePosition();
        }

        public int RailPosition
        {
            get { return Node.slotPosition; }
        }

        private void UpdatePosition()
        {
            if (!attachmentPoint.usesRail)
                return;
            
            transform.SetParent(attachmentPoint.railSlots[Node.slotPosition]);
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
