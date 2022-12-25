using System;
using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;
using GhettosFirearmSDKv2.SaveData;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    public class FirearmBase : MonoBehaviour
    {
        public Item item;
        public bool disableMainFireHandle = false;
        public List<Handle> additionalTriggerHandles;
        public bool triggerState;
        public BoltBase bolt;
        public Handle mainFireHandle;
        public MagazineWell magazineWell;
        public Transform hitscanMuzzle;
        public Transform actualHitscanMuzzle;
        public bool integrallySuppressed;
        public AudioSource[] fireSounds;
        public AudioSource[] suppressedFireSounds;
        public FireModes fireMode;
        public ParticleSystem defaultMuzzleFlash;
        public int burstSize = 3;
        public int roundsPerMinute;
        public float lastPressTime = 0f;
        public float longPressTime;
        public float recoilModifier = 1f;
        public bool countingForLongpress = false;

        public enum FireModes
        {
            Safe,
            Semi,
            Burst,
            Auto
        }

        public virtual bool SaveChamber()
        {
            return false;
        }

        public void Item_OnUnSnapEvent(Holder holder)
        {
            OnColliderToggleEvent?.Invoke(true);
        }

        public void Item_OnSnapEvent(Holder holder)
        {
            OnColliderToggleEvent?.Invoke(false);
            if (triggerState)
            {
                ChangeTrigger(false);
            }
        }

        public void ChangeTrigger(bool pulled)
        {
            OnTriggerChangeEvent?.Invoke(pulled);
            triggerState = pulled;
        }

        public virtual void CalculateMuzzle()
        {
        }

        public void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == mainFireHandle || Util.ListContainsHandle(additionalTriggerHandles, handle))
            {
                if (action == Interactable.Action.UseStart)
                {
                    ChangeTrigger(true);
                }
                else if (action == Interactable.Action.UseStop || action == Interactable.Action.Ungrab)
                {
                    ChangeTrigger(false);
                }

                if (action == Interactable.Action.AlternateUseStart)
                {
                    lastPressTime = Time.time;
                    countingForLongpress = true;
                }
                if (action == Interactable.Action.AlternateUseStop && countingForLongpress)
                {
                    countingForLongpress = false;
                    if (Time.time - lastPressTime >= longPressTime) LongPress();
                    else ShortPress();
                }
            }
        }

        public void ShortPress()
        {
            OnAltActionEvent?.Invoke(false);
            if (magazineWell != null && magazineWell.canEject)
            {
                if (!bolt.caught || (bolt.caught && magazineWell.IsEmptyAndHasMagazine())) magazineWell.Eject();
                else if (bolt.caught && !magazineWell.IsEmpty()) bolt.TryRelease();
            }
            else bolt.TryRelease();
        }

        public void LongPress()
        {
            OnAltActionEvent?.Invoke(true);
        }

        public void SetFiremode(FireModes mode)
        {
            fireMode = mode;
            OnFiremodeChangedEvent?.Invoke();
        }

        public virtual bool isSuppressed()
        {
            return false;
        }

        public void PlayFireSound(bool overrideSuppressedbool = false, bool suppressed = false)
        {
            bool supp = isSuppressed();
            if (overrideSuppressedbool) supp = suppressed;
            if (!supp)
            {
                Util.PlayRandomAudioSource(fireSounds);
                Util.AlertAllCreaturesInRange(hitscanMuzzle.position, 50);
            }
            else
            {
                Util.PlayRandomAudioSource(suppressedFireSounds);
            }
        }

        public virtual void PlayMuzzleFlash()
        {
        }

        public bool NoMuzzleFlashOverridingAttachmentChildren(Attachment attachment)
        {
            foreach (AttachmentPoint p in attachment.attachmentPoints)
            {
                if (p.currentAttachment != null)
                {
                    if (!NMFOACrecurve(p.currentAttachment)) return false;
                }
            }
            return true;
        }

        private bool NMFOACrecurve(Attachment attachment)
        {
            foreach (AttachmentPoint p in attachment.attachmentPoints)
            {
                if (p.currentAttachment != null)
                {
                    if (!NMFOACrecurve(p.currentAttachment)) return false;
                }
            }
            if (attachment.overridesMuzzleFlash) return false;
            return true;
        }

        private bool NoAttachments(Attachment attachment)
        {
            foreach (AttachmentPoint p in attachment.attachmentPoints)
            {
                if (p.currentAttachment != null) return false;
            }
            return true;
        }

        public void OnCollisionEnter(Collision collision)
        {
            OnCollisionEvent?.Invoke(collision);
        }

        //EVENTS
        public delegate void OnTriggerChange(bool isPulled);
        public event OnTriggerChange OnTriggerChangeEvent;

        public delegate void OnCollision(Collision collision);
        public event OnCollision OnCollisionEvent;

        public delegate void OnAction(Interactable.Action action);
        public event OnAction OnActionEvent;

        public delegate void OnAltAction(bool longPress);
        public event OnAltAction OnAltActionEvent;

        public delegate void OnToggleColliders(bool active);
        public event OnToggleColliders OnColliderToggleEvent;

        public delegate void OnFiremodeChanged();
        public event OnFiremodeChanged OnFiremodeChangedEvent;
    }
}
