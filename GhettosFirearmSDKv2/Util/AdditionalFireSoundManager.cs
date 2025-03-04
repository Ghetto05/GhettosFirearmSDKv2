using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AdditionalFireSoundManager : MonoBehaviour
{
    public BoltBase bolt;
    public List<AudioSource> sounds;

    private void Start()
    {
        bolt.OnFireEvent += Bolt_OnFireEvent;
    }

    private void Bolt_OnFireEvent()
    {
        Util.PlayRandomAudioSource(sounds);
    }
}