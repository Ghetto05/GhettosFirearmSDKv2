using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Clothing/HUD/HUD module - held weapon ammo counter")]
    public class HUDModuleHeldWeapon : MonoBehaviour
    {
        public HUD hud;
        public Image icon;
        public Text roundCounter;
        public Text capacityDisplay;
        public Firearm currentFirearm;
        public Color defaultColor;
        public Color lowColor;
        public List<GameObject> additionalDisableObjects;

        private void Update()
        {
            SetIcon();
            UpdateRoundCounter();
        }

        public void UpdateRoundCounter()
        {
            if (currentFirearm != null)
            {
                if (currentFirearm.magazineWell != null && currentFirearm.magazineWell.currentMagazine != null)
                {
                    roundCounter.enabled = true;
                    capacityDisplay.enabled = true;
                    foreach (GameObject g in additionalDisableObjects)
                    {
                        g.SetActive(true);
                    }
                    int count = currentFirearm.magazineWell.currentMagazine.cartridges.Count;
                    if (currentFirearm.bolt.GetChamber() != null) count++;
                    roundCounter.text = count.ToString();
                    //float f = (float)currentFirearm.magazineWell.currentMagazine.cartridges.Count / (float)currentFirearm.magazineWell.currentMagazine.maximumCapacity;
                    //if ((f <= 0.1f) || (currentFirearm.bolt.state == BoltBase.BoltState.Locked && currentFirearm.bolt.GetChamber() == null))
                    //{
                    //    roundCounter.color = lowColor;
                    //}
                    //else
                    //{
                    //    roundCounter.color = defaultColor;
                    //}
                    capacityDisplay.text = currentFirearm.magazineWell.currentMagazine.maximumCapacity.ToString();
                    return;
                }
            }
            roundCounter.enabled = false;
            capacityDisplay.enabled = false;
            foreach (GameObject g in additionalDisableObjects)
            {
                g.SetActive(false);
            }
        }

        public void SetIcon()
        {
            if (GetHeldFirearm() is Firearm firearm && icon.sprite == null)
            {
                icon.enabled = true;
                currentFirearm = firearm;
                if (firearm.GetComponent<HUDModuleHeldWeaponOverrideIcon>() is HUDModuleHeldWeaponOverrideIcon ico)
                {
                    icon.sprite = Sprite.Create(ico.overrideIcon, new Rect(0, 0, ico.overrideIcon.width, ico.overrideIcon.height), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    icon.sprite = Sprite.Create((Texture2D) firearm.icon, new Rect(0, 0, firearm.icon.width, firearm.icon.height), new Vector2(0.5f, 0.5f));
                }
            }
            else if (GetHeldFirearm() == null)
            {
                icon.sprite = null;
                icon.enabled = false;
                currentFirearm = null;
            }
        }

        public Firearm GetHeldFirearm()
        {
            Firearm f = null;
            if (Player.local == null || Player.local.creature == null) return null;
            if (Player.local.GetHand(Side.Right).ragdollHand.grabbedHandle is Handle h && h.item is Item i && i.GetComponent<Firearm>() != null)
            {
                f = Player.local.GetHand(Side.Right).ragdollHand.grabbedHandle.item.GetComponent<Firearm>();
            }
            else if (Player.local.GetHand(Side.Left).ragdollHand.grabbedHandle is Handle h2 && h2.item is Item i2 && i2.GetComponent<Firearm>() != null)
            {
                f = Player.local.GetHand(Side.Left).ragdollHand.grabbedHandle.item.GetComponent<Firearm>();
            }
            return f;
        }

        public static Vector2 SizeToParent(RawImage image, float padding = 0)
        {
            float w = 0, h = 0;
            var parent = image.GetComponentInParent<RectTransform>();
            var imageTransform = image.GetComponent<RectTransform>();

            if (image.texture != null)
            {
                if (!parent) { return imageTransform.sizeDelta; }
                padding = 1 - padding;
                float ratio = image.texture.width / (float)image.texture.height;
                var bounds = new Rect(0, 0, parent.rect.width, parent.rect.height);
                if (Mathf.RoundToInt(imageTransform.eulerAngles.z) % 180 == 90)
                {
                    bounds.size = new Vector2(bounds.height, bounds.width);
                }
                h = bounds.height * padding;
                w = h * ratio;
                if (w > bounds.width * padding)
                {
                    w = bounds.width * padding;
                    h = w / ratio;
                }
            }
            imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            return imageTransform.sizeDelta;
        }
    }
}
