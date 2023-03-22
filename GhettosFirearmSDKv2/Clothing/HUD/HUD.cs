using UnityEngine;
using ThunderRoad;
using System.Collections;
using Chabuk.ManikinMono;
using System.Linq;
using System;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Clothing/HUD/HUD")]
    public class HUD : MonoBehaviour
    {
        public GameObject hudObject;
        public Transform scaleRoot;

        private void Awake()
        {
            StartCoroutine(Delayed());
            //Debug.Log("Creature on HUD: " + GetComponentInParent<Creature>().creatureId);
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
            try { scaleRoot.localScale = Vector3.one * Settings_LevelModule.local.hudScale; } catch (Exception e) { }
        }

        private IEnumerator Delayed()
        {
            yield return new WaitForSeconds(0.5f);
            Creature cr = GetComponentInParent<Creature>();
            if (!cr.brain.instance.id.Equals("Player"))
            {
                Destroy(hudObject);
                enabled = false;
                Destroy(this);
            }
            else
            {
                Settings_LevelModule.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
                Settings_LevelModule_OnValueChangedEvent();

                hudObject.transform.SetParent(Player.local.head.cam.transform);
                hudObject.transform.localPosition = Vector3.zero;
                hudObject.transform.localEulerAngles = Vector3.zero;
            }
        }

        private void OnDestroy()
        {
            Destroy(hudObject);
        }
    }
}
