using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class ThermalBody : MonoBehaviour
    {
        public static List<ThermalBody> all = new List<ThermalBody>();

        public Transform rig;
        public List<Transform> bones;
        public List<SkinnedMeshRenderer> renderers;
        Creature cc;

        public Material standardMaterial;
        public Material redHotMaterial;
        public Material whiteHotMaterial;
        public Material blackHotMaterial;

        public void ApplyTo(Creature c)
        {
            cc = c;
            c.OnKillEvent += C_OnKillEvent;
            Invoke(nameof(Apply), 0.2f);
        }

        private void C_OnKillEvent(CollisionInstance collisionInstance, EventTime eventTime)
        {
            try
            {
                if (eventTime == EventTime.OnEnd)
                {
                    Destroy(rig?.gameObject);
                    foreach (Transform t in bones)
                    {
                        Destroy(t?.gameObject);
                    }
                    if (gameObject != null) Destroy(gameObject);
                }
            }
            catch (System.Exception)
            { }
        }

        private void Apply()
        {
            rig.SetParent(cc.ragdoll.animatorRig);
            rig.localPosition = Vector3.zero;
            rig.localEulerAngles = new Vector3(0, 0, 0);
            rig.localScale = Vector3.one;

            foreach (Transform b in bones)
            {
                if (Util.RecursiveFindChild(cc.ragdoll.animatorRig, b.gameObject.name) is Transform t)
                {
                    b.SetParent(t);
                    b.localPosition = Vector3.zero;
                    b.localEulerAngles = new Vector3(0, 0, 0);
                    b.localScale = Vector3.one;
                }
            }
        }

        private void OnDestroy()
        {
            Catalog.ReleaseAsset(gameObject);
        }

        public void Start()
        {
            if (all == null) all = new List<ThermalBody>();
            all.Add(this);
        }

        public void SetColor(NVGOnlyRenderer.ThermalTypes t)
        {
            if (standardMaterial == null || renderers.Count == 0 || renderers[0] == null) return;
            Material m = null;
            if (t == NVGOnlyRenderer.ThermalTypes.Standard)
            {
                m = standardMaterial;
            }
            else if (t == NVGOnlyRenderer.ThermalTypes.BlackHot)
            {
                m = blackHotMaterial;
            }
            else if (t == NVGOnlyRenderer.ThermalTypes.RedHot)
            {
                m = redHotMaterial;
            }
            else if (t == NVGOnlyRenderer.ThermalTypes.WhiteHot)
            {
                m = whiteHotMaterial;
            }

            try
            {
                foreach (SkinnedMeshRenderer r in renderers)
                {
                    r.material = m;
                }
            }
            catch (System.Exception)
            {

                throw;
            }
        }
    }
}
