using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PistolHandleSwitcher : MonoBehaviour
    {
        public Handle mainHandle;
        public Handle secondaryHandle;
        public Firearm firearm;
        public Attachment attachment;
        private bool _lastFrameDualHeld;

        public void Update()
        {
            if (firearm == null || mainHandle == null || secondaryHandle == null)
            {
                if (Settings.debugMode && attachment == null)
                {
                    Debug.Log("Handle switcher on " + GetComponentInParent<Item>().itemId + " is not set up!");
                    return;
                }

                if (attachment?.attachmentPoint?.parentFirearm != null)
                    firearm = attachment.attachmentPoint.parentFirearm;
            }

            mainHandle.SetTouch(mainHandle.handlers.Count == 0);
            secondaryHandle.SetTouch(mainHandle.handlers.Count != 0);

            if (mainHandle.handlers.Count == 0 && secondaryHandle.handlers.Count > 0)
            {
                var hand = secondaryHandle.handlers[0];
                hand.UnGrab(false);
                hand.Grab(mainHandle);
            }

            var dualHeld = mainHandle.handlers.Count > 0 && secondaryHandle.handlers.Count > 0;
            if (!_lastFrameDualHeld && dualHeld)
                firearm.AddRecoilModifier(0.3f, 1f, this);
            else if (_lastFrameDualHeld && !dualHeld)
                firearm.RemoveRecoilModifier(this);
            _lastFrameDualHeld = dualHeld;
        }
    }
}