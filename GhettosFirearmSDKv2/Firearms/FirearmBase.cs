using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2
{
    public class FirearmBase : AIFireable
    {
        public static List<FirearmBase> all = new();
        
        public bool setUpForHandPose;
        public bool disableMainFireHandle;
        public List<Handle> additionalTriggerHandles;
        public bool triggerState;
        public BoltBase bolt;
        public Handle mainFireHandle;
        public MagazineWell magazineWell;
        public Transform hitscanMuzzle;
        public Transform actualHitscanMuzzle;
        public bool integrallySuppressed;
        public AudioSource[] fireSounds;
        private float[] _fireSoundsPitch;
        public AudioSource[] suppressedFireSounds;
        private float[] _suppressedFireSoundsPitch;
        public FireModes fireMode;
        public ParticleSystem defaultMuzzleFlash;
        public int burstSize = 3;
        public float roundsPerMinute;
        public float lastPressTime;
        public float longPressTime;
        public float recoilModifier = 1f;
        public bool countingForLongpress;
        public List<RecoilModifier> RecoilModifiers = new();
        public Light muzzleLight;
        public string defaultAmmoItem;
        public SaveNodeValueMagazineContents SavedAmmoData;

        public FirearmSaveData.AttachmentTreeNode SaveNode => FirearmSaveData.GetNode(this);

        public virtual void Start()
        {
            all.Add(this);
            _fireSoundsPitch = new float[fireSounds.Length];
            _suppressedFireSoundsPitch = new float[suppressedFireSounds.Length];
            for (var i = 0; i < fireSounds.Length; i++)
            {
                _fireSoundsPitch[i] = fireSounds[i].pitch;
            }
            for (var i = 0; i < suppressedFireSounds.Length; i++)
            {
                _suppressedFireSoundsPitch[i] = suppressedFireSounds[i].pitch;
            }
            muzzleLight = new GameObject("MuzzleFlashLight").AddComponent<Light>();
            muzzleLight.transform.SetParent(transform);
            muzzleLight.enabled = false;
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 5f;
            muzzleLight.intensity = 3f;
            aimTransform = hitscanMuzzle;
        }

        public virtual void InvokedStart()
        {
            SavedAmmoData = SaveNode.GetOrAddValue("SavedAmmoData", new SaveNodeValueMagazineContents(), out var addedNew);

            if (addedNew)
            {
                SavedAmmoData.Value.ItemID = defaultAmmoItem;
                SavedAmmoData.Value.Contents = null;
            }
        }

        public virtual void Update()
        {
            Util.ApplyAudioConfig(fireSounds);
            Util.ApplyAudioConfig(suppressedFireSounds, true);
            
            longPressTime = Settings.longPressTime;
            if (item != null && item.data.moduleAI != null)
            {
                item.data.moduleAI.primaryClass =
                    item.handlers.Count > 0 ? ItemModuleAI.WeaponClass.Firearm : ItemModuleAI.WeaponClass.Melee;
            }
        }

        public virtual List<Handle> AllTriggerHandles()
        {
            return null;
        }

        private Color RandomColor(Cartridge cartridge)
        {
            if (cartridge != null && cartridge.data.overrideMuzzleFlashLightColor)
                return Color.Lerp(cartridge.data.muzzleFlashLightColorOne, cartridge.data.muzzleFlashLightColorTwo, Random.Range(0f, 1f));

            return Color.Lerp(new Color(1.0f, 0.3843f, 0.0f), new Color(1.0f, 0.5294f, 0.0f), Random.Range(0f, 1f));
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
            if ((handle == mainFireHandle && !disableMainFireHandle) || additionalTriggerHandles.Any(x => x == handle))
            {
                OnActionEvent?.Invoke(action);
                
                if (action == Interactable.Action.UseStart && fireMode != FireModes.AttachmentFirearm)
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
                else if (action == Interactable.Action.UseStop && fireMode != FireModes.AttachmentFirearm)
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
                    if (Time.time - lastPressTime >= longPressTime)
                        LongPress();
                    else if (fireMode != FireModes.AttachmentFirearm)
                        ShortPress();
                }
            }
        }

        public void ShortPress()
        {
            OnAltActionEvent?.Invoke(false);
            if (magazineWell != null && (magazineWell.canEject || (!magazineWell.canEject && magazineWell.currentMagazine != null && !magazineWell.currentMagazine.canBeGrabbedInWell && !magazineWell.currentMagazine.overrideItem && !magazineWell.currentMagazine.overrideAttachment)))
            {
                if (bolt.disallowRelease || !bolt.caught || (bolt.caught && (magazineWell.IsEmptyAndHasMagazine() || bolt.isHeld))) magazineWell.Eject();
                else if (bolt.caught && !magazineWell.IsEmpty()) bolt.TryRelease();
            }
            else if (bolt != null && !bolt.disallowRelease) bolt.TryRelease();
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

        public virtual bool IsSuppressed()
        {
            return false;
        }

        public void PlayFireSound(Cartridge cartridge, bool overrideSuppressedBool = false, bool suppressed = false)
        {
            var supp = IsSuppressed();
            var fromCartridgeData = false;
            if (overrideSuppressedBool)
                supp = suppressed;
            if (cartridge != null && cartridge.data.alwaysSuppressed)
                supp = true;
            AudioSource source;
            if (!supp)
            {
                if (cartridge != null && cartridge.data.overrideFireSounds)
                {
                    source = Util.GetRandomFromList(cartridge.data.fireSounds);
                    fromCartridgeData = true;
                }
                else
                    source = Util.GetRandomFromList(fireSounds);
            }
            else
            {
                if (cartridge != null && cartridge.data.overrideFireSounds)
                {
                    source = Util.GetRandomFromList(cartridge.data.suppressedFireSounds);
                    fromCartridgeData = true;
                }
                else
                    source = Util.GetRandomFromList(suppressedFireSounds);
            }

            if (source == null)
                return;
            
            var pitch = 1f;
            if (!supp)
            {
                NoiseManager.AddNoise(actualHitscanMuzzle.position, 600f);
                Util.AlertAllCreaturesInRange(hitscanMuzzle.position, 100);
                if (!fromCartridgeData)
                    pitch = _fireSoundsPitch[fireSounds.ToList().IndexOf(source)];
            }
            else
            {
                if (!fromCartridgeData)
                    pitch = _suppressedFireSoundsPitch[suppressedFireSounds.ToList().IndexOf(source)];
            }
            
            if (fromCartridgeData)
            {
                var sourceInstance = Instantiate(source.gameObject, fireSounds.Any() ? fireSounds.First().transform : transform, true);
                source = sourceInstance.GetComponent<AudioSource>();
                StartCoroutine(Explosive.DelayedDestroy(sourceInstance, source.clip.length + 1f)); 
            }
            
            var deviation =  Settings.firingSoundDeviation / pitch;
            source.pitch = pitch + Random.Range(-deviation, deviation);
            source.Play();
        }

        public virtual void PlayMuzzleFlash(Cartridge cartridge)
        { }

        public IEnumerator PlayMuzzleFlashLight(Cartridge cartridge)
        {
            muzzleLight.color = RandomColor(cartridge);
            muzzleLight.transform.position = actualHitscanMuzzle.position + (actualHitscanMuzzle.forward * 0.04f);
            muzzleLight.enabled = true;
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            muzzleLight.enabled = false;
        }

        public bool NoMuzzleFlashOverridingAttachmentChildren(Attachment attachment)
        {
            foreach (var p in attachment.attachmentPoints)
            {
                if (p.currentAttachments.Any())
                {
                    if (!NmfoaCrecurve(p.currentAttachments))
                        return false;
                }
            }
            return true;
        }

        private bool NmfoaCrecurve(ICollection<Attachment> attachments)
        {
            foreach (var p in attachments.SelectMany(x => x.attachmentPoints))
            {
                if (p.currentAttachments.Any())
                {
                    if (!NmfoaCrecurve(p.currentAttachments))
                        return false;
                }
            }
            if (attachments.Any(x => x.overridesMuzzleFlash))
                return false;
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
            var modifier = new RecoilModifier
                           {
                               Modifier = linearModifier,
                               MuzzleRiseModifier = muzzleRiseModifier,
                               Handler = modifierHandler
                           };
            RemoveRecoilModifier(modifierHandler);
            RecoilModifiers.Add(modifier);
        }

        public void RemoveRecoilModifier(object modifierHandler)
        {
            foreach (var mod in RecoilModifiers.ToArray())
            {
                if (mod.Handler == modifierHandler) RecoilModifiers.Remove(mod);
            }
        }

        public void RefreshRecoilModifiers()
        {
            foreach (var mod in RecoilModifiers.ToArray())
            {
                if (mod.Handler == null) RecoilModifiers.Remove(mod);
            }
        }

        public void InvokeCollisionTR(CollisionInstance collisionInstance) => OnCollisionEventTR?.Invoke(collisionInstance);

        public class RecoilModifier
        {
            public float Modifier;
            public float MuzzleRiseModifier;
            public object Handler;
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
