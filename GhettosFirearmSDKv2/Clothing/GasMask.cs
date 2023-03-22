using System.Collections;
using UnityEngine;
using ThunderRoad;
using GhettosFirearmSDKv2.Chemicals;

namespace GhettosFirearmSDKv2
{
    public class GasMask : MonoBehaviour
    {
        private Creature creature;
        private NPCChemicalsModule npcModule;
        private bool ready = false;

        private void Awake()
        {
        }

        private void Update()
        {
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
            if (!PlayerEffectsAndChemicalsModule.local.gasMasks.Contains(this.gameObject))
            {
                PlayerEffectsAndChemicalsModule.local.gasMasks.Add(this.gameObject);
            }
        }

        private void RemoveMaskPlayer()
        {
            if (PlayerEffectsAndChemicalsModule.local.gasMasks.Contains(this.gameObject))
            {
                PlayerEffectsAndChemicalsModule.local.gasMasks.Remove(this.gameObject);
            }
        }

        private void AddMask()
        {
            if (!npcModule.gasMasks.Contains(this.gameObject))
            {
                npcModule.gasMasks.Add(this.gameObject);
            }
        }

        private void RemoveMask()
        {
            if (npcModule == null) return;
            if (npcModule.gasMasks.Contains(this.gameObject))
            {
                npcModule.gasMasks.Remove(this.gameObject);
            }
        }
    }
}