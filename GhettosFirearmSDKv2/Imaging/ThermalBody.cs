using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ThunderRoad;
using Unity.Mathematics;

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

        private Material _smInst;
        private Material _rhInst;
        private Material _whInst;
        private Material _bhInst;

        public void ApplyTo(Creature c)
        {
            cc = c;
            c.OnKillEvent += C_OnKillEvent;
            c.OnDespawnEvent += COnDespawnEvent;
            Invoke(nameof(Apply), 0.1f);
        }

        private void COnDespawnEvent(EventTime eventTime)
        {
            try
            {
                if (eventTime == EventTime.OnStart)
                {
                    Destroy(rig?.gameObject);
                    foreach (var t in bones)
                    {
                        Destroy(t?.gameObject);
                    }
                    if (gameObject != null) Destroy(gameObject);
                }
            }
            catch (System.Exception)
            { }
        }

        private void C_OnKillEvent(CollisionInstance collisionInstance, EventTime eventTime)
        {
            try
            {
                if (eventTime == EventTime.OnEnd)
                {
                    StartCoroutine(Fade());
                }
            }
            catch (System.Exception)
            { }
        }

        private void Apply()
        {
            rig.SetParent(cc.ragdoll.meshRig);
            rig.localPosition = Vector3.zero;
            rig.localEulerAngles = new Vector3(0, 0, 0);
            rig.localScale = Vector3.one;

            foreach (var b in bones)
            {
                if (cc.ragdoll.bones?.FirstOrDefault(cb => cb.mesh.gameObject.name.Equals(b.gameObject.name + "_Mesh"))?.mesh is { } t)
                {
                    b.SetParent(t);
                    b.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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
            _smInst = new Material(standardMaterial);
            _rhInst = new Material(redHotMaterial);
            _bhInst = new Material(blackHotMaterial);
            _whInst = new Material(whiteHotMaterial);
            
            if (all == null) all = new List<ThermalBody>();
            all.Add(this);
        }

        public void SetColor(NVGOnlyRenderer.ThermalTypes t)
        {
            if (_smInst == null || renderers.Count == 0 || renderers[0] == null) return;
            Material m = null;
            if (t == NVGOnlyRenderer.ThermalTypes.Standard)
            {
                m = _smInst;
            }
            else if (t == NVGOnlyRenderer.ThermalTypes.BlackHot)
            {
                m = _bhInst;
            }
            else if (t == NVGOnlyRenderer.ThermalTypes.RedHot)
            {
                m = _rhInst;
            }
            else if (t == NVGOnlyRenderer.ThermalTypes.WhiteHot)
            {
                m = _whInst;
            }

            foreach (var r in renderers)
            {
                r.material = m;
            }
        }

        private IEnumerator Fade()
        {
            var startingTemperature = standardMaterial.GetFloat("_Temperature");
            var duration = 20f;
            var elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                var t = Mathf.Clamp01(elapsedTime / duration);
                var falloffValue = startingTemperature * (1 - Mathf.Pow(t, 2));
                SetAllMaterialTemperatures(falloffValue);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            Destroy(rig?.gameObject);
            foreach (var t in bones)
            {
                Destroy(t?.gameObject);
            }
            if (gameObject != null) Destroy(gameObject);
        }

        private void SetAllMaterialTemperatures(float temperature)
        {
            _smInst.SetFloat("_Temperature", temperature);
            _rhInst.SetFloat("_Temperature", temperature);
            _whInst.SetFloat("_Temperature", temperature);
            _bhInst.SetFloat("_Temperature", temperature);
        }
    }
}
