using System;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class BetterAudioLoader : MonoBehaviour
    {
        public string audioClipAddress;
        public AudioMixerName audioMixer;
        [NonSerialized]
        public AudioSource AudioSource;

        protected void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
            AudioSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(audioMixer);
            OnEnable();
        }

        protected void OnDungeonGenerated(EventTime eventTime)
        {
            if (eventTime != EventTime.OnEnd || !gameObject.activeInHierarchy || !enabled)
                return;
            OnEnable();
        }

        protected void OnEnable()
        {
            Addressables.LoadAssetAsync<AudioClip>(audioClipAddress).Completed += (handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (!gameObject.activeInHierarchy || !enabled)
                    {
                        Addressables.Release(handle.Result);
                    }
                    else
                    {
                        AudioSource.clip = handle.Result;
                    }
                }
                else Debug.LogError("Could not find audio at address: " + audioClipAddress);
            });
        }


        public void PlayAndDestroy()
        {
            AudioSource.transform.SetParent(null);
            AudioSource.Play((ulong)0.1);
            StartCoroutine(Explosive.DelayedDestroy(AudioSource.gameObject, AudioSource.clip.length + 3f));
        }

        protected void OnDisable()
        {
            if (AudioSource.clip != null) Addressables.Release(AudioSource.clip);
            AudioSource.clip = null;
        }
    }
}
