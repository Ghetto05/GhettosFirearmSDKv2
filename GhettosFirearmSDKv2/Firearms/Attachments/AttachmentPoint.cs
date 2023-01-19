using System.Collections.Generic;
using System.Collections;
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
        public Attachment currentAttachment;
        public string defaultAttachment;
        public GameObject disableOnAttach;

        private void Awake()
        {
            StartCoroutine(Alert());
        }

        public List<Attachment> GetAllChildAttachments()
        {
            List<Attachment> list = new List<Attachment>();
            if (currentAttachment == null) return list;
            foreach (AttachmentPoint point in currentAttachment.attachmentPoints)
            {
                list.AddRange(point.GetAllChildAttachments());
            }
            list.Add(currentAttachment);
            return list;
        }

        private IEnumerator Alert()
        {
            yield return new WaitForSeconds(1f);
            if (parentFirearm == null) Debug.Log("Not initialized! Name: " + name);
        }

        public void SpawnDefaultAttachment()
        {
            if (!string.IsNullOrEmpty(defaultAttachment))
            {
                Catalog.GetData<AttachmentData>(defaultAttachment).SpawnAndAttach(this);
            }
        }

        private void Update()
        {
            if (disableOnAttach != null)
            {
                if (currentAttachment == null && !disableOnAttach.activeInHierarchy) disableOnAttach.SetActive(true);
                else if (currentAttachment != null && disableOnAttach.activeInHierarchy) disableOnAttach.SetActive(false);
            }
        }
    }
}