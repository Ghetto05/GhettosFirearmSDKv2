using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class PistolHandleSwitcher : MonoBehaviour
    {
        public Handle mainHandle;
        public Handle secondaryHandle;
        public Item item;

        public void Awake()
        {
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnUngrabEvent += Item_OnUngrabEvent;
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (handle == secondaryHandle) secondaryHandle.SetTouch(true);
            if (handle == mainHandle)
            {
                if (secondaryHandle.handlers.Count > 0)
                {
                    foreach (RagdollHand hand in secondaryHandle.handlers.ToArray())
                    {
                        hand.UnGrab(false);
                        hand.Grab(mainHandle);
                    }
                }
                else
                {
                    mainHandle.SetTouch(true);
                }
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            if (handle == mainHandle) mainHandle.SetTouch(false);
            else if (handle == secondaryHandle) secondaryHandle.SetTouch(false);
        }
    }
}