// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using ThunderRoad;
//
// namespace GhettosFirearmSDKv2.UI
// {
//     public class CheatsWindow : MonoBehaviour
//     {
//         public Toggle infiniteAmmoButton;
//         public Toggle doCaliberChecksButton;
//         public Toggle doMagazineChecksButton;
//         public Toggle incapitateOnTorsoShotButton;
//
//         private void Awake()
//         {
//             FirearmsSettings.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
//             Settings_LevelModule_OnValueChangedEvent();
//             PointerInputModule.SetUICameraToAllCanvas();
//         }
//
//         private void Settings_LevelModule_OnValueChangedEvent()
//         {
//             infiniteAmmoButton.isOn = FirearmsSettings.infiniteAmmo;
//             doCaliberChecksButton.isOn = !FirearmsSettings.doCaliberChecks;
//             doMagazineChecksButton.isOn = !FirearmsSettings.doMagazineTypeChecks;
//             //incapitateOnTorsoShotButton.isOn = FirearmsSettings.incapitateOnTorsoShot;
//         }
//
//         public void SetInfiniteAmmo()
//         {
//             FirearmsSettings.infiniteAmmo = infiniteAmmoButton.isOn;
//             FirearmsSettings.local.SendUpdate();
//         }
//
//         public void SetIncapitateOnTorsoShot()
//         {
//             //FirearmsSettings.incapitateOnTorsoShot = incapitateOnTorsoShotButton.isOn;
//             FirearmsSettings.local.SendUpdate();
//         }
//
//         public void SetIgnoreCaliberChecks()
//         {
//             FirearmsSettings.doCaliberChecks = !doCaliberChecksButton.isOn;
//             FirearmsSettings.local.SendUpdate();
//         }
//
//         public void SetIgnoreMagazineTypeChecks()
//         {
//             FirearmsSettings.doMagazineTypeChecks = !doMagazineChecksButton.isOn;
//             FirearmsSettings.local.SendUpdate();
//         }
//     }
// }
