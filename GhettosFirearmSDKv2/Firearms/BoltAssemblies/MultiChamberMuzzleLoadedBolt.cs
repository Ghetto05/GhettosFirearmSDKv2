using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Bolt assemblies/Muzzle loaded, multi chamber, self cocking")]
public class MultiChamberMuzzleLoadedBolt : BoltBase, IAmmunitionLoadable
{
    public bool ejectOnFire;

    public List<AudioSource> ejectSounds;
    public List<AudioSource> insertSounds;

    public List<string> calibers;
    public List<Transform> muzzles;
    public List<Transform> actualMuzzles;
    public List<ParticleSystem> muzzleFlashes;
    public List<ParticleSystem> actualMuzzleFlashes;
    public List<Hammer> hammers;
    public List<Transform> mountPoints;
    public List<Collider> loadColliders;
    public List<Transform> ejectDirections;
    public List<Transform> ejectPoints;
    private Cartridge[] _loadedCartridges;
    public List<float> ejectForces;

    private int _shotsSinceTriggerReset;
    private bool _allowInsert;

    private MagazineSaveData _data;

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        _loadedCartridges = new Cartridge[mountPoints.Count];
        actualMuzzles = muzzles;
        firearm.OnMuzzleCalculatedEvent += Firearm_OnMuzzleCalculatedEvent;
        firearm.OnCollisionEvent += OnCollisionEvent;
        Initialize();
        if (firearm.item.TryGetCustomData(out _data))
        {
            for (var i = 0; i < _data.Contents.Length; i++)
            {
                if (_data.Contents[i] is not null)
                {
                    var index = i;
                    Util.SpawnItem(_data.Contents[index]?.ItemId, "Bolt Chamber", ci =>
                    {
                        var c = ci.GetComponent<Cartridge>();
                        _data.Contents[index].Apply(c);
                        LoadChamber(index, c, false);
                    }, transform.position + Vector3.up * 3);
                }
            }
            UpdateChamberedRounds();
        }
        else
        {
            firearm.item.AddCustomData(new MagazineSaveData());
            firearm.item.TryGetCustomData(out _data);
            _data.Contents = new CartridgeSaveData[_loadedCartridges.Length];
        }
        _allowInsert = true;
    }

    private void Firearm_OnMuzzleCalculatedEvent()
    {
        if (firearm.actualHitscanMuzzle.TryGetComponent(out BreakActionMuzzleOverride overr))
        {
            actualMuzzles = overr.newMuzzles;
            actualMuzzleFlashes = overr.newMuzzleFlashes;
        }
        else
        {
            actualMuzzles = muzzles;
            actualMuzzleFlashes = muzzleFlashes;
        }
    }

    private void OnCollisionEvent(Collision collision)
    {
        if (!_allowInsert)
        {
            return;
        }
        if (collision.collider.GetComponentInParent<Cartridge>() is { } car && !car.loaded)
        {
            foreach (var insertCollider in loadColliders)
            {
                if (Util.CheckForCollisionWithThisCollider(collision, insertCollider))
                {
                    var index = loadColliders.IndexOf(insertCollider);
                    LoadChamber(index, car);
                }
            }
        }
    }

    public void LoadChamber(int index, Cartridge cartridge, bool overrideSave = true)
    {
        if (!_loadedCartridges[index] && Util.AllowLoadCartridge(cartridge, calibers[index]))
        {
            if (overrideSave)
            {
                Util.PlayRandomAudioSource(insertSounds);
            }
            _loadedCartridges[index] = cartridge;
            cartridge.item.DisallowDespawn = true;
            cartridge.loaded = true;
            cartridge.ToggleHandles(false);
            cartridge.ToggleCollision(false);
            cartridge.UngrabAll();
            Util.IgnoreCollision(cartridge.gameObject, firearm.gameObject, true);
            cartridge.GetComponent<Rigidbody>().isKinematic = true;
            cartridge.transform.parent = mountPoints[index];
            cartridge.transform.localPosition = Vector3.zero;
            cartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
            if (overrideSave)
            {
                SaveCartridges();
            }
        }
        UpdateChamberedRounds();
    }

    public override void TryEject()
    {
        for (var i = 0; i < _loadedCartridges.Length; i++)
        {
            TryEjectSingle(i);
        }
    }

    public void TryEjectSingle(int i)
    {
        if (_loadedCartridges[i])
        {
            Util.PlayRandomAudioSource(ejectSounds);
            var c = _loadedCartridges[i];
            _loadedCartridges[i] = null;
            if (ejectPoints.Count > i && ejectPoints[i])
            {
                c.transform.position = ejectPoints[i].position;
                c.transform.rotation = ejectPoints[i].rotation;
            }
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.ToggleCollision(true);
            Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
            var rb = c.item.physicBody.rigidBody;
            c.item.DisallowDespawn = false;
            c.transform.parent = null;
            c.loaded = false;
            rb.isKinematic = false;
            rb.WakeUp();
            if (ejectDirections[i])
            {
                AddForceToCartridge(c, ejectDirections[i], ejectForces[i]);
                //AddTorqueToCartridge(c);
            }
            c.ToggleHandles(true);
            InvokeEjectRound(c);
            SaveCartridges();
        }
    }

    public void FixedUpdate()
    {
        if (firearm.triggerState)
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
                for (var i = 0; i < mountPoints.Count; i++) { TryFire(); }
            }
        }
        else
        {
            _shotsSinceTriggerReset = 0;
        }
    }

    private void Update()
    {
        BaseUpdate();
    }

    private int GetFirstFireableChamber()
    {
        var car = -1;
        for (var i = _loadedCartridges.Length - 1; i >= 0; i--)
        {
            var hammerCocked = hammers.Count - 1 < i || !hammers[i] || hammers[i].cocked;
            var cartridge = _loadedCartridges[i] && !_loadedCartridges[i].Fired && !_loadedCartridges[i].Failed;
            if (hammerCocked && cartridge)
            {
                car = i;
            }
        }
        return car;
    }

    public override void TryFire()
    {
        if (state != BoltState.Locked)
        {
            InvokeFireLogicFinishedEvent();
            return;
        }
        var ca = GetFirstFireableChamber();
        if (ca == -1)
        {
            InvokeFireLogicFinishedEvent();
            return;
        }

        _shotsSinceTriggerReset++;
        if (hammers.Count > ca && hammers[ca])
        {
            hammers[ca].Fire();
        }
        foreach (var hand in firearm.item.handlers)
        {
            if (hand.playerHand || hand.playerHand.controlHand is not null)
            {
                hand.playerHand.controlHand.HapticShort(50f);
            }
        }
        var muzzle = muzzles.Count < 2 ? firearm.actualHitscanMuzzle : actualMuzzles[ca];
        var loadedCartridge = _loadedCartridges[ca];
        IncrementBreachSmokeTime();
        firearm.PlayFireSound(loadedCartridge);
        if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
        {
            if (actualMuzzleFlashes is not null && actualMuzzleFlashes.Count > ca && actualMuzzleFlashes[ca] && muzzles.Count > 1)
            {
                actualMuzzleFlashes[ca].Play();
            }
            else
            {
                firearm.PlayMuzzleFlash(loadedCartridge);
            }
        }
        FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
        FireMethods.Fire(firearm.item, muzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
        loadedCartridge.Fire(hits, trajectories, muzzle, hitCreatures, killedCreatures, !Settings.infiniteAmmo);
        SaveCartridges();
        InvokeFireEvent();
        InvokeFireLogicFinishedEvent();
    }

    public override void TryRelease(bool forced = false)
    {
        TryEject();
    }

    public override void Initialize()
    { }

    public override void EjectRound()
    {
        if (mountPoints.Count == 1)
        {
            TryEjectSingle(0);
        }
        else if (GetFirstFireableChamber() != -1)
        {
            TryEjectSingle(GetFirstFireableChamber() - 1);
        }
        else if (mountPoints.Count > 0)
        {
            TryEjectSingle(mountPoints.Count - 1);
        }
    }

    public void SaveCartridges()
    {
        _data.Contents = new CartridgeSaveData[_loadedCartridges.Length];
        for (var i = 0; i < _loadedCartridges.Length; i++)
        {
            _data.Contents[i] = new CartridgeSaveData(_loadedCartridges[i]?.item.itemId, _loadedCartridges[i]?.Fired, _loadedCartridges[i]?.Failed, _loadedCartridges[i]?.item.contentCustomData);
        }
    }

    public override void UpdateChamberedRounds()
    {
        base.UpdateChamberedRounds();
        for (var i = 0; i < mountPoints.Count; i++)
        {
            if (_loadedCartridges[i])
            {
                _loadedCartridges[i].GetComponent<Rigidbody>().isKinematic = true;
                _loadedCartridges[i].transform.parent = mountPoints[i];
                _loadedCartridges[i].transform.localPosition = Vector3.zero;
                _loadedCartridges[i].transform.localEulerAngles = Util.RandomCartridgeRotation();
            }
        }
    }

    private int GetFirstFreeChamber()
    {
        var availableChambers = new List<int>();
        for (var i = _loadedCartridges.Length - 1; i >= 0; i--)
        {
            if (!_loadedCartridges[i])
            {
                availableChambers.Add(i);
            }
        }

        if (availableChambers.Count == 0)
        {
            return 0;
        }

        return availableChambers.First();
    }

    public string GetCaliber()
    {
        return calibers[GetFirstFreeChamber()];
    }

    public int GetCapacity()
    {
        return _loadedCartridges.Length;
    }

    public List<Cartridge> GetLoadedCartridges()
    {
        return _loadedCartridges.ToList();
    }

    public void LoadRound(Cartridge cartridge)
    {
        LoadChamber(GetFirstFreeChamber(), cartridge);
    }

    public void ClearRounds()
    {
        foreach (var cartridge in _loadedCartridges)
        {
            EjectRound();
            cartridge.item.Despawn(0.05f);
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public bool GetForceCorrectCaliber()
    {
        return false;
    }

    public List<string> GetAlternativeCalibers()
    {
        return null;
    }
}