using System.Linq;
using UnityEngine;
using ThunderRoad;
using System.Collections.Generic;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    public class FirearmBase : AIFireable
    {
        public bool setUpForHandPose = false;
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
        private float[] fireSoundsPitch;
        public AudioSource[] suppressedFireSounds;
        private float[] suppressedFireSoundsPitch;
        public FireModes fireMode;
        public ParticleSystem defaultMuzzleFlash;
        public int burstSize = 3;
        public int roundsPerMinute;
        public float lastPressTime = 0f;
        public float longPressTime;
        public float recoilModifier = 1f;
        public bool countingForLongpress = false;
        public List<RecoilModifier> recoilModifiers = new List<RecoilModifier>();
        public Light muzzleLight;

        public FirearmSaveData.AttachmentTreeNode GetFirearmNode;

        public virtual void Start()
        {
            fireSoundsPitch = new float[fireSounds.Length];
            suppressedFireSoundsPitch = new float[suppressedFireSounds.Length];
            for (int i = 0; i < fireSounds.Length; i++)
            {
                fireSoundsPitch[i] = fireSounds[i].pitch;
            }
            for (int i = 0; i < suppressedFireSounds.Length; i++)
            {
                suppressedFireSoundsPitch[i] = suppressedFireSounds[i].pitch;
            }
            muzzleLight = new GameObject("MuzzleFlashLight").AddComponent<Light>();
            muzzleLight.enabled = false;
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 5f;
            muzzleLight.intensity = 3f;
        }

        public virtual void Update()
        {
            longPressTime = FirearmsSettings.longPressTime;
        }

        public virtual List<Handle> AllTriggerHandles()
        {
            return null;
        }

        private Color RandomColor(Cartridge cartridge)
        {
            if (cartridge.data.overrideMuzzleFlashLightColor) return Color.Lerp(cartridge.data.muzzleFlashLightColorOne, cartridge.data.muzzleFlashLightColorTwo, Random.Range(0f, 1f));
            else return Color.Lerp(new Color(1.0f, 0.3843f, 0.0f), new Color(1.0f, 0.5294f, 0.0f), Random.Range(0f, 1f));
        }

        public virtual float CalculateDamageMultiplier()
        {
            return 1f;
        }

        public enum FireModes
        {
            Safe,
            Semi,
            Burst,
            Auto,
            AttachmentFirearm
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
            OnMuzzleCalculatedEvent?.Invoke();
        }

        public void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == mainFireHandle || Util.ListContainsHandle(additionalTriggerHandles, handle))
            {
                if (action == Interactable.Action.UseStart)
                {
                    if (!countingForLongpress)
                    {
                        ChangeTrigger(true);
                    }
                    else
                    {
                        countingForLongpress = false;
                        CockAction();
                    }
                }
                else if (action == Interactable.Action.UseStop)
                {
                    ChangeTrigger(false);
                }
                else if (action == Interactable.Action.Ungrab && triggerState)
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
            else if (bolt != null) bolt.TryRelease();
        }

        public void LongPress()
        {
            OnAltActionEvent?.Invoke(true);
        }

        public void CockAction()
        {
            OnCockActionEvent?.Invoke();
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

        public void PlayFireSound(Cartridge cartridge, bool overrideSuppressedbool = false, bool suppressed = false)
        {
            bool supp = isSuppressed();
            if (overrideSuppressedbool) supp = suppressed;
            if (cartridge.data.alwaysSuppressed) supp = true;
            AudioSource source;
            if (!supp)
            {
                if (cartridge.data.overrideFireSounds) source = Util.GetRandomFromList(cartridge.data.fireSounds);
                else source = Util.GetRandomFromList(fireSounds);
            }
            else
            {
                if (cartridge.data.overrideFireSounds) source = Util.GetRandomFromList(cartridge.data.suppressedFireSounds);
                else source = Util.GetRandomFromList(suppressedFireSounds);
            }

            if (source == null) return;
            float pitch = 1f;
            if (!supp)
            {
                NoiseManager.AddNoise(actualHitscanMuzzle.position, 600f);
                Util.AlertAllCreaturesInRange(hitscanMuzzle.position, 50);
                if (!cartridge.data.overrideFireSounds) pitch = fireSoundsPitch[fireSounds.ToList().IndexOf(source)];
            }
            else
            {
                if (!cartridge.data.overrideFireSounds) pitch = suppressedFireSoundsPitch[suppressedFireSounds.ToList().IndexOf(source)];
            }

            float deviation =  FirearmsSettings.firingSoundDeviation / pitch;
            source.pitch = pitch += Random.Range(-deviation, deviation);
            source.Play();
            source.transform.SetParent(transform);
            if (cartridge.data.overrideFireSounds) StartCoroutine(Explosives.Explosive.delayedDestroy(source.gameObject, source.clip.length + 1f)); 
        }

        public virtual void PlayMuzzleFlash(Cartridge cartridge)
        { }

        public IEnumerator PlayMuzzleFlashLight(Cartridge cartridge)
        {
            muzzleLight.color = RandomColor(cartridge);
            muzzleLight.transform.position = actualHitscanMuzzle.position + (actualHitscanMuzzle.forward * 0.04f);
            muzzleLight.enabled = true;
            yield return new WaitForEndOfFrame();
            muzzleLight.enabled = false;
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

        public void InvokeAttachmentAdded(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            OnAttachmentAddedEvent?.Invoke(attachment, attachmentPoint);
        }

        public void InvokeAttachmentRemoved(Attachment attachment, AttachmentPoint attachmentPoint)
        {
            OnAttachmentRemovedEvent?.Invoke(attachment, attachmentPoint);
        }

        public void AddRecoilModifier(float linearModifier, float muzzleRiseModifier, object modifierHandler)
        {
            RecoilModifier modifier = new RecoilModifier
            {
                modifier = linearModifier,
                muzzleRiseModifier = muzzleRiseModifier,
                handler = modifierHandler
            };
            RemoveRecoilModifier(modifierHandler);
            recoilModifiers.Add(modifier);
        }

        public void RemoveRecoilModifier(object modifierHandler)
        {
            foreach (RecoilModifier mod in recoilModifiers.ToArray())
            {
                if (mod.handler == modifierHandler) recoilModifiers.Remove(mod);
            }
        }

        public void RefreshRecoilModifiers()
        {
            foreach (RecoilModifier mod in recoilModifiers.ToArray())
            {
                if (mod.handler == null) recoilModifiers.Remove(mod);
            }
        }

        public void InvokeCollisionTR(CollisionInstance collisionInstance) => OnCollisionEventTR?.Invoke(collisionInstance);

        public class RecoilModifier
        {
            public float modifier;
            public float muzzleRiseModifier;
            public object handler;
        }

        //EVENTS
        public delegate void OnTriggerChange(bool isPulled);
        public event OnTriggerChange OnTriggerChangeEvent;

        public delegate void OnCollision(Collision collision);
        public event OnCollision OnCollisionEvent;

        public delegate void OnCollisionTR(CollisionInstance collisionInstance);
        public event OnCollisionTR OnCollisionEventTR;

        public delegate void OnAction(Interactable.Action action);
        public event OnAction OnActionEvent;

        public delegate void OnAltAction(bool longPress);
        public event OnAltAction OnAltActionEvent;

        public delegate void OnCockAction();
        public event OnCockAction OnCockActionEvent;

        public delegate void OnToggleColliders(bool active);
        public event OnToggleColliders OnColliderToggleEvent;

        public delegate void OnFiremodeChanged();
        public event OnFiremodeChanged OnFiremodeChangedEvent;

        public delegate void OnAttachmentAdded(Attachment attachment, AttachmentPoint attachmentPoint);
        public event OnAttachmentAdded OnAttachmentAddedEvent;

        public delegate void OnAttachmentRemoved(Attachment attachment, AttachmentPoint attachmentPoint);
        public event OnAttachmentRemoved OnAttachmentRemovedEvent;

        public delegate void OnMuzzleCalculated();
        public event OnMuzzleCalculated OnMuzzleCalculatedEvent;
    }
}
