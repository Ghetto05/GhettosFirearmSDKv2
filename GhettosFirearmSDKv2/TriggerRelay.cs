using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class TriggerRelay : MonoBehaviour
    {
        public FirearmBase source;
        public FirearmBase target;
        public AttachmentPoint targetPoint;

        private void Awake()
        {
            source.OnTriggerChangeEvent += Source_OnTriggerChangeEvent;
        }

        private void Source_OnTriggerChangeEvent(bool isPulled)
        {
            if (target != null) target.ChangeTrigger(isPulled);
            if (targetPoint != null && targetPoint.currentAttachment != null && targetPoint.currentAttachment.GetComponent<AttachmentFirearm>() is AttachmentFirearm targetFire) targetFire.ChangeTrigger(isPulled);
        }
    }
}
