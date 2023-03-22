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
        public Toggle forceDespawnCasingsButton;
        public Text despawnCasingsTimeDisplay;
        public UnityEngine.UI.Slider despawnCasingsTimeSlider;

        [Header("Fire sound volume")]
        public Text fireSoundVolumeDisplay;
        public UnityEngine.UI.Slider fireSoundsVolumeSlider;

        [Header("HUD scale")]
        public Text hudScaleDisplay;

        void Awake()
        {
            Settings_LevelModule.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
            Settings_LevelModule_OnValueChangedEvent();
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
            //damage multiplier
            damageMultiplierValue.text = "Damage times " + Settings_LevelModule.local.damageMultiplier;
            //no phys mags
            disableMagazineCollisionsButton.isOn = Settings_LevelModule.local.magazinesHaveNoCollision;
            //hud scale
            hudScaleDisplay.text = "HUD Scale: " + Settings_LevelModule.local.hudScale;
        }

        public void ChangeDamageMultiplier(float value)
        {
            Settings_LevelModule.local.damageMultiplier += value;
            Settings_LevelModule.local.SendUpdate();
        }

        public void ToggleDisableMagazineCollisions()
        {
            Settings_LevelModule.local.magazinesHaveNoCollision = disableMagazineCollisionsButton.isOn;
            Settings_LevelModule.local.SendUpdate();
        }

        public void ChangeHUDScale(float value)
        {
            Settings_LevelModule.local.hudScale += value;
            Settings_LevelModule.local.SendUpdate();
        }
    }
}