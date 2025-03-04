using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class HUDToggle : MonoBehaviour
{
    public List<Collider> toggleColliders;
    public List<GameObject> componentsToToggle;
    public bool hudActive;
    private bool _lastFramePressed;

    private void Update()
    {
        if (Player.local.creature != null)
        {
            if (CheckInteraction(Side.Left) || CheckInteraction(Side.Right))
            {
                if (!_lastFramePressed) Toggle();
                _lastFramePressed = true;
            }
            else
            {
                _lastFramePressed = false;
            }
        }
    }

    private bool CheckInteraction(Side side)
    {
        if (!Player.local.GetHand(side).controlHand.usePressed) return false;
        foreach (var c in toggleColliders)
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
        foreach (var obj in componentsToToggle)
        {
            foreach (var be in obj.GetComponents<Behaviour>())
            {
                be.enabled = hudActive;
            }
        }
    }
}