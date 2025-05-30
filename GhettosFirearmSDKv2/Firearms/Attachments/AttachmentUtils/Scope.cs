﻿using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2;

public class Scope : MonoBehaviour
{
    public Attachment connectedAttachment;
    public GameObject attachmentManager;
    private IAttachmentManager _attachmentManager;

    public enum LensSizes
    {
        _10mm = 10,
        _15mm = 15,
        _20mm = 20,
        _25mm = 25,
        _30mm = 30,
        _35mm = 35,
        _40mm = 40,
        _45mm = 45,
        _50mm = 50
    }

    [Header("Reticle")]
    public Canvas reticleCanvas;

    [Header("Lens")]
    public MeshRenderer lens;

    [FormerlySerializedAs("additionalLenses")]
    public List<MeshRenderer> lenses;

    public int materialIndex;
    public Camera cam;
    public List<Camera> additionalCameras;
    public LensSizes size;

    [Header("Zoom")]
    public bool overrideX1CameraFOV;

    public float noZoomMagnification;
    public bool hasZoom;
    public Handle controllingHandle;
    public List<float> magnificationLevels;
    public Transform selector;
    public List<Transform> selectorPositions;
    public List<GameObject> reticles;
    public AudioSource cycleUpSound;
    public AudioSource cycleDownSound;
    public int currentIndex;
    private SaveNodeValueInt _zoomIndex;

    public virtual void Start()
    {
        if (lens)
        {
            lenses.Add(lens);
        }
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    private void InvokedStart()
    {
        var rt = new RenderTexture(1024, 1024, 1, DefaultFormat.HDR);
        rt.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
        cam.targetTexture = rt;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        if (hasZoom && attachmentManager is not null)
        {
            _attachmentManager = attachmentManager.GetComponent<IAttachmentManager>();
            _attachmentManager.OnHeldAction += OnHeldAction;
            _zoomIndex = _attachmentManager.SaveData.FirearmNode.GetOrAddValue("ScopeZoom", new SaveNodeValueInt());
        }
        else if (hasZoom && connectedAttachment)
        {
            connectedAttachment.OnHeldAction += OnHeldAction;
            _zoomIndex = connectedAttachment.Node.GetOrAddValue("ScopeZoom", new SaveNodeValueInt());
        }
        else
        {
            SetFOVFromMagnification(noZoomMagnification);
        }

        if (hasZoom)
        {
            currentIndex = _zoomIndex.Value;
            SetZoom();
            UpdatePosition();
        }
    }

    private void OnHeldAction(IInteractionProvider.HeldActionData e)
    {
        if (e.Handle == controllingHandle)
        {
            if (e.Action == Interactable.Action.UseStart)
            {
                Cycle(true);
                e.Handled = true;
            }
            else if (e.Action == Interactable.Action.AlternateUseStart)
            {
                Cycle(false);
                e.Handled = true;
            }
        }
    }

    public void Cycle(bool up)
    {
        if (up)
        {
            cycleUpSound.Play();
            if (currentIndex == magnificationLevels.Count - 1)
            {
                currentIndex = -1;
            }
            currentIndex++;
        }
        else
        {
            cycleDownSound.Play();
            if (currentIndex == 0)
            {
                currentIndex = magnificationLevels.Count;
            }
            currentIndex--;
        }
        _zoomIndex.Value = currentIndex;
        SetZoom();
        UpdatePosition();
    }

    public void SetZoom()
    {
        SetFOVFromMagnification(magnificationLevels[currentIndex]);
    }

    public float GetScale()
    {
        return (float)size / 100f;
    }

    public void UpdatePosition()
    {
        if (reticles.Count > currentIndex && reticles[currentIndex])
        {
            foreach (var reticle in reticles)
            {
                reticle.SetActive(false);
            }
            reticles[currentIndex].SetActive(true);
        }
        if (selector && selectorPositions[currentIndex] is { } t)
        {
            selector.localPosition = t.localPosition;
            selector.localEulerAngles = t.localEulerAngles;
        }
    }

    public void SetFOVFromMagnification(float magnification)
    {
        var factor = 2.0f * Mathf.Tan(0.5f * Settings.scopeX1MagnificationFOV * Mathf.Deg2Rad);
        var fov = 2.0f * Mathf.Atan(factor / (2.0f * magnification)) * Mathf.Rad2Deg;

        cam.fieldOfView = fov;
        foreach (var c in additionalCameras)
        {
            c.fieldOfView = fov;
        }
        UpdateRenderers();
    }

    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

    public void UpdateRenderers()
    {
        foreach (var l in lenses)
        {
            l.materials[materialIndex].SetTexture(BaseMap, cam.targetTexture);
            l.materials[materialIndex].SetTextureScale(BaseMap, Vector2.one * GetScale());
            l.materials[materialIndex].SetTextureOffset(BaseMap, Vector3.one * ((1 - GetScale()) / 2));
        }
    }
}