using System.Collections.Generic;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Clothing/HUD/HUD module - held weapon ammo counter")]
    public class HUDModuleHeldWeapon : MonoBehaviour
    {
        public HUD hud;
        public Image icon;
        public TextMeshProUGUI roundCounter;
        public TextMeshProUGUI capacityDisplay;
        public Firearm currentFirearm;
        public Color defaultColor;
        public Color lowColor;
        public List<GameObject> additionalDisableObjects;

        private void Update()
        {
            if (icon != null)
                SetIcon();
            UpdateRoundCounter();
        }

        public void UpdateRoundCounter()
        {
            currentFirearm = GetHeldFirearm();
            if (currentFirearm != null && currentFirearm.magazineWell != null && currentFirearm.magazineWell.currentMagazine != null)
            {
                var count = currentFirearm.magazineWell.currentMagazine.cartridges.Count;
                if (currentFirearm.bolt.GetChamber() != null)
                {
                    count++;
                }
                if (roundCounter != null)
                    roundCounter.text = count.ToString();
                if (capacityDisplay != null)
                    capacityDisplay.text = currentFirearm.magazineWell.currentMagazine.maximumCapacity.ToString();
            }
            else if (currentFirearm != null && currentFirearm.bolt.GetChamber() != null)
            {
                if (roundCounter != null)
                    roundCounter.text = 1.ToString();
                if (capacityDisplay != null)
                    capacityDisplay.text = 0.ToString();
            }
            else
            {
                if (roundCounter != null)
                    roundCounter.text = 0.ToString();
                if (capacityDisplay != null)
                    capacityDisplay.text = 0.ToString();
            }
        }

        public void SetIcon()
        {
            if (GetHeldFirearm() is { } firearm && icon.sprite == null)
            {
                ToggleHUD(true);
                currentFirearm = firearm;
                if (firearm.GetComponent<HUDModuleHeldWeaponOverrideIcon>() is { } ico)
                {
                    icon.sprite = Sprite.Create(ico.overrideIcon, new Rect(0, 0, ico.overrideIcon.width, ico.overrideIcon.height), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    Catalog.LoadAssetAsync<Texture2D>(firearm.item.data.iconAddress, t =>
                    {
                        icon.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                    }, "HUD Module Held Weapon");
                }
            }
            else if (GetHeldFirearm() == null)
            {
                icon.sprite = null;
                currentFirearm = null;
                ToggleHUD(false);
            }
        }

        private void ToggleHUD(bool hudEnabled)
        {
            icon.enabled = hudEnabled;
            if (roundCounter != null) roundCounter.enabled = hudEnabled;
            if (capacityDisplay != null) capacityDisplay.enabled = hudEnabled;
            for (var i = 0; i < additionalDisableObjects.Count; i++)
            {
                additionalDisableObjects[i].SetActive(hudEnabled);
            }
        }

        public Firearm GetHeldFirearm()
        {
            Firearm f = null;
            if (Player.local == null || Player.local.creature == null)
                return null;
            if (Player.local.GetHand(Side.Right).ragdollHand.grabbedHandle is { } h && h.item is { } i && i.GetComponent<Firearm>() != null)
            {
                f = Player.local.GetHand(Side.Right).ragdollHand.grabbedHandle.item.GetComponent<Firearm>();
            }
            else if (Player.local.GetHand(Side.Left).ragdollHand.grabbedHandle is { } h2 && h2.item is { } i2 && i2.GetComponent<Firearm>() != null)
            {
                f = Player.local.GetHand(Side.Left).ragdollHand.grabbedHandle.item.GetComponent<Firearm>();
            }
            return f;
        }
    }
}
