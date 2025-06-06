﻿using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MuzzleLoadedBolt : BoltBase
{
    public bool ejectCasingOnReleaseButton = true;
    public Cartridge loadedCartridge;
    public Transform roundMount;
    public AudioSource[] ejectSounds;

    public Transform roundEjectPoint;
    public float roundEjectForce;
    public Transform roundEjectDir;
    public bool ejectOnFire;
    private int _shotsSinceTriggerReset;
    public List<Lock> locks;

    public Hammer hammer;

    public override bool LoadChamber(Cartridge c, bool forced)
    {
        if (!loadedCartridge && !c.loaded)
        {
            loadedCartridge = c;
            c.item.DisallowDespawn = true;
            c.loaded = true;
            c.ToggleHandles(false);
            c.ToggleCollision(false);
            c.UngrabAll();
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.GetComponent<Rigidbody>().isKinematic = true;
            c.transform.parent = roundMount;
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Util.RandomCartridgeRotation();
            SaveChamber(c.item.itemId, c.Fired, c.Failed, c.item.contentCustomData);
            return true;
        }
        return false;
    }

    public override void TryRelease(bool forced = false)
    {
        if (ejectCasingOnReleaseButton)
        {
            EjectRound();
        }
    }

    public override void TryFire()
    {
        _shotsSinceTriggerReset++;
        if (!Util.AllLocksUnlocked(locks))
        {
            InvokeFireLogicFinishedEvent();
            return;
        }
        if (hammer)
        {
            var f = hammer.cocked;
            hammer.Fire();
            if (!f)
            {
                InvokeFireLogicFinishedEvent();
                return;
            }
        }

        if (!loadedCartridge || loadedCartridge.Fired || loadedCartridge.Failed)
        {
            InvokeFireLogicFinishedEvent();
            return;
        }
        foreach (var hand in firearm.item.handlers)
        {
            if (hand.playerHand && hand.playerHand.controlHand is not null)
            {
                hand.playerHand.controlHand.HapticShort(50f);
            }
        }
        IncrementBreachSmokeTime();
        firearm.PlayFireSound(loadedCartridge);
        if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
        {
            firearm.PlayMuzzleFlash(loadedCartridge);
        }
        FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
        FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
        loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, !Settings.infiniteAmmo);
        SaveChamber(loadedCartridge?.item.itemId, loadedCartridge?.Fired, loadedCartridge?.Failed, loadedCartridge?.item.contentCustomData);
        if (ejectOnFire && !Settings.infiniteAmmo)
        {
            EjectRound();
        }
        InvokeFireEvent();
        InvokeFireLogicFinishedEvent();
    }

    public override Cartridge GetChamber()
    {
        return loadedCartridge;
    }

    public override void UpdateChamberedRounds()
    {
        base.UpdateChamberedRounds();
        if (!loadedCartridge)
        {
            return;
        }
        loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
        loadedCartridge.transform.parent = roundMount;
        loadedCartridge.transform.localPosition = Vector3.zero;
        loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
    }

    private void Firearm_OnTriggerChangeEvent(bool isPulled)
    {
        if (fireOnTriggerPress && isPulled && firearm.fireMode != FirearmBase.FireModes.Safe)
        {
            if (firearm.fireMode == FirearmBase.FireModes.Semi && _shotsSinceTriggerReset == 0)
            {
                TryFire();
            }
            else if (firearm.fireMode == FirearmBase.FireModes.Burst && _shotsSinceTriggerReset < firearm.burstSize)
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
        UpdateChamberedRounds();
    }

    public override void EjectRound()
    {
        if (!loadedCartridge)
        {
            return;
        }
        Util.PlayRandomAudioSource(ejectSounds);
        SaveChamber(null, false, false, null);
        var c = loadedCartridge;
        loadedCartridge = null;
        if (roundEjectPoint)
        {
            c.transform.position = roundEjectPoint.position;
            c.transform.rotation = roundEjectPoint.rotation;
        }
        Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
        c.ToggleCollision(true);
        Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
        var rb = c.GetComponent<Rigidbody>();
        c.item.DisallowDespawn = false;
        c.transform.parent = null;
        rb.isKinematic = false;
        c.loaded = false;
        rb.WakeUp();
        if (roundEjectDir)
        {
            rb.AddForce(roundEjectDir.forward * roundEjectForce, ForceMode.Impulse);
        }
        c.ToggleHandles(true);
        InvokeEjectRound(c);
    }

    private void Update()
    {
        BaseUpdate();
    }
}