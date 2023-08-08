using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ThunderRoad;

namespace GhettosFirearmSDKv2.UI
{
    public class SettingsWindow : MonoBehaviour
    {
        [Header("Damage multiplier")]
        public Text damageMultiplierValue;

        [Header("Disable magazine collisions")]
        public Toggle disableMagazineCollisionsButton;

        [Header("Forced casing despawn")]
        public Text despawnCasingsTimeDisplay;
        public Slider despawnCasingsTimeSlider;

        [Header("HUD scale")]
        public Text hudScaleDisplay;

        [Header("Long press time")]
        public Text longPressTimeDisplay;

        [Header("Revolver trigger deadzone")]
        public Text revolverTriggerDeadzoneDisplay;
        public Slider revolverTriggerDeadzoneSlider;

        void Awake()
        {
            DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData("All settings panel options were moved to the settings book. Please open the book and go to Mods > Ghetto's Firearm Framework.", "", "", "", 100));
            GetComponentInParent<Item>().Despawn();

            //FirearmsSettings.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
            //despawnCasingsTimeSlider.onValueChanged.AddListener(UpdateCartridgeDespawnTime);
            //revolverTriggerDeadzoneSlider.onValueChanged.AddListener(UpdateRevolverTriggerDeadzone);
            //Settings_LevelModule_OnValueChangedEvent();
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
            //damage multiplier
            damageMultiplierValue.text = "Damage times " + FirearmsSettings.damageMultiplier;
            //no phys mags
            disableMagazineCollisionsButton.isOn = FirearmsSettings.magazinesHaveNoCollision;
            //hud scale
            hudScaleDisplay.text = "HUD Scale: " + FirearmsSettings.hudScale;

            despawnCasingsTimeSlider.value = FirearmsSettings.cartridgeDespawnTime;
            bool flag = !Mathf.Approximately(FirearmsSettings.cartridgeDespawnTime, 0f);
            string s = flag ? "after " + FirearmsSettings.cartridgeDespawnTime.ToString() + " second(s)" : "disabled";
            despawnCasingsTimeDisplay.text = "Force despawn casings:\n" + s;


            revolverTriggerDeadzoneSlider.value = FirearmsSettings.revolverTriggerDeadzone;
            revolverTriggerDeadzoneDisplay.text = "Revolver trigger deadzone:\n" + FirearmsSettings.revolverTriggerDeadzone;
        }

        public void ChangeDamageMultiplier(float value)
        {
            FirearmsSettings.damageMultiplier += value;
            FirearmsSettings.local.SendUpdate();
        }

        public void ToggleDisableMagazineCollisions()
        {
            FirearmsSettings.magazinesHaveNoCollision = disableMagazineCollisionsButton.isOn;
            FirearmsSettings.local.SendUpdate();
        }

        public void ChangeHUDScale(float value)
        {
            FirearmsSettings.hudScale += value;
            FirearmsSettings.local.SendUpdate();
        }

        public void UpdateCartridgeDespawnTime(float value)
        {
            FirearmsSettings.cartridgeDespawnTime = value;
            FirearmsSettings.local.SendUpdate();
        }

        public void UpdateRevolverTriggerDeadzone(float value)
        {
            FirearmsSettings.revolverTriggerDeadzone = value;
            FirearmsSettings.local.SendUpdate();
        }

        public void ChangeLongPressTime(float value)
        {
            FirearmsSettings.longPressTime += value;
            FirearmsSettings.local.SendUpdate();
        }
    }
}