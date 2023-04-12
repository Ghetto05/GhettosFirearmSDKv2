﻿using System.Collections;
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
            FirearmsSettings.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
            Settings_LevelModule_OnValueChangedEvent();
            PointerInputModule.SetUICameraToAllCanvas();
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
            infiniteAmmoButton.isOn = FirearmsSettings.values.infiniteAmmo;
            doCaliberChecksButton.isOn = !FirearmsSettings.values.doCaliberChecks;
            doMagazineChecksButton.isOn = !FirearmsSettings.values.doMagazineTypeChecks;
            incapitateOnTorsoShotButton.isOn = FirearmsSettings.values.incapitateOnTorsoShot;
        }

        public void SetInfiniteAmmo()
        {
            FirearmsSettings.values.infiniteAmmo = infiniteAmmoButton.isOn;
            FirearmsSettings.local.SendUpdate();
        }

        public void SetIncapitateOnTorsoShot()
        {
            FirearmsSettings.values.incapitateOnTorsoShot = incapitateOnTorsoShotButton.isOn;
            FirearmsSettings.local.SendUpdate();
        }

        public void SetIgnoreCaliberChecks()
        {
            FirearmsSettings.values.doCaliberChecks = !doCaliberChecksButton.isOn;
            FirearmsSettings.local.SendUpdate();
        }

        public void SetIgnoreMagazineTypeChecks()
        {
            FirearmsSettings.values.doMagazineTypeChecks = !doMagazineChecksButton.isOn;
            FirearmsSettings.local.SendUpdate();
        }
    }
}
