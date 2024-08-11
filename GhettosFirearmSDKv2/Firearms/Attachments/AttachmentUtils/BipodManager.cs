using System.Collections.Generic;
using UnityEngine;

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
        private bool _lastFrameOverriden;
        private bool _active;

        private void Start()
        {
            if (firearm == null && attachment != null)
                attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            else if (firearm != null)
                _active = true;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            firearm = attachment.attachmentPoint.parentFirearm;
            _active = true;
        }

        private void FixedUpdate()
        {
            if (!_active)
                return;
            var extended = true;
            foreach (var bp in bipods)
            {
                if (bp.index == 0)
                    extended = false;
            }
            foreach (var t in groundFollowers)
            {
                if (!Physics.Raycast(t.position, t.forward, 0.1f, LayerMask.GetMask("Default")))
                    extended = false;
            }

            if (extended && !_lastFrameOverriden)
                firearm.AddRecoilModifier(linearRecoilModifier, muzzleRiseModifier, this);
            else if (!extended && _lastFrameOverriden)
                firearm.RemoveRecoilModifier(this);

            _lastFrameOverriden = extended;
        }
    }
}
