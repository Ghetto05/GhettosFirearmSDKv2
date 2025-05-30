using System.Collections;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class Mortar : BoltBase
{
    private Util.InitializationData _initializationData;

    public AudioSource[] cartridgeAttachSounds;
    public AudioSource[] cartridgeDetachSounds;
    public AudioSource[] additionalFireSounds;
    public AudioSource[] cartridgeSlideSounds;
    public AudioSource[] cartridgeDropSounds;
    public string caliber;
    public Collider loadCollider;
    public float dropTime;
    public Transform cartridgeStartPoint;
    public Transform cartridgeEndPoint;
    
    private Cartridge _attachedCartridge;
    private Cartridge _loadedCartridge;
    private int _shotsSinceTriggerRest;

    private void Start()
    {
        StartCoroutine(Util.RequestInitialization(firearm.gameObject, Initialization));
    }

    private void Initialization(Util.InitializationData e)
    {
        _initializationData = e;
        _initializationData.InteractionProvider.OnTeardown += OnTeardown;
        _initializationData.InteractionProvider.OnCollision += OnCollision;
        firearm.OnTriggerChangeEvent += OnTriggerChange;
    }

    private void OnTeardown()
    {
        _initializationData.InteractionProvider.OnTeardown -= OnTeardown;
        _initializationData.InteractionProvider.OnCollision -= OnCollision;
        firearm.OnTriggerChangeEvent -= OnTriggerChange;
    }

    private void OnTriggerChange(bool pulled)
    {
        if (pulled)
        {
            TryFire();
        }
        else
        {
            _shotsSinceTriggerRest = 0;
        }
    }

    private void OnCollision(Collision collision)
    {
        if (Util.CheckForCollisionWithThisCollider(collision, loadCollider) &&
            collision.gameObject.GetComponentInParent<Cartridge>() is { } c && c.caliber.Equals(caliber))
        {
            AttachCartridge(c);
        }
    }

    public void AttachCartridge(Cartridge c)
    {
        if (!c)
        {
            return;
        }

        Util.PlayRandomAudioSource(cartridgeAttachSounds);
        c.item.OnUngrabEvent += AttachedCartridgeOnUnGrab;
        c.item.OnHeldActionEvent += AttachedCartridgeOnHeldAction;
        c.item.physicBody.isKinematic = true;
        c.item.transform.SetParent(cartridgeStartPoint);
        c.item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private void AttachedCartridgeOnHeldAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (action == Interactable.Action.AlternateUseStart)
        {
            DetachCartridge();
        }
    }

    public void DetachCartridge()
    {
        if (!_attachedCartridge)
        {
            return;
        }

        Util.PlayRandomAudioSource(cartridgeDetachSounds);
        _attachedCartridge.item.OnUngrabEvent -= AttachedCartridgeOnUnGrab;
        _attachedCartridge.item.OnHeldActionEvent -= AttachedCartridgeOnHeldAction;
        _attachedCartridge.item.physicBody.isKinematic = false;
        _attachedCartridge.item.transform.SetParent(null);
    }

    private void AttachedCartridgeOnUnGrab(Handle handle, RagdollHand ragdollHand, bool throwing)
    {
        if (!_attachedCartridge.item.handlers.Any())
        {
            StartCoroutine(DropCartridge());
        }
    }

    private IEnumerator DropCartridge()
    {
        var target = 0f;
        var start = Time.time;
        Util.PlayRandomAudioSource(cartridgeSlideSounds);

        while (target < 1)
        {
            target = (Time.time - start) / dropTime;
            _attachedCartridge.transform.localPosition = Vector3.Lerp(cartridgeStartPoint.localPosition,
                cartridgeEndPoint.localPosition, target);
            yield return null;
        }

        Util.PlayRandomAudioSource(cartridgeDropSounds);
        LoadChamber(_attachedCartridge, false);
        _attachedCartridge = null;
        TryFire();
    }

    public override bool LoadChamber(Cartridge c, bool forced)
    {
        if (_loadedCartridge || !c)
        {
            return false;
        }

        _loadedCartridge = c;
        c.item.physicBody.isKinematic = true;
        c.item.transform.SetParent(cartridgeEndPoint);
        c.item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        SaveChamber(c.item.itemId, c.Fired, c.Failed, c.item.contentCustomData);
        return true;
    }

    public override void TryFire()
    {
        if (!((firearm.fireMode != FirearmBase.FireModes.Semi && _shotsSinceTriggerRest < 1) || firearm.fireMode == FirearmBase.FireModes.Auto) || _loadedCartridge?.CanFire != true)
        {
            InvokeFireLogicFinishedEvent();
            return;
        }

        _shotsSinceTriggerRest++;
        
        var failureToFire = Util.DoMalfunction(Settings.malfunctionFailureToFire, Settings.failureToFireChance, firearm.malfunctionChanceMultiplier, firearm.HeldByAI());
        if (failureToFire)
        {
            _loadedCartridge.data.muzzleVelocity = 0.2f;
        }

        firearm.PlayMuzzleFlash(_loadedCartridge);
        firearm.PlayFireSound(_loadedCartridge);
        Util.ApplyAudioConfig(additionalFireSounds);
        Util.PlayRandomAudioSource(additionalFireSounds);
        
        FireMethods.ApplyRecoil(firearm.transform, firearm.item, _loadedCartridge.data.recoil, _loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
        FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, _loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
        _loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, !(firearm.roundsPerMinute > 0 && firearm.HeldByAI()));
        
        InvokeFireEvent();
        InvokeFireLogicFinishedEvent();
        SaveChamber(null, false, false, null);
    }
}