using System.Collections.Generic;
using System.Collections;
using ThunderRoad;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace GhettosFirearmSDKv2.Chemicals
{
    public class PlayerEffectsAndChemicalsModule : MonoBehaviour
    {
        public static PlayerEffectsAndChemicalsModule local;

        public List<GameObject> gasMasks;

        public Volume volume;

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
        readonly string CSgasCoughAudioContainerId = "CoughingAgony_Ghetto05_FirearmSDKv2";
        AudioContainer CSgasCoughAudioContainer;

        //---EFFECTS---
        readonly string flashBangRingingClipId = "EarRingSoundEffect_Ghetto05_FirearmSDKv2";
        AudioSource flashBangRingingSource;

        void Awake()
        {
            local = this;

            gasMasks = new List<GameObject>();

            //speech
            speech = Player.local.head.cam.gameObject.AddComponent<AudioSource>();
            speech.transform.position = Player.local.head.cam.transform.position;

            //cs gas
            Catalog.LoadAssetAsync<AudioContainer>(CSgasCoughAudioContainerId, AC => { CSgasCoughAudioContainer = AC; }, "Player chemicals module");

            //flash bang
            flashBangRingingSource = Player.local.head.cam.gameObject.AddComponent<AudioSource>();
            flashBangRingingSource.loop = true;
            Catalog.LoadAssetAsync<AudioClip>(flashBangRingingClipId, FBRAC => { flashBangRingingSource.clip = FBRAC; }, "Player chemicals module");

            //post process volume
            volume = Player.local.head.cam.gameObject.AddComponent<Volume>();
            volume.priority = 10;
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<LiftGammaGain>();
        }

        void Update()
        {
            var foundCSgas = false;
            var foundSmoke = false;
            var foundPoisonGas = false;
            var highestPoisonGasDamage = 0f;
            GameObject csgasCollider = null;

            for (var i = 0; i < gasMasks.Count; i++)
            {
                if (gasMasks[i] == null) gasMasks.RemoveAt(i);
            }

            foreach (var c in Physics.OverlapSphere(Player.local.head.cam.transform.position, 0.1f))
            {
                if (c.gameObject.name.Equals("CSgas_Zone") && !WearingGasMask())
                {
                    foundCSgas = true;
                    csgasCollider = c.gameObject;
                }
                if (c.gameObject.name.Equals("Smoke_Zone"))
                {
                    foundSmoke = true;
                }
                if (c.gameObject.name.Equals("PoisonGas_Zone") && !WearingGasMask())
                {
                    foundPoisonGas = true;
                    var d = float.Parse(c.transform.GetChild(0).name);
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
            if (local.volume.profile.TryGet(out LiftGammaGain lgg))
            {
                lgg.active = true;
                lgg.lift.overrideState = true;
                lgg.lift.Override(new Vector4(1f, 1f, 1f, 1f));
                local.StartCoroutine(FlashbangCoroutine(time, lgg));
            }
        }

        static IEnumerator FlashbangCoroutine(float time, LiftGammaGain lgg)
        {
            local.flashBangRingingSource.Play();
            local.flashBangRingingSource.volume = 1f;
            if (time > 4f) yield return new WaitForSeconds(time - 4f);
            yield return FadeOut(lgg, 4f);
        }

        public static IEnumerator FadeOut(LiftGammaGain lgg, float FadeTime)
        {
            var val = 1f;
            while (lgg.lift.value.w > 0)
            {
                val -= Time.deltaTime / FadeTime;
                val = Mathf.Clamp01(val);
                local.flashBangRingingSource.volume = val;
                lgg.lift.Override(new Vector4(1, 1, 1, val));
                yield return null;
            }
        }

        public bool WearingGasMask()
        {
            return gasMasks.Count > 0;
        }
    }
}
