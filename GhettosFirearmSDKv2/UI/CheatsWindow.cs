using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ThunderRoad;

namespace GhettosFirearmSDKv2.UI
{
    public class CheatsWindow : MonoBehaviour
    {
        public Toggle infiniteAmmoButton;
        public Toggle doCaliberChecksButton;
        public Toggle doMagazineChecksButton;
        public Toggle incapitateOnTorsoShotButton;

        private void Awake()
        {
            Settings_LevelModule.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
            Settings_LevelModule_OnValueChangedEvent();
            PointerInputModule.SetUICameraToAllCanvas();
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
            infiniteAmmoButton.isOn = Settings_LevelModule.local.infiniteAmmo;
            doCaliberChecksButton.isOn = !Settings_LevelModule.local.doCaliberChecks;
            doMagazineChecksButton.isOn = !Settings_LevelModule.local.doMagazineTypeChecks;
            incapitateOnTorsoShotButton.isOn = Settings_LevelModule.local.incapitateOnTorsoShot;
        }

        public void SetInfiniteAmmo()
        {
            Settings_LevelModule.local.infiniteAmmo = infiniteAmmoButton.isOn;
            Settings_LevelModule.local.SendUpdate();
        }

        public void SetIncapitateOnTorsoShot()
        {
            Settings_LevelModule.local.incapitateOnTorsoShot = incapitateOnTorsoShotButton.isOn;
            Settings_LevelModule.local.SendUpdate();
        }

        public void SetIgnoreCaliberChecks()
        {
            Settings_LevelModule.local.doCaliberChecks = !doCaliberChecksButton.isOn;
            Settings_LevelModule.local.SendUpdate();
        }

        public void SetIgnoreMagazineTypeChecks()
        {
            Settings_LevelModule.local.doMagazineTypeChecks = !doMagazineChecksButton.isOn;
            Settings_LevelModule.local.SendUpdate();
        }
    }
}
