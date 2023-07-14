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
        public Transform body;
        public Rigidbody rb;
        public float deployForce;
        public Transform forceDir;
        public List<Lock> locks;
        public Item grenadeItem;
        public float deployTime;
        public AudioSource[] deploySounds;
        float startTime = 0f;
        bool moving = false;
        bool triggered = false;

        private void Start()
        {
            grenadeItem.OnUngrabEvent += GrenadeItem_OnUngrabEvent;
            grenadeItem.OnHeldActionEvent += GrenadeItem_OnHeldActionEvent;
            foreach (Lock l in locks)
            {
                l.ChangedEvent += L_ChangedEvent;
            }
        }

        private void L_ChangedEvent()
        {
            StartCoroutine(delayedCheck());
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
            if (moving && body != null && !triggered)
            {
                body.localRotation = Quaternion.Lerp(startPosition.localRotation, endPosition.localRotation, (Time.time - startTime) / deployTime);
            }
            if (Quaternion.Angle(endPosition.localRotation, body.transform.localRotation) < 0.01f && !triggered)
            {
                triggered = true;
                rb = body.gameObject.AddComponent<Rigidbody>();
                explosive.Detonate(fuseTime);
                body.SetParent(null);
                body.rotation = endPosition.rotation;
                body.position = endPosition.position;
                rb.velocity = grenadeItem.physicBody.velocity;
                rb.useGravity = true;
                StartCoroutine(DelayedAddForce(rb));
            }
        }

        IEnumerator DelayedAddForce(Rigidbody rb)
        {
            yield return new WaitForSeconds(0.01f);
            rb.AddForce(forceDir.forward * deployForce * 10);
        }

        private void GrenadeItem_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (handle == grenadeItem.mainHandleLeft) StartCoroutine(delayedCheck());
        }

        IEnumerator delayedCheck()
        {
            yield return new WaitForSeconds(0.01f);
            if (grenadeItem.mainHandleRight.handlers.Count < 1)
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
                if (!l.IsUnlocked()) return false;
            }
            return true;
        }
    }
}