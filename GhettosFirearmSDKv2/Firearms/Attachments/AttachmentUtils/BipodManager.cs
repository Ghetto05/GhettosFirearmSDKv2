using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Bipod Manager")]
    public class BipodManager : MonoBehaviour
    {
        public Firearm firearm;
        public Attachment attachment;
        public List<Bipod> bipods;
        public List<Transform> groundFollowers;
        public float linearRecoilModifier;
        public float muzzleRiseModifier;
        private bool lastFrameOverriden = false;
        private bool active = false;

        private void Awake()
        {
            if (firearm == null && attachment != null) attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            else if (firearm != null) active = true;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            firearm = attachment.attachmentPoint.parentFirearm;
            active = true;
        }

        private void FixedUpdate()
        {
            if (!active) return;
            bool extended = true;
            foreach (Bipod bp in bipods)
            {
                if (bp.index == 0) extended = false;
            }
            foreach (Transform t in groundFollowers)
            {
                if (!Physics.Raycast(t.position, t.forward, 0.06f, LayerMask.GetMask("Default"))) extended = false;
            }

            if (extended && !lastFrameOverriden) firearm.AddRecoilModifier(linearRecoilModifier, muzzleRiseModifier, this);
            else if (!extended && lastFrameOverriden) firearm.RemoveRecoilModifier(this);

            lastFrameOverriden = extended;
        }
    }
}
