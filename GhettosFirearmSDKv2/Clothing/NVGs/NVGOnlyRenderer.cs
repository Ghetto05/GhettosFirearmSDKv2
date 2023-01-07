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
            Thermal
        }

        public Types renderType;
        public Camera renderCamera;

        private void Start()
        {
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
            RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
        }

        private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam == renderCamera)
            {
                foreach (NVGOnlyRendererMeshModule module in NVGOnlyRendererMeshModule.all)
                {
                    foreach (GameObject obj in module.objects)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam == renderCamera)
            {
                foreach (NVGOnlyRendererMeshModule module in NVGOnlyRendererMeshModule.all)
                {
                    foreach (GameObject obj in module.objects)
                    {
                        obj.SetActive(true);
                    }
                }
            }
        }
    }
}
