using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AttachmentPoint : MonoBehaviour
    {
        public string type;
        public List<string> alternateTypes;
        public string id;
        public Firearm parentFirearm;
        public List<Attachment> currentAttachments = new();
        public string defaultAttachment;
        public GameObject disableOnAttach;
        public GameObject enableOnAttach;
        public Attachment attachment;
        public List<Collider> attachColliders;

        [Space]
        public bool usesRail;
        public string railType;
        public List<Transform> railSlots;

        private void Start()
        {
            if (FirearmsSettings.debugMode)
                StartCoroutine(Alert());
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime + 0.01f);
        }

        private void InvokedStart()
        {
            if (parentFirearm != null)
                parentFirearm.OnCollisionEvent += ParentFirearm_OnCollisionEvent;
        }

        private void ParentFirearm_OnCollisionEvent(Collision collision)
        {
            if (!currentAttachments.Any() && collision.contacts[0].otherCollider.gameObject.GetComponentInParent<AttachableItem>() is AttachableItem ati)
            {
                if ((ati.attachmentType.Equals(type) || alternateTypes.Contains(ati.attachmentType)) && Util.CheckForCollisionWithColliders(attachColliders, ati.attachColliders, collision))
                {
                    Catalog.GetData<AttachmentData>(Util.GetSubstituteId(ati.attachmentId, $"[Attachable item - point {id} on {parentFirearm?.item?.itemId}]")).SpawnAndAttach(this);
                    AudioSource s = Util.PlayRandomAudioSource(ati.attachSounds);
                    if (s != null)
                    {
                        s.transform.SetParent(transform);
                        StartCoroutine(Explosives.Explosive.delayedDestroy(s.gameObject, s.clip.length + 1f));
                    } 
                    ati.item.handles.ForEach(h => h.Release());
                    ati.item.Despawn();
                }
            }
        }

        public List<Attachment> GetAllChildAttachments()
        {
            List<Attachment> list = new List<Attachment>();
            if (!currentAttachments.Any())
                return list;
            foreach (AttachmentPoint point in currentAttachments.SelectMany(x => x.attachmentPoints))
            {
                list.AddRange(point.GetAllChildAttachments());
            }
            list.AddRange(currentAttachments);
            return list;
        }

        private IEnumerator Alert()
        {
            yield return new WaitForSeconds(1f);
            if (parentFirearm == null)
            {
                string parentFound = "";
                if (GetComponentInParent<Attachment>() is Attachment a)
                {
                    parentFound = "Attachment name: " + a.gameObject.name;
                }
                else if (GetComponentInParent<Firearm>() is Firearm f)
                {
                    parentFound = "Firearm name: " + f.gameObject.name;
                }
                else
                {
                    parentFound = "No parent firearm or attachment found!";
                }
                Debug.Log("Not initialized! Name: " + name + "\n" + parentFound);
            }

            if (gameObject.name.Equals("mod_muzzle") && !attachColliders.Any())
            {
                Debug.Log($"Muzzle on {attachment.gameObject.name} does not have any attach colliders!");
            }
        }

        public void SpawnDefaultAttachment()
        {
            if (!string.IsNullOrEmpty(defaultAttachment))
            {
                Catalog.GetData<AttachmentData>(Util.GetSubstituteId(defaultAttachment, $"[Default attachment on point {id} on {parentFirearm?.item?.itemId}]")).SpawnAndAttach(this, null);
            }
        }

        private void Update()
        {
            if (disableOnAttach != null)
            {
                if (!currentAttachments.Any() && !disableOnAttach.activeInHierarchy)
                    disableOnAttach.SetActive(true);
                else if (currentAttachments.Any() && disableOnAttach.activeInHierarchy)
                    disableOnAttach.SetActive(false);
            }
            if (enableOnAttach != null)
            {
                if (currentAttachments.Any() && !enableOnAttach.activeInHierarchy)
                    enableOnAttach.SetActive(true);
                else if (!currentAttachments.Any() && enableOnAttach.activeInHierarchy)
                    enableOnAttach.SetActive(false);
            }
        }

        public void InvokeAttachmentAdded(Attachment attachment) => OnAttachmentAddedEvent?.Invoke(attachment);
        public delegate void OnAttachmentAdded(Attachment attachment);
        public event OnAttachmentAdded OnAttachmentAddedEvent;
    }
}