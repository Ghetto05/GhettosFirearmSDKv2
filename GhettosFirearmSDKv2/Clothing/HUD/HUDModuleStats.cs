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
        if (Player.local.creature == null)
            return;
        if (health != null)
            health.localScale = Scale(Player.local.creature.currentHealth, Player.local.creature.maxHealth);
        //ToDo
        //if (mana != null)
        //mana.localScale = Scale(Player.local.creature.mana.currentFocus, Player.local.creature.mana.);
        if (focus != null)
            focus.localScale = Scale(Player.local.creature.mana.currentFocus, Player.local.creature.mana.MaxFocus);
        if (timeSlow != null)
            timeSlow.text = Time.timeScale * 100 + "%";
    }

    private static Vector3 Scale(float current, float max)
    {
        return new Vector3(1, current / max, 1);
    }
}