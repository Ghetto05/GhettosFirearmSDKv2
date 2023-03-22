using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class HUDToggle : MonoBehaviour
    {
        public List<Collider> toggleColliders;
        public List<GameObject> componentsToToggle;
        public bool hudActive;
        private bool lastFramePressed;

        private void Update()
        {
            if (Player.local.creature != null)
            {
                if (CheckInteraction(Side.Left) || CheckInteraction(Side.Right))
                {
                    if (!lastFramePressed) Toggle();
                    lastFramePressed = true;
                }
                else
                {
                    lastFramePressed = false;
                }
            }
        }

        private bool CheckInteraction(Side side)
        {
            if (!Player.local.GetHand(side).controlHand.usePressed) return false;
            foreach (Collider c in toggleColliders)
            {
                if (c.bounds.Contains(Player.local.GetHand(side).ragdollHand.touchCollider.transform.position))
                {
                    return true;
                }
            }
            return false;
        }

        public void Toggle()
        {
            hudActive = !hudActive;
            foreach (GameObject obj in componentsToToggle)
            {
                foreach (Behaviour be in obj.GetComponents<Behaviour>())
                {
                    be.enabled = hudActive;
                }
            }
        }
    }
}
