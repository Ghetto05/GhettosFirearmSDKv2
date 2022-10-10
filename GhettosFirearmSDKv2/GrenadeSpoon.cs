using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class GrenadeSpoon : MonoBehaviour
    {
        public Explosives.Explosive explosive;
        public float fuseTime;
        public Transform startPosition;
        public Transform endPosition;
        public Rigidbody body;
        public float deployForce;
        public Transform forceDir;
        public List<Lock> locks;
        public Item grenadeItem;
        public float deployTime;
        public AudioSource[] deploySounds;
        float startTime = 0f;
        bool moving = false;
        bool triggered = false;

        private void Awake()
        {
            grenadeItem.OnUngrabEvent += GrenadeItem_OnUngrabEvent;
            grenadeItem.OnHeldActionEvent += GrenadeItem_OnHeldActionEvent;
        }

        private void GrenadeItem_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStop)
            {
                Release();
            }
        }

        private void Update()
        {
            if (moving && body != null)
            {
                body.transform.localRotation = Quaternion.Lerp(startPosition.localRotation, endPosition.localRotation, (Time.time - startTime) / deployTime);
            }
            if (Quaternion.Angle(endPosition.localRotation, body.transform.localRotation) < 0.01f && !triggered)
            {
                triggered = true;
                explosive.Detonate(fuseTime);
                body.isKinematic = false;
                body.transform.SetParent(null);
                body.transform.rotation = endPosition.rotation;
                body.transform.position = endPosition.position;
                body.position = endPosition.position;
                body.rotation = endPosition.rotation;
                body.useGravity = true;
                body.AddForce(forceDir.forward * deployForce);
            }
        }

        private void GrenadeItem_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (handle == grenadeItem.mainHandleLeft) StartCoroutine(delayedCheck());
        }

        IEnumerator delayedCheck()
        {
            yield return new WaitForSeconds(0.01f);
            if (grenadeItem.handlers.Count == 0)
            {
                Release();
            }
        }

        public void Release(bool forced = false)
        {
            if (moving) return;
            if (forced || AllLocksReleased())
            {
                startTime = Time.time;
                Util.PlayRandomAudioSource(deploySounds);
                moving = true;
            }
        }

        private bool AllLocksReleased()
        {
            foreach (Lock l in locks)
            {
                if (!l.isUnlocked()) return false;
            }
            return true;
        }
    }
}