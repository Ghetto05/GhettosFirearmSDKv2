using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.Clothing
{
    public class WearableItem : MonoBehaviour
    {
        public Item item;
        public HumanBodyBones baseBone;
        public List<Handle> unequipHandles;

        private Creature _currentCreature;

        public void Start()
        {
            item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
            item.OnGrabEvent += ItemOnOnGrabEvent;
            item.OnUngrabEvent += ItemOnOnUngrabEvent;
        }

        private void ItemOnOnUngrabEvent(Handle handle, RagdollHand ragdollhand, bool throwing)
        {
            Equip(ragdollhand.creature);
        }

        private void ItemOnOnGrabEvent(Handle handle, RagdollHand ragdollhand)
        {
            if (unequipHandles.Contains(handle) && _currentCreature != null)
                Unequip();
        }

        private void ItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
        {
        }

        public void Equip(Creature target)
        {
            if (_currentCreature != null)
                return;
            _currentCreature = target;
            var bone = _currentCreature.animator.GetBoneTransform(baseBone);
            if (Vector3.Distance(bone.position, transform.position) > 0.3f)
                return;
            item.DisallowDespawn = true;
            item.disableSnap = true;
            item.physicBody.rigidBody.isKinematic = true;
            transform.SetParent(bone);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 90));
            if (baseBone == HumanBodyBones.Head && target.isPlayer)
                transform.SetParent(Player.local.head.cam.transform);
        }

        public void Unequip()
        {
            if (_currentCreature == null)
                return;

            item.physicBody.rigidBody.isKinematic = false;
            item.DisallowDespawn = false;
            item.disableSnap = false;
            transform.SetParent(null);
            _currentCreature = null;
        }
    }
}