using System.Collections.Generic;
using System.Collections;
using ThunderRoad;
using UnityEngine.Rendering;
using UnityEngine;

namespace GhettosFirearmSDKv2.Chemicals
{
    public class PlayerEffectsAndChemicalsModule : MonoBehaviour
    {
        public static PlayerEffectsAndChemicalsModule local;

        //---STATE---
        bool inCSgas = false;
        bool inSmoke = false;
        bool inPoisonGas = false;

        //---SPEECH---
        AudioSource speech;
        float nextSpeechTime;
        float delayBetweenSpeechMin = 2f;
        float delayBetweenSpeechMax = 5f;

        //CS gas

        Volume csGasVolume;
        readonly string CSgasCoughAudioContainerId = "CoughingAgony_Ghetto05_FirearmSDKv2";
        AudioContainer CSgasCoughAudioContainer;

        //---EFFECTS---
        readonly string flashBangRingingClipId = "EarRingSoundEffect_Ghetto05_FirearmSDKv2";
        AudioSource flashBangRingingSource;

        void Awake()
        {
            local = this;

            //speech
            speech = Player.local.head.cam.gameObject.AddComponent<AudioSource>();
            speech.transform.position = Player.local.head.cam.transform.position;

            //cs gas
            Catalog.LoadAssetAsync<AudioContainer>(CSgasCoughAudioContainerId, AC => { CSgasCoughAudioContainer = AC; }, "Player chemicals module");

            //flash bang
            flashBangRingingSource = Player.local.head.cam.gameObject.AddComponent<AudioSource>();
            flashBangRingingSource.loop = true;
            Catalog.LoadAssetAsync<AudioClip>(flashBangRingingClipId, FBRAC => { flashBangRingingSource.clip = FBRAC; }, "Player chemicals module");
        }

        void Update()
        {
            bool foundCSgas = false;
            bool foundSmoke = false;
            bool foundPoisonGas = false;
            float highestPoisonGasDamage = 0f;
            GameObject csgasCollider = null;
            foreach (Collider c in Physics.OverlapSphere(Player.local.head.cam.transform.position, 0.1f))
            {
                if (c.gameObject.name.Equals("CSgas_Zone"))
                {
                    foundCSgas = true;
                    csgasCollider = c.gameObject;
                }
                if (c.gameObject.name.Equals("Smoke_Zone"))
                {
                    foundSmoke = true;
                }
                if (c.gameObject.name.Equals("PoisonGas_Zone"))
                {
                    foundPoisonGas = true;
                    float d = float.Parse(c.transform.GetChild(0).name);
                    if (d > highestPoisonGasDamage) highestPoisonGasDamage = d;
                }
            }

            if (foundSmoke && !inSmoke) EnterSmoke();
            else if (!foundSmoke && inSmoke) ExitSmoke();

            if (foundCSgas && !inCSgas) EnterCSgas(csgasCollider);
            else if (!foundCSgas && inCSgas) ExitCSgas();

            if (foundPoisonGas && !inPoisonGas) EnterPoisonGas();
            else if (!foundPoisonGas && inPoisonGas) ExitPoisonGas();

            UpdateCSgas();
            UpdateSmoke();
            UpdatePoisonGas(highestPoisonGasDamage * Time.deltaTime);

            if (Time.time >= nextSpeechTime) Speak();
        }

        void Speak()
        {
            if (inCSgas && CSgasCoughAudioContainer != null)
            {
                speech.clip = CSgasCoughAudioContainer.PickAudioClip();
            }
            else
            {
                speech.clip = null;
            }

            //---END---
            if (speech.clip != null)
            {
                speech.Play();
                nextSpeechTime = Time.time + Random.Range(delayBetweenSpeechMin, delayBetweenSpeechMax) + speech.clip.length;
            }
        }

        public bool IsInSmoke()
        {
            return inSmoke;
        }


        void UpdateSmoke()
        {
            if (!inSmoke) return;
        }

        void EnterSmoke()
        {
            inSmoke = true;
        }

        void ExitSmoke()
        {
            inSmoke = false;
        }

        void UpdateCSgas()
        {
            if (!inCSgas) return;
        }

        void EnterCSgas(GameObject obj)
        {
            inCSgas = true;
            csGasVolume = obj.AddComponent<Volume>();
            csGasVolume.isGlobal = false;
            csGasVolume.priority = 0;
            csGasVolume.weight = 1f;
        }

        void ExitCSgas()
        {
            inCSgas = false;
        }

        void UpdatePoisonGas(float damage)
        {
            if (!inPoisonGas) return;
            Player.local.creature.Damage(new CollisionInstance(new DamageStruct(DamageType.Energy, damage)));
        }

        void EnterPoisonGas()
        {
            inPoisonGas = true;
        }

        void ExitPoisonGas()
        {
            inPoisonGas = false;
        }

        public static void Flashbang(float time)
        {
            CameraEffects.DoTimedEffect(Color.grey, CameraEffects.TimedEffect.Flash, time);
            local.StartCoroutine(FlashbangCoroutine(time));
        }

        static IEnumerator FlashbangCoroutine(float time)
        {
            local.flashBangRingingSource.Play();
            if (time > 2f) yield return new WaitForSeconds(time - 2f);
            yield return Utils.FadeOut(local.flashBangRingingSource, 2f);
        }
    }
}
