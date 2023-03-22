using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class HUDModuleStats : MonoBehaviour
    {
        public Transform health;
        public Transform mana;
        public Transform focus;
        public Text timeSlow;

        void Update()
        {
            if (Player.local.creature == null) return;
            health.localScale = Scale(Player.local.creature.currentHealth, Player.local.creature.maxHealth);
            mana.localScale = Scale(Player.local.creature.mana.currentFocus, Player.local.creature.mana.maxMana);
            focus.localScale = Scale(Player.local.creature.mana.currentFocus, Player.local.creature.mana.maxFocus);
            timeSlow.text = (Time.timeScale * 100).ToString() + "%";
        }

        private Vector3 Scale(float current, float max)
        {
            return new Vector3(1, current / max, 1);
        }
    }
}
