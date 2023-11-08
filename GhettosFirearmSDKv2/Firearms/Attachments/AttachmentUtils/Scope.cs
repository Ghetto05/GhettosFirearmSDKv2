using UnityEngine;
using ThunderRoad;
using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine.Rendering.Universal;

namespace GhettosFirearmSDKv2
{
    public class Scope : MonoBehaviour
    {
        public enum LensSizes
        {
            _10mm = 10,
            _20mm = 20,
            _25mm = 25,
            _30mm = 30,
            _35mm = 35,
            _40mm = 40,
            _50mm = 50
        }

        [Header("Lens")]
        public MeshRenderer lens;
        public int materialIndex = 0;
        public Camera cam;
        public List<Camera> additionalCameras;
        public LensSizes size;
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
        SaveNodeValueInt zoomIndex;

        public virtual void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        private void InvokedStart()
        {
            RenderTexture rt = new RenderTexture(1024, 1024, 1, UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
            rt.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
            cam.targetTexture = rt;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            lens.materials[materialIndex].SetTexture("_BaseMap", rt);

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
            SetZoomNoZoomer(MagnificationLevels[currentIndex]);
        }

        public float GetScale()
        {
            return (float)size / 100f;
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
            var factor = 2.0f * Mathf.Tan(0.5f * FirearmsSettings.scopeX1MagnificationFOV * Mathf.Deg2Rad);
            var zoomedFOV = 2.0f * Mathf.Atan(factor / (2.0f * zoom)) * Mathf.Rad2Deg;
            cam.fieldOfView = zoomedFOV;
            foreach (Camera c in additionalCameras)
            {
                c.fieldOfView = zoomedFOV;
            }
            lens.material.SetTextureScale("_BaseMap", Vector2.one * GetScale());
            lens.material.SetTextureOffset("_BaseMap", Vector3.one * ((1 - GetScale()) / 2));
        }
    }
}
