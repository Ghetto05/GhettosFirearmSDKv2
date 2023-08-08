using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Attachments/Systems/Illuminators/NVG Only Renderer")]
    public class NVGOnlyRenderer : MonoBehaviour
    {
        public enum Types
        {
            InfraRed,
            FirstPerson,
            Thermal
        }

        public enum ThermalTypes
        {
            Standard,
            RedHot,
            WhiteHot,
            BlackHot
        }

        public Types renderType;
        public Camera renderCamera;
        public ThermalTypes thermalType;

        private void Start()
        {
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
            RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
        }

        private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam == renderCamera)
            {
                if (NVGOnlyRendererMeshModule.all == null) return;
                foreach (NVGOnlyRendererMeshModule module in NVGOnlyRendererMeshModule.all)
                {
                    if (module.renderType.Equals(renderType))
                    {
                        foreach (GameObject obj in module.objects)
                        {
                            obj.SetActive(false);
                        }
                    }
                }
            }
        }

        private void UpdateThermal()
        {
            if (ThermalBody.all == null) return;
            foreach (ThermalBody t in ThermalBody.all)
            {
                t.SetColor(thermalType);
            }
        }

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam == renderCamera)
            {
                if (NVGOnlyRendererMeshModule.all == null) return;
                foreach (NVGOnlyRendererMeshModule module in NVGOnlyRendererMeshModule.all)
                {
                    if (module.renderType.Equals(renderType))
                    {
                        foreach (GameObject obj in module.objects)
                        {
                            obj.SetActive(true);
                            if (renderType == Types.Thermal) UpdateThermal();
                            foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
                            {
                                r.enabled = true;
                            }
                        }
                    }
                }
            }
        }
    }
}