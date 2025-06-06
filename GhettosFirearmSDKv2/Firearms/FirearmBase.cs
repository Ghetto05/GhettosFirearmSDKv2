﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2;

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
    public MagazineLoad overrideMagazineLoad;

    // ReSharper disable once InconsistentNaming
    public SaveNodeValueItem SavedAmmoItemData;
    public float malfunctionChanceMultiplier = 1f;
    public Dictionary<MonoBehaviour, ItemSaveData> AmmoOverrides = new();

    public FirearmSaveData.AttachmentTreeNode SaveNode
    {
        get
        {
            return FirearmSaveData.GetNode(this);
        }
    }

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
        SavedAmmoItemData = SaveNode.GetOrAddValue("SavedAmmoItemData", new SaveNodeValueItem(), out var addedNew);

        if (addedNew)
        {
            SetSavedAmmoItem(defaultAmmoItem, overrideMagazineLoad ? new ContentCustomData[] { overrideMagazineLoad.ToSaveData() } : null);
        }

        ImprovedLazyPouch.InvokeAmmoItemChanged(this);
    }

    public virtual void Update()
    {
        longPressTime = Settings.longPressTime;
    }

    public virtual List<Handle> AllTriggerHandles()
    {
        return null;
    }

    private Color RandomColor(Cartridge cartridge)
    {
        if (cartridge && cartridge.data.overrideMuzzleFlashLightColor)
        {
            return Color.Lerp(cartridge.data.muzzleFlashLightColorOne, cartridge.data.muzzleFlashLightColorTwo, Random.Range(0f, 1f));
        }

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
        aimTransform = hitscanMuzzle;
        OnMuzzleCalculatedEvent?.Invoke();
    }

    public void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        OnHeldActionEvent(ragdollHand, handle, action, out _);
    }

    public void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action, out bool handled)
    {
        if ((handle == mainFireHandle && !disableMainFireHandle) || additionalTriggerHandles.Any(x => x == handle))
        {
            handled = true;
            OnActionEvent?.Invoke(action);

            if (CanFire && action == Interactable.Action.UseStart && fireMode != FireModes.AttachmentFirearm)
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
                {
                    LongPress();
                }
                else if (fireMode != FireModes.AttachmentFirearm)
                {
                    ShortPress();
                }
            }
        }
        else
        {
            handled = false;
        }
    }

    public void ShortPress()
    {
        OnAltActionEvent?.Invoke(false);
        if (magazineWell && (magazineWell.canEject || (!magazineWell.canEject && magazineWell.currentMagazine && !magazineWell.currentMagazine.CanGrab && !magazineWell.currentMagazine.overrideItem && !magazineWell.currentMagazine.overrideAttachment)))
        {
            if (bolt.disallowRelease || !bolt.caught || (bolt.caught && (magazineWell.IsEmptyAndHasMagazine() || bolt.isHeld)))
            {
                magazineWell.Eject();
            }
            else if (bolt.caught && !magazineWell.IsEmpty())
            {
                bolt.TryRelease();
            }
        }
        else if (bolt && !bolt.disallowRelease)
        {
            bolt.TryRelease();
        }
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
        {
            supp = suppressed;
        }
        if (cartridge && cartridge.data.alwaysSuppressed)
        {
            supp = true;
        }
        AudioSource source;
        if (!supp)
        {
            if (cartridge && cartridge.data.overrideFireSounds)
            {
                source = Util.GetRandomFromList(cartridge.data.fireSounds);
                fromCartridgeData = true;
            }
            else
            {
                Util.ApplyAudioConfig(fireSounds);
                source = Util.GetRandomFromList(fireSounds);
            }
        }
        else
        {
            if (cartridge && cartridge.data.overrideFireSounds)
            {
                source = Util.GetRandomFromList(cartridge.data.suppressedFireSounds);
                fromCartridgeData = true;
            }
            else
            {
                Util.ApplyAudioConfig(suppressedFireSounds, true);
                source = Util.GetRandomFromList(suppressedFireSounds);
            }
        }

        if (!source)
        {
            return;
        }

        var pitch = 1f;
        if (!supp)
        {
            NoiseManager.AddNoise(actualHitscanMuzzle.position, 600f);
            Util.AlertAllCreaturesInRange(hitscanMuzzle.position, 100);
            if (!fromCartridgeData)
            {
                pitch = _fireSoundsPitch[fireSounds.ToList().IndexOf(source)];
            }
        }
        else
        {
            if (!fromCartridgeData)
            {
                pitch = _suppressedFireSoundsPitch[suppressedFireSounds.ToList().IndexOf(source)];
            }
        }

        if (fromCartridgeData)
        {
            var sourceInstance = Instantiate(source.gameObject, fireSounds.Any() ? fireSounds.First().transform : transform, true);
            source = sourceInstance.GetComponent<AudioSource>();
            StartCoroutine(Explosive.DelayedDestroy(sourceInstance, source.clip.length + 1f));
        }

        var deviation = Settings.firingSoundDeviation / pitch;
        source.pitch = pitch + Random.Range(-deviation, deviation);
        //source.Play();
        source.PlayOneShot(source.clip, Settings.endableFireSoundVolume ? Settings.fireSoundVolume : 1f);
    }

    public virtual void PlayMuzzleFlash(Cartridge cartridge)
    { }

    public IEnumerator PlayMuzzleFlashLight(Cartridge cartridge)
    {
        muzzleLight.color = RandomColor(cartridge);
        muzzleLight.transform.position = actualHitscanMuzzle.position + actualHitscanMuzzle.forward * 0.04f;
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
                {
                    return false;
                }
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
                {
                    return false;
                }
            }
        }
        if (attachments.Any(x => x.overridesMuzzleFlash && !x.attachmentPoint.dummyMuzzleSlot))
        {
            return false;
        }
        return true;
    }

    public virtual void OnCollisionEnter(Collision collision)
    {
        OnCollisionEvent?.Invoke(collision);
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
            if (mod.Handler == modifierHandler)
            {
                RecoilModifiers.Remove(mod);
            }
        }
    }

    public void RefreshRecoilModifiers()
    {
        foreach (var mod in RecoilModifiers.ToArray())
        {
            if (mod.Handler is null)
            {
                RecoilModifiers.Remove(mod);
            }
        }
    }

    public void InvokeCollisionTR(CollisionInstance collisionInstance)
    {
        OnCollisionEventTR?.Invoke(collisionInstance);
    }

    public void SetSavedAmmoItem(string id, ContentCustomData[] data)
    {
        SetSavedAmmoItem(new ItemSaveData { ItemID = id, CustomData = data });
    }

    public void SetSavedAmmoItem(ItemSaveData data)
    {
        SavedAmmoItemData.Value = data.CloneJson();
        SavedAmmoItemChangedEvent?.Invoke();
        ImprovedLazyPouch.InvokeAmmoItemChanged(this);
    }

    public void SetOverideAmmoItem(ItemSaveData data, MonoBehaviour handler)
    {
        AmmoOverrides[handler] = data;
        ImprovedLazyPouch.InvokeAmmoItemChanged(this);
    }

    public void RemoveOverideAmmoItem(MonoBehaviour handler)
    {
        AmmoOverrides.Remove(handler);
        ImprovedLazyPouch.InvokeAmmoItemChanged(this);
    }

    public ItemSaveData GetAmmoItem(bool ignoreOverrides = false)
    {
        var value = AmmoOverrides.Where(x => x.Key && x.Value is not null)
                                 .Select(e => (KeyValuePair<MonoBehaviour, ItemSaveData>?)e)
                                 .FirstOrDefault();

        if (value is not null && !ignoreOverrides)
        {
            return value.Value.Value;
        }

        return SavedAmmoItemData?.Value;
    }

    public virtual bool HeldByAI()
    {
        return false;
    }

    public virtual bool CanFire
    {
        get
        {
            return true;
        }
    }

    public class RecoilModifier
    {
        public float Modifier;
        public float MuzzleRiseModifier;
        public object Handler;
    }

    //EVENTS
    public delegate void OnTriggerChange(bool isPulled);

    public event OnTriggerChange OnTriggerChangeEvent;
    public event IInteractionProvider.Collision OnCollisionEvent;

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

    public delegate void OnMuzzleCalculated();

    public event OnMuzzleCalculated OnMuzzleCalculatedEvent;

    public delegate void SavedAmmoItemChanged();

    public event SavedAmmoItemChanged SavedAmmoItemChangedEvent;
}