using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using UnityEngine;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Bipod Manager")]
    public class BipodManager : MonoBehaviour
    {
        [FormerlySerializedAs("firearm"), SerializeField, SerializeReference]
        public IAttachmentManager attachmentManager;
        public Attachment attachment;
        public List<Bipod> bipods;
        public List<Transform> groundFollowers;
        public float linearRecoilModifier;
        public float muzzleRiseModifier;
        private bool _lastFrameOverriden;
        private bool _active;

        private void Start()
        {
            if (attachmentManager != null && attachment)
                attachment.OnDelayedAttachEvent += Attachment_OnDelayedAttachEvent;
            else if (attachmentManager != null)
                _active = true;
        }

        private void Attachment_OnDelayedAttachEvent()
        {
            attachmentManager = attachment.attachmentPoint.parentManager;
            _active = true;
        }

        private void FixedUpdate()
        {
            if (!_active)
                return;
            var extended = !bipods.Any(x => x.index == 0);
            if (groundFollowers.Any(t => !Physics.Raycast(t.position, t.forward, 0.1f, LayerMask.GetMask("Default"))))
                extended = false;

            if (attachmentManager is Firearm firearm)
            {
                switch (extended)
                {
                    case true when !_lastFrameOverriden:
                        firearm.AddRecoilModifier(linearRecoilModifier, muzzleRiseModifier, this);
                        break;
                    case false when _lastFrameOverriden:
                        firearm.RemoveRecoilModifier(this);
                        break;
                }
            }

            _lastFrameOverriden = extended;
        }
    }
}
