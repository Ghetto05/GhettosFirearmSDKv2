using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2.UI
{
    public class CanvasCamAssigner : MonoBehaviour
    {
        public Canvas canvas;

        private void Awake()
        {
            PointerInputModule.SetUICameraToAllCanvas();
        }
    }
}
