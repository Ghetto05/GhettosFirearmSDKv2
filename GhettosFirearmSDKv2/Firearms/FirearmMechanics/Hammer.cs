using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    public class Hammer : MonoBehaviour
    {
        public Item item;
        public Transform hammer;
        public Transform idlePosition;
        public Transform cockedPosition;
        public List<AudioSource> hitSounds;
        private HammerSaveData data;
        public bool cocked;

        private void Awake()
        {
            StartCoroutine(DelayedLoad());
        }

        private IEnumerator DelayedLoad()
        {
            yield return new WaitForSeconds(0.03f);
            if (item.TryGetCustomData(out data))
            {
                if (data.cocked) Cock();
                else Fire(true);
            }
            else
            {
                data = new HammerSaveData();
                data.cocked = false;
                Fire(true);
                item.AddCustomData(data);
            }
            yield break;
        }

        public void Cock()
        {
            data.cocked = true;
            if (hammer != null)
            {
                hammer.localPosition = cockedPosition.localPosition;
                hammer.localEulerAngles = cockedPosition.localEulerAngles;
            }
            cocked = true;
        }

        public void Fire(bool silent = false)
        {
            data.cocked = false;
            cocked = false;
            if (hammer != null)
            {
                hammer.localPosition = idlePosition.localPosition;
                hammer.localEulerAngles = idlePosition.localEulerAngles;
            }
            if (!silent) Util.PlayRandomAudioSource(hitSounds);
        }
    }
}
