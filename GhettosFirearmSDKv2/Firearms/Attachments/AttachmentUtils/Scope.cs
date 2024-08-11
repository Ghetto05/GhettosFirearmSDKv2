using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace GhettosFirearmSDKv2
{
    public class Scope : MonoBehaviour
    {
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
        public Firearm connectedFirearm;
        public Attachment connectedAttachment;
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
            if (lens != null) lenses.Add(lens);
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        private void InvokedStart()
        {
            var rt = new RenderTexture(1024, 1024, 1, DefaultFormat.HDR);
            rt.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
            cam.targetTexture = rt;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            if (hasZoom && connectedFirearm != null)
            {
                connectedFirearm.item.OnHeldActionEvent += Item_OnHeldActionEvent;
                _zoomIndex = connectedFirearm.SaveData.FirearmNode.GetOrAddValue("ScopeZoom", new SaveNodeValueInt());
            }
            else if (hasZoom && connectedAttachment != null)
            {
                connectedAttachment.OnHeldActionEvent += Item_OnHeldActionEvent;
                _zoomIndex = connectedAttachment.Node.GetOrAddValue("ScopeZoom", new SaveNodeValueInt());
            }
            else SetFOVFromMagnification(noZoomMagnification);

            if (hasZoom)
            {
                currentIndex = _zoomIndex.Value;
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
                cycleUpSound.Play();
                if (currentIndex == magnificationLevels.Count - 1) currentIndex = -1;
                currentIndex++;
            }
            else
            {
                cycleDownSound.Play();
                if (currentIndex == 0) currentIndex = magnificationLevels.Count;
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
            if (reticles.Count > currentIndex && reticles[currentIndex] != null)
            {
                foreach (var reticle in reticles)
                {
                    reticle.SetActive(false);
                }
                reticles[currentIndex].SetActive(true);
            }
            if (selector != null && selectorPositions[currentIndex] is { } t)
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
}
