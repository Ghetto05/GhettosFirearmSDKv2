using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Attachments/Reticle switcher")]
public class ReticleSwitcher : MonoBehaviour
{
    public Handle toggleHandle;
    public List<GameObject> reticles;
    public GameObject defaultReticle;
    public AudioSource switchSound;

    private void Start()
    {
        if (!defaultReticle && reticles is not null && reticles.Count > 0)
        {
            defaultReticle = reticles[0];
        }
        foreach (var reti in reticles!)
        {
            reti.SetActive(false);
        }
        if (defaultReticle)
        {
            defaultReticle.SetActive(true);
        }
        toggleHandle.OnHeldActionEvent += ToggleHandle_OnHeldActionEvent;
    }

    private void ToggleHandle_OnHeldActionEvent(RagdollHand hand, Interactable.Action action)
    {
        if (action == Interactable.Action.UseStart)
        {
            Switch();
        }
    }

    public void Switch()
    {
        if (!defaultReticle)
        {
            return;
        }
        if (reticles is not null && reticles.Count > 1)
        {
            if (switchSound)
            {
                switchSound.Play();
            }
            if (reticles.IndexOf(defaultReticle) + 1 < reticles.Count)
            {
                defaultReticle = reticles[reticles.IndexOf(defaultReticle) + 1];
            }
            else
            {
                defaultReticle = reticles[0];
            }

            foreach (var reti in reticles)
            {
                reti.SetActive(false);
            }
            if (defaultReticle)
            {
                defaultReticle.SetActive(true);
            }
        }
    }
}