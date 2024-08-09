using ThunderRoad;
using TMPro;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class ModderTool : MonoBehaviour
    {
        public Item item;
        public TextMeshPro text;
        public Collider tip;

        private void Start()
        {
            item.OnDespawnEvent += OnDespawn;
            item.mainCollisionHandler.OnCollisionStartEvent += OnCollisionStart;
        }

        private void OnCollisionStart(CollisionInstance collisioninstance)
        {
            if (collisioninstance.sourceCollider == tip)
               text.text = LayerMask.LayerToName(collisioninstance.targetCollider.gameObject.layer);
        }

        private void OnDespawn(EventTime eventtime)
        {
            item.OnDespawnEvent -= OnDespawn;
            item.mainCollisionHandler.OnCollisionStartEvent -= OnCollisionStart;
        }
    }
}