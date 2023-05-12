using System;
using UnityEngine;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class BetterAudioLoader : MonoBehaviour
    {
        public string audioClipAddress;
        public AudioMixerName audioMixer;
        [NonSerialized]
        public AudioSource audioSource;

        protected void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(audioMixer);
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
                        audioSource.clip = handle.Result;
                    }
                }
                else Debug.LogError("Could not find audio at address: " + audioClipAddress);
            });
        }


        public void PlayAndDestroy()
        {
            audioSource.transform.SetParent(null);
            audioSource.Play((ulong)0.1);
            StartCoroutine(Explosives.Explosive.delayedDestroy(audioSource.gameObject, audioSource.clip.length + 3f));
        }

        protected void OnDisable()
        {
            if (audioSource.clip != null) Addressables.Release(audioSource.clip);
            audioSource.clip = null;
        }
    }
}
