using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AttachmentPoint : MonoBehaviour
{
    public Firearm parentFirearm;
    public AttachmentManager attachmentManager;
    public IAttachmentManager ConnectedManager;
    public Attachment attachment;
        
    public string type;
    public List<string> alternateTypes;
    public string id;
    public List<Attachment> currentAttachments = [];
    public string defaultAttachment;
    public GameObject disableOnAttach;
    public GameObject enableOnAttach;
    public List<Collider> attachColliders;

    [Space]
    public bool usesRail;
    public string railType;
    public List<Transform> railSlots;
    public bool requiredToFire;
    public bool dummyMuzzleSlot;
    public bool useOverrideMagazineAttachmentType;

    private void Start()
    {
        if (parentFirearm)
            ConnectedManager = parentFirearm;
        if (attachmentManager)
            ConnectedManager = attachmentManager;
            
        if (Settings.debugMode)
            StartCoroutine(Alert());
        Invoke(nameof(InvokedStart), Settings.invokeTime + 0.01f);
    }

    private void InvokedStart()
    {
        if (ConnectedManager != null)
            ConnectedManager.OnCollision += OnCollision;
    }

    private void OnCollision(Collision collision)
    {
        if (!currentAttachments.Any() && collision.contacts[0].otherCollider.gameObject.GetComponentInParent<Item>()?.GetComponentInChildren<AttachableItem>() is { } ati)
        {
            if ((ati.attachmentType.Equals(type) || alternateTypes.Contains(ati.attachmentType)) && Util.CheckForCollisionWithColliders(attachColliders, ati.attachColliders, collision))
            {
                var node = ati.item.GetComponent<IAttachmentManager>()?.SaveData.FirearmNode.CloneJson();
                Catalog.GetData<AttachmentData>(Util.GetSubstituteId(ati.attachmentId, $"[Attachable item - point {id} on {ConnectedManager?.Item?.itemId}]")).SpawnAndAttach(this, null, node);
                var s = Util.PlayRandomAudioSource(ati.attachSounds);
                if (s)
                {
                    s.transform.SetParent(transform);
                    StartCoroutine(Explosive.DelayedDestroy(s.gameObject, s.clip.length + 1f));
                } 
                ati.item.handles.ForEach(h => h.Release());
                ati.item.Despawn();
            }
        }
    }

    public List<Attachment> GetAllChildAttachments()
    {
        var list = new List<Attachment>();
        if (!currentAttachments.Any())
            return list;
        foreach (var point in currentAttachments.SelectMany(x => x.attachmentPoints))
        {
            list.AddRange(point.GetAllChildAttachments());
        }
        list.AddRange(currentAttachments);
        return list;
    }

    private IEnumerator Alert()
    {
        yield return new WaitForSeconds(1f);
            
        string parentFound;
        if (GetComponentInParent<Attachment>() is { } a)
            parentFound = "Attachment name: " + a.gameObject.name;
        else if (GetComponentInParent<IAttachmentManager>() is { } f)
            parentFound = "Firearm name: " + f.Transform.name;
        else
            parentFound = "No parent firearm or attachment found!";
            
        if (ConnectedManager == null)
            Debug.Log("Not initialized! Name: " + name + "\n" + parentFound);

        if (gameObject.name.Equals("mod_muzzle") && !attachColliders.Any())
            Debug.Log($"Muzzle on {parentFound} does not have any attach colliders!");
    }

    public void SpawnDefaultAttachment()
    {
        if (!string.IsNullOrEmpty(defaultAttachment))
        {
            Catalog.GetData<AttachmentData>(Util.GetSubstituteId(defaultAttachment, $"[Default attachment on point {id} on {ConnectedManager?.Item?.itemId}]")).SpawnAndAttach(this);
        }
    }

    public void SetDependantObjectVisibility()
    {
        if (disableOnAttach)
        {
            if (!currentAttachments.Any() && !disableOnAttach.activeInHierarchy)
                disableOnAttach.SetActive(true);
            else if (currentAttachments.Any() && disableOnAttach.activeInHierarchy)
                disableOnAttach.SetActive(false);
        }
        if (enableOnAttach)
        {
            if (currentAttachments.Any() && !enableOnAttach.activeInHierarchy)
                enableOnAttach.SetActive(true);
            else if (!currentAttachments.Any() && enableOnAttach.activeInHierarchy)
                enableOnAttach.SetActive(false);
        }
    }

    public void InvokeAttachmentAdded(Attachment addedAttachment) => OnAttachmentAddedEvent?.Invoke(addedAttachment);
    public delegate void OnAttachmentAdded(Attachment attachment);
    public event OnAttachmentAdded OnAttachmentAddedEvent;
}