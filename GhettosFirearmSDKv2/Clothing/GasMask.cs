using System.Linq;
using GhettosFirearmSDKv2.Chemicals;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class GasMask : MonoBehaviour
{
    public AudioSource breathingLoop;
    private Creature _creature;
    private NpcChemicalsModule _npcModule;
    private bool _ready;

    private void Awake()
    {
        if (breathingLoop == null && Settings.debugMode)
            Debug.Log($"GAS MASK BREATHING LOOP MISSING: {string.Join("/", gameObject.GetComponentsInParent<Transform>().Reverse().Select(t => t.name).ToArray())}");
    }

    private void Update()
    {
        if (breathingLoop != null)
        {
            if (Settings.playGasMaskSound && !breathingLoop.isPlaying) breathingLoop.Play();
            else if (!Settings.playGasMaskSound && breathingLoop.isPlaying) breathingLoop.Stop();
        }

        if (_creature == null)
        {
            _creature = GetComponentInParent<Creature>();
            if (!_creature.brain.instance.id.Equals("Player")) _npcModule = _creature.GetComponent<NpcChemicalsModule>();
        }
        if (!_ready && _creature != null)
        {
            _ready = true;
            if (enabled) OnEnable();
        }
    }

    private void OnEnable()
    {
        if (!_ready || _creature == null) return;
        if (_creature.isPlayer) AddMaskPlayer();
        else AddMask();
    }

    private void OnDisable()
    {
        if (!_ready || _creature == null) return;
        if (_creature.isPlayer) RemoveMaskPlayer();
        else RemoveMask();
    }

    private void AddMaskPlayer()
    {
        if (!PlayerEffectsAndChemicalsModule.local.gasMasks.Contains(gameObject))
        {
            PlayerEffectsAndChemicalsModule.local.gasMasks.Add(gameObject);
        }
    }

    private void RemoveMaskPlayer()
    {
        if (PlayerEffectsAndChemicalsModule.local.gasMasks.Contains(gameObject))
        {
            PlayerEffectsAndChemicalsModule.local.gasMasks.Remove(gameObject);
        }
    }

    private void AddMask()
    {
        if (!_npcModule.gasMasks.Contains(gameObject))
        {
            _npcModule.gasMasks.Add(gameObject);
        }
    }

    private void RemoveMask()
    {
        if (_npcModule == null) return;
        if (_npcModule.gasMasks.Contains(gameObject))
        {
            _npcModule.gasMasks.Remove(gameObject);
        }
    }
}