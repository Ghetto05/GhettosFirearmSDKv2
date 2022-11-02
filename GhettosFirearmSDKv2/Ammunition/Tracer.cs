using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Tracer : MonoBehaviour
    {
        public enum Colors
        {
            Red,
            Green,
            Blue,
            Orange,
            White
        }

        public Colors color;
        [HideInInspector]
        public Material redMaterial;
        [HideInInspector]
        public Material greenMaterial;
        [HideInInspector]
        public Material blueMaterial;
        [HideInInspector]
        public Material orangeMaterial;
        [HideInInspector]
        public Material whiteMaterial;
        public MeshRenderer tracerRenderer;

        private void OnValidate()
        {
            tracerRenderer.material = getMaterial(color);
        }

        public Material getMaterial(Colors targetColor)
        {
            if (targetColor == Colors.Red) return redMaterial;
            else if (targetColor == Colors.Green) return greenMaterial;
            else if (targetColor == Colors.Blue) return blueMaterial;
            else if (targetColor == Colors.Orange) return orangeMaterial;
            else if (targetColor == Colors.White) return whiteMaterial;
            else return null;
        }
    }
}
