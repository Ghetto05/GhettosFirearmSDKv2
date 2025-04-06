using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class ThermalBody : MonoBehaviour
{
    public static List<ThermalBody> all = [];

    public Transform rig;
    public List<Transform> bones;
    public List<SkinnedMeshRenderer> renderers;
    private Creature _cc;

    public Material standardMaterial;
    public Material redHotMaterial;
    public Material whiteHotMaterial;
    public Material blackHotMaterial;

    private Material _smInst;
    private Material _rhInst;
    private Material _whInst;
    private Material _bhInst;
    private static readonly int Temperature = Shader.PropertyToID("_Temperature");

    public void ApplyTo(Creature c)
    {
        _cc = c;
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
                if (gameObject)
                {
                    Destroy(gameObject);
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
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
        catch (Exception)
        {
            // ignored
        }
    }

    private void Apply()
    {
        rig.SetParent(_cc.ragdoll.meshRig);
        rig.localPosition = Vector3.zero;
        rig.localEulerAngles = new Vector3(0, 0, 0);
        rig.localScale = Vector3.one;

        foreach (var b in bones)
        {
            if (_cc.ragdoll.bones?.FirstOrDefault(cb => cb.mesh.gameObject.name.Equals(b.gameObject.name + "_Mesh"))?.mesh is { } t)
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

        all.Add(this);
    }

    public void SetColor(NvgOnlyRenderer.ThermalTypes t)
    {
        if (!_smInst || renderers.Count == 0 || !renderers[0])
        {
            return;
        }
        Material m = null;
        if (t == NvgOnlyRenderer.ThermalTypes.Standard)
        {
            m = _smInst;
        }
        else if (t == NvgOnlyRenderer.ThermalTypes.BlackHot)
        {
            m = _bhInst;
        }
        else if (t == NvgOnlyRenderer.ThermalTypes.RedHot)
        {
            m = _rhInst;
        }
        else if (t == NvgOnlyRenderer.ThermalTypes.WhiteHot)
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
        var startingTemperature = standardMaterial.GetFloat(Temperature);
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
        if (gameObject)
        {
            Destroy(gameObject);
        }
    }

    private void SetAllMaterialTemperatures(float temperature)
    {
        _smInst.SetFloat(Temperature, temperature);
        _rhInst.SetFloat(Temperature, temperature);
        _whInst.SetFloat(Temperature, temperature);
        _bhInst.SetFloat(Temperature, temperature);
    }
}