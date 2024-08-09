using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class PistolHandleSwitcher : MonoBehaviour
    {
        public Handle mainHandle;
        public Handle secondaryHandle;
        public Firearm firearm;
        public Attachment attachment;
        private bool lastFrameDualHeld = false;

        public void Update()
        {
            if (firearm == null || mainHandle == null || secondaryHandle == null)
            {
                if (Settings.debugMode && attachment == null)
                {
                    Debug.Log("Handle switcher on " + GetComponentInParent<Item>().itemId + " is not set up!");
                    return;
                }
                else if (attachment?.attachmentPoint?.parentFirearm != null)
                    firearm = attachment.attachmentPoint.parentFirearm;
            }

            mainHandle.SetTouch(mainHandle.handlers.Count == 0);
            secondaryHandle.SetTouch(mainHandle.handlers.Count != 0);

            if (mainHandle.handlers.Count == 0 && secondaryHandle.handlers.Count > 0)
            {
                RagdollHand hand = secondaryHandle.handlers[0];
                hand.UnGrab(false);
                hand.Grab(mainHandle);
            }

            bool dualHeld = mainHandle.handlers.Count > 0 && secondaryHandle.handlers.Count > 0;
            if (!lastFrameDualHeld && dualHeld)
                firearm.AddRecoilModifier(0.3f, 1f, this);
            else if (lastFrameDualHeld && !dualHeld)
                firearm.RemoveRecoilModifier(this);
            lastFrameDualHeld = dualHeld;
        }
    }
}