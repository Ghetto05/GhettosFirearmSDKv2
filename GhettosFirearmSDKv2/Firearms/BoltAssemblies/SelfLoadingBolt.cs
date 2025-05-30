using UnityEngine;

namespace GhettosFirearmSDKv2;

public class SelfLoadingBolt : BoltBase
{
    public Transform roundMount;
    public AudioSource[] ejectSounds;

    public Transform roundEjectPoint;
    public float roundEjectForce;
    public Transform roundEjectDir;
    private int _shotsSinceTriggerReset;
    private float _lastFireTime = -100f;

    public bool ReadyToFire()
    {
        var timePerRound = 60f / firearm.roundsPerMinute;
        var passedTime = Time.time - _lastFireTime;
        return passedTime >= timePerRound;
    }

    public override bool LoadChamber(Cartridge c, bool forced)
    {
        return false;
    }

    private Cartridge _loadedCartridge;

    public override void TryFire()
    {
        if (firearm.magazineWell.IsEmpty())
        {
            InvokeFireLogicFinishedEvent();
            return;
        }
        _shotsSinceTriggerReset++;
        _lastFireTime = Time.time;
        foreach (var hand in firearm.item.handlers)
        {
            if (hand.playerHand?.controlHand is not null)
            {
                hand.playerHand.controlHand.HapticShort(50f);
            }
        }
        _loadedCartridge = firearm.magazineWell.ConsumeRound();
        IncrementBreachSmokeTime();
        firearm.PlayFireSound(_loadedCartridge);
        if (_loadedCartridge.data.playFirearmDefaultMuzzleFlash)
        {
            firearm.PlayMuzzleFlash(_loadedCartridge);
        }
        FireMethods.ApplyRecoil(firearm.transform, firearm.item, _loadedCartridge.data.recoil, _loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
        FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, _loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
        _loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, true);
        EjectRound();
        InvokeFireEvent();
        InvokeFireLogicFinishedEvent();
    }

    public override Cartridge GetChamber()
    {
        return null;
    }

    private void Firearm_OnTriggerChangeEvent(bool isPulled)
    {
        if (fireOnTriggerPress && isPulled && firearm.fireMode != FirearmBase.FireModes.Safe)
        {
            if (firearm.fireMode == FirearmBase.FireModes.Semi && _shotsSinceTriggerReset == 0 && ReadyToFire())
            {
                TryFire();
            }
            else if (firearm.fireMode == FirearmBase.FireModes.Burst && _shotsSinceTriggerReset < firearm.burstSize && ReadyToFire())
            {
                TryFire();
            }
            else if (firearm.fireMode == FirearmBase.FireModes.Auto)
            {
                TryFire();
            }
        }
        if (!isPulled)
        {
            _shotsSinceTriggerReset = 0;
        }
    }

    public void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
        ChamberSaved();
    }

    public override void EjectRound()
    {
        if (!_loadedCartridge)
        {
            return;
        }
        Util.PlayRandomAudioSource(ejectSounds);
        SaveChamber(null, false, false, null);
        if (roundEjectPoint)
        {
            _loadedCartridge.transform.position = roundEjectPoint.position;
            _loadedCartridge.transform.rotation = roundEjectPoint.rotation;
        }
        Util.IgnoreCollision(_loadedCartridge.gameObject, firearm.gameObject, true);
        _loadedCartridge.ToggleCollision(true);
        Util.DelayIgnoreCollision(_loadedCartridge.gameObject, firearm.gameObject, false, 3f, firearm.item);
        var rb = _loadedCartridge.GetComponent<Rigidbody>();
        _loadedCartridge.item.DisallowDespawn = false;
        _loadedCartridge.transform.parent = null;
        rb.isKinematic = false;
        _loadedCartridge.loaded = false;
        rb.WakeUp();
        if (roundEjectDir)
        {
            rb.AddForce(roundEjectDir.forward * roundEjectForce, ForceMode.Impulse);
        }
        _loadedCartridge.ToggleHandles(true);
        if (firearm.magazineWell && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired)
        {
            firearm.magazineWell.Eject();
        }
    }

    private void Update()
    {
        BaseUpdate();
    }
}