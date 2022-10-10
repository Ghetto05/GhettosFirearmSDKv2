using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2.UI
{
    public class CanvasCamAssigner : MonoBehaviour
    {
        public Canvas canvas;

        void Awake()
        {
            PointerInputModule.SetUICameraToAllCanvas();
        }
    }
}
