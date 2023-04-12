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
        public UnityEngine.UI.Slider despawnCasingsTimeSlider;

        [Header("HUD scale")]
        public Text hudScaleDisplay;

        [Header("Long press time")]
        public Text longPressTimeDisplay;

        [Header("Revolver trigger deadzone")]
        public Text revolverTriggerDeadzoneDisplay;
        public UnityEngine.UI.Slider revolverTriggerDeadzoneSlider;

        void Awake()
        {
            FirearmsSettings.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
            despawnCasingsTimeSlider.onValueChanged.AddListener(UpdateCartridgeDespawnTime);
            Settings_LevelModule_OnValueChangedEvent();
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
            //damage multiplier
            damageMultiplierValue.text = "Damage times " + FirearmsSettings.values.damageMultiplier;
            //no phys mags
            disableMagazineCollisionsButton.isOn = FirearmsSettings.values.magazinesHaveNoCollision;
            //hud scale
            hudScaleDisplay.text = "HUD Scale: " + FirearmsSettings.values.hudScale;

            despawnCasingsTimeSlider.value = FirearmsSettings.values.cartridgeDespawnTime;
            bool flag = !Mathf.Approximately(FirearmsSettings.values.cartridgeDespawnTime, 0f);
            string s = flag ? "after " + FirearmsSettings.values.cartridgeDespawnTime.ToString() + " second(s)" : "disabled";
            despawnCasingsTimeDisplay.text = "Force despawn casings:\n" + s;


            revolverTriggerDeadzoneSlider.value = FirearmsSettings.values.revolverTriggerDeadzone;
            revolverTriggerDeadzoneDisplay.text = "Revolver trigger deadzone:\n" + FirearmsSettings.values.revolverTriggerDeadzone;
        }

        public void ChangeDamageMultiplier(float value)
        {
            FirearmsSettings.values.damageMultiplier += value;
            FirearmsSettings.local.SendUpdate();
        }

        public void ToggleDisableMagazineCollisions()
        {
            FirearmsSettings.values.magazinesHaveNoCollision = disableMagazineCollisionsButton.isOn;
            FirearmsSettings.local.SendUpdate();
        }

        public void ChangeHUDScale(float value)
        {
            FirearmsSettings.values.hudScale += value;
            FirearmsSettings.local.SendUpdate();
        }

        public void UpdateCartridgeDespawnTime(float value)
        {
            FirearmsSettings.values.cartridgeDespawnTime = value;
            FirearmsSettings.local.SendUpdate();
        }

        public void UpdateRevolverTriggerDeadzone(float value)
        {
            FirearmsSettings.values.revolverTriggerDeadzone = value;
            FirearmsSettings.local.SendUpdate();
        }

        public void ChangeLongPressTime(float value)
        {
            FirearmsSettings.values.longPressTime += value;
            FirearmsSettings.local.SendUpdate();
        }
    }
}