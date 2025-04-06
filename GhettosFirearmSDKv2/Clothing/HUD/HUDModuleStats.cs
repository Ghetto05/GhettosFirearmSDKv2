using ThunderRoad;
using TMPro;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class HUDModuleStats : MonoBehaviour
{
    public Transform health;
    public Transform mana;
    public Transform focus;
    public TextMeshProUGUI timeSlow;

    private void Update()
    {
        if (!Player.local.creature)
        {
            return;
        }
        if (health)
        {
            health.localScale = Scale(Player.local.creature.currentHealth, Player.local.creature.maxHealth);
        }
        //ToDo
        //if (mana)
        //mana.localScale = Scale(Player.local.creature.mana.currentFocus, Player.local.creature.mana.);
        if (focus)
        {
            focus.localScale = Scale(Player.local.creature.mana.currentFocus, Player.local.creature.mana.MaxFocus);
        }
        if (timeSlow)
        {
            timeSlow.text = Time.timeScale * 100 + "%";
        }
    }

    private static Vector3 Scale(float current, float max)
    {
        return new Vector3(1, current / max, 1);
    }
}