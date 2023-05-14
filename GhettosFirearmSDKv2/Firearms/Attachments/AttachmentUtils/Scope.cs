﻿using UnityEngine;
using ThunderRoad;
using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;

namespace GhettosFirearmSDKv2
{
    public class Scope : MonoBehaviour
    {
        [Header("Lens")]
        public MeshRenderer lens;
        public int materialIndex = 0;
        public Camera cam;
        [Header("Zoom")]
        public bool overrideX1CameraFOV = false;
        public float noZoomMagnification;
        public bool hasZoom;
        public Handle controllingHandle;
        public Firearm connectedFirearm;
        public Attachment connectedAtatchment;
        public List<float> MagnificationLevels;
        public Transform Selector;
        public List<Transform> SelectorPositions;
        public List<GameObject> Reticles;
        public AudioSource CycleUpSound;
        public AudioSource CycleDownSound;
        int currentIndex;
        float baseFov;
        SaveNodeValueInt zoomIndex;

        public static void SetX1FOV(float fov)
        {
            FirearmsSettings.scopeX1MagnificationFOV = fov;
            FirearmsSettings.local.SendUpdate();
        }

        private void Start()
        {
            DebugLogConsole.AddCommand<float>("SetScopeX1FOV", "Sets scope default zoom", SetX1FOV);
            baseFov = overrideX1CameraFOV? cam.fieldOfView : FirearmsSettings.scopeX1MagnificationFOV;
            RenderTexture rt = new RenderTexture(512, 512, 1, UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
            rt.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
            cam.targetTexture = rt;
            lens.materials[materialIndex].SetTexture("_BaseMap", rt);
            FirearmsSettings.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
            StartCoroutine(delayedLoad());
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
            if (!overrideX1CameraFOV)
            {
                baseFov = FirearmsSettings.scopeX1MagnificationFOV;
                if (hasZoom) SetZoom();
                else SetZoomNoZoomer(noZoomMagnification);
            }
        }

        IEnumerator delayedLoad()
        {
            yield return new WaitForSeconds(1.05f);
            if (hasZoom && connectedFirearm != null)
            {
                connectedFirearm.item.OnHeldActionEvent += Item_OnHeldActionEvent;
                zoomIndex = connectedFirearm.saveData.firearmNode.GetOrAddValue("ScopeZoom", new SaveNodeValueInt());
            }
            else if (hasZoom && connectedAtatchment != null)
            {
                connectedAtatchment.OnHeldActionEvent += Item_OnHeldActionEvent;
                zoomIndex = connectedAtatchment.node.GetOrAddValue("ScopeZoom", new SaveNodeValueInt());
            }
            else SetZoomNoZoomer(noZoomMagnification);

            if (hasZoom)
            {
                currentIndex = zoomIndex.value;
                SetZoom();
                UpdatePosition();
            }
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == controllingHandle)
            {
                if (action == Interactable.Action.UseStart) Cycle(true);
                else if (action == Interactable.Action.AlternateUseStart) Cycle(false);
            }
        }

        public void Cycle(bool up)
        {
            if (up)
            {
                CycleUpSound.Play();
                if (currentIndex == MagnificationLevels.Count - 1) currentIndex = -1;
                currentIndex++;
            }
            else
            {
                CycleDownSound.Play();
                if (currentIndex == 0) currentIndex = MagnificationLevels.Count;
                currentIndex--;
            }
            zoomIndex.value = currentIndex;
            SetZoom();
            UpdatePosition();
        }

        public void SetZoom()
        {
            var factor = 2.0f * Mathf.Tan(0.5f * baseFov * Mathf.Deg2Rad);
            var zoomedFOV = 2.0f * Mathf.Atan(factor / (2.0f * MagnificationLevels[currentIndex])) * Mathf.Rad2Deg;
            if (cam != null) cam.fieldOfView = zoomedFOV;
        }

        public void UpdatePosition()
        {
            if (Reticles.Count > currentIndex && Reticles[currentIndex] != null)
            {
                foreach (GameObject reticle in Reticles)
                {
                    reticle.SetActive(false);
                }
                Reticles[currentIndex].SetActive(true);
            }
            if (Selector != null && SelectorPositions[currentIndex] is Transform t)
            {
                Selector.localPosition = t.localPosition;
                Selector.localEulerAngles = t.localEulerAngles;
            }
        }

        public void SetZoomNoZoomer(float zoom)
        {
            var factor = 2.0f * Mathf.Tan(0.5f * baseFov * Mathf.Deg2Rad);
            var zoomedFOV = 2.0f * Mathf.Atan(factor / (2.0f * zoom)) * Mathf.Rad2Deg;
            cam.fieldOfView = zoomedFOV;
        }
    }
}
