using ThunderRoad;
using TMPro;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class ModderTool : MonoBehaviour
{
    public Item item;
    public TextMeshPro text;
    public Collider tip;
    public Transform positionReference;

    private void Start()
    {
        item.OnDespawnEvent += OnDespawn;
        item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
        item.mainCollisionHandler.OnCollisionStartEvent += OnCollisionStart;
    }

    private void ItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
    {
        if (action == Interactable.Action.AlternateUseStart)
            text.text = positionReference.position.ToString();
    }

    private void OnCollisionStart(CollisionInstance collisioninstance)
    {
        if (collisioninstance.sourceCollider == tip)
            text.text = LayerMask.LayerToName(collisioninstance.targetCollider.gameObject.layer);
    }

    private void OnDespawn(EventTime eventtime)
    {
        item.OnDespawnEvent -= OnDespawn;
        item.OnHeldActionEvent -= ItemOnOnHeldActionEvent;
        item.mainCollisionHandler.OnCollisionStartEvent -= OnCollisionStart;
    }
}