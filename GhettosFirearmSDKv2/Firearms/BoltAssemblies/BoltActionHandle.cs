using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class BoltActionHandle : MonoBehaviour
    {
        public BoltBase bolt;
        public Transform handle;
        public Transform lockedPosition;
        public Transform openedPosition;

        public List<AudioSource> handleUpSounds;
        public List<AudioSource> handleDownSounds;

        private bool _lastFrameIsHeld;

        private void FixedUpdate()
        {
            if (bolt.firearm.roundsPerMinute == 0)
            {
                if (bolt.state != BoltBase.BoltState.Locked)
                {
                    handle.localEulerAngles = openedPosition.localEulerAngles;
                }
                else
                {
                    handle.localEulerAngles = bolt.isHeld ? openedPosition.localEulerAngles : lockedPosition.localEulerAngles;
                    if (_lastFrameIsHeld != bolt.isHeld)
                    {
                        Util.PlayRandomAudioSource(bolt.isHeld ? handleUpSounds : handleDownSounds);
                    }
                }

                _lastFrameIsHeld = bolt.isHeld;
            }
            else
            {
                if (bolt.state != BoltBase.BoltState.Locked)
                {
                    handle.localEulerAngles = openedPosition.localEulerAngles;
                }
                else
                {
                    handle.localEulerAngles = lockedPosition.localEulerAngles;
                }
            }
        }
    }
}