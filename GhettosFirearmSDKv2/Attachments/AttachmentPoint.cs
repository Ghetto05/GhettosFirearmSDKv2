using System.Collections.Generic;
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

        public void SpawnDefaultAttachment()
        {
            if (!string.IsNullOrEmpty(defaultAttachment))
            {
                Catalog.GetData<AttachmentData>(defaultAttachment).SpawnAndAttach(this);
            }
        }
    }
}