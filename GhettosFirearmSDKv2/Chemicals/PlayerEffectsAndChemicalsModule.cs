using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GhettosFirearmSDKv2.Chemicals
{
    public class PlayerEffectsAndChemicalsModule : MonoBehaviour
    {
        public static PlayerEffectsAndChemicalsModule local;

        public List<GameObject> gasMasks;

        public Volume volume;

        //---STATE---
        private bool _inCSgas;
        private bool _inSmoke;
        private bool _inPoisonGas;

        //---SPEECH---
        private AudioSource _speech;
        private float _nextSpeechTime;
        private float _delayBetweenSpeechMin = 2f;
        private float _delayBetweenSpeechMax = 5f;

        //CS gas
        private readonly string _cSgasCoughAudioContainerId = "CoughingAgony_Ghetto05_FirearmSDKv2";
        private AudioContainer _cSgasCoughAudioContainer;

        //---EFFECTS---
        private readonly string _flashBangRingingClipId = "EarRingSoundEffect_Ghetto05_FirearmSDKv2";
        private AudioSource _flashBangRingingSource;

        private void Awake()
        {
            local = this;

            gasMasks = new List<GameObject>();

            //speech
            _speech = Player.local.head.cam.gameObject.AddComponent<AudioSource>();
            _speech.transform.position = Player.local.head.cam.transform.position;

            //cs gas
            Catalog.LoadAssetAsync<AudioContainer>(_cSgasCoughAudioContainerId, ac => { _cSgasCoughAudioContainer = ac; }, "Player chemicals module");

            //flash bang
            _flashBangRingingSource = Player.local.head.cam.gameObject.AddComponent<AudioSource>();
            _flashBangRingingSource.loop = true;
            Catalog.LoadAssetAsync<AudioClip>(_flashBangRingingClipId, fbrac => { _flashBangRingingSource.clip = fbrac; }, "Player chemicals module");

            //post process volume
            volume = Player.local.head.cam.gameObject.AddComponent<Volume>();
            volume.priority = 10;
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<LiftGammaGain>();
        }

        private void Update()
        {
            var foundCSgas = false;
            var foundSmoke = false;
            var foundPoisonGas = false;
            var highestPoisonGasDamage = 0f;

            for (var i = 0; i < gasMasks.Count; i++)
            {
                if (gasMasks[i] == null) gasMasks.RemoveAt(i);
            }

            var hits = Physics.OverlapSphere(Player.local.head.cam.transform.position, 0.1f);
            foreach (var c in hits)
            {
                if (c.gameObject.name.Equals("CSgas_Zone") && !WearingGasMask())
                {
                    foundCSgas = true;
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

            if (foundSmoke && !_inSmoke) EnterSmoke();
            else if (!foundSmoke && _inSmoke) ExitSmoke();

            if (foundCSgas && !_inCSgas) EnterCSgas();
            else if (!foundCSgas && _inCSgas) ExitCSgas();

            if (foundPoisonGas && !_inPoisonGas) EnterPoisonGas();
            else if (!foundPoisonGas && _inPoisonGas) ExitPoisonGas();

            UpdateCSgas();
            UpdateSmoke();
            UpdatePoisonGas(highestPoisonGasDamage * Time.deltaTime);

            if (Time.time >= _nextSpeechTime) Speak();
        }

        private void Speak()
        {
            if (_inCSgas && _cSgasCoughAudioContainer != null)
            {
                _speech.clip = _cSgasCoughAudioContainer.PickAudioClip();
            }
            else
            {
                _speech.clip = null;
            }

            //---END---
            if (_speech.clip != null)
            {
                _speech.Play();
                _nextSpeechTime = Time.time + Random.Range(_delayBetweenSpeechMin, _delayBetweenSpeechMax) + _speech.clip.length;
            }
        }

        public bool IsInSmoke()
        {
            return _inSmoke;
        }

        private void UpdateSmoke()
        {
            
        }

        private void EnterSmoke()
        {
            _inSmoke = true;
        }

        private void ExitSmoke()
        {
            _inSmoke = false;
        }

        private void UpdateCSgas()
        {
            
        }

        private void EnterCSgas()
        {
            _inCSgas = true;
        }

        private void ExitCSgas()
        {
            _inCSgas = false;
        }

        private void UpdatePoisonGas(float damage)
        {
            if (!_inPoisonGas) return;
            Player.local.creature.Damage(new CollisionInstance(new DamageStruct(DamageType.Energy, damage)));
        }

        private void EnterPoisonGas()
        {
            _inPoisonGas = true;
        }

        private void ExitPoisonGas()
        {
            _inPoisonGas = false;
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

        private static IEnumerator FlashbangCoroutine(float time, LiftGammaGain lgg)
        {
            local._flashBangRingingSource.Play();
            local._flashBangRingingSource.volume = 1f;
            if (time > 4f) yield return new WaitForSeconds(time - 4f);
            yield return FadeOut(lgg, 4f);
        }

        public static IEnumerator FadeOut(LiftGammaGain lgg, float fadeTime)
        {
            var val = 1f;
            while (lgg.lift.value.w > 0)
            {
                val -= Time.deltaTime / fadeTime;
                val = Mathf.Clamp01(val);
                local._flashBangRingingSource.volume = val;
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
