using System.Collections;
using UnityEngine;
using ThunderRoad;
using GhettosFirearmSDKv2.Chemicals;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class GasMask : MonoBehaviour
    {
        public AudioSource breathingLoop;
        private Creature creature;
        private NPCChemicalsModule npcModule;
        private bool ready = false;

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

            if (creature == null)
            {
                creature = GetComponentInParent<Creature>();
                if (!creature.brain.instance.id.Equals("Player")) npcModule = creature.GetComponent<NPCChemicalsModule>();
            }
            if (!ready && creature != null)
            {
                ready = true;
                if (enabled) OnEnable();
            }
        }

        private void OnEnable()
        {
            if (!ready || creature == null) return;
            if (creature.isPlayer) AddMaskPlayer();
            //else AddMask();
        }

        private void OnDisable()
        {
            if (!ready || creature == null) return;
            if (creature.isPlayer) RemoveMaskPlayer();
            //else RemoveMask();
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
            if (!npcModule.gasMasks.Contains(gameObject))
            {
                npcModule.gasMasks.Add(gameObject);
            }
        }

        private void RemoveMask()
        {
            if (npcModule == null) return;
            if (npcModule.gasMasks.Contains(gameObject))
            {
                npcModule.gasMasks.Remove(gameObject);
            }
        }
    }
}