﻿using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class BreakAction : BoltBase, IAmmunitionLoadable
{
    public bool ejector = true;

    public Axes foldAxis;
    public float minFoldAngle;
    public float maxFoldAngle;

    public List<AudioSource> lockSounds;
    public List<AudioSource> unlockSounds;

    public List<AudioSource> ejectSounds;
    public List<AudioSource> insertSounds;

    public Rigidbody rb;
    public Transform barrel;
    public Transform closedPosition;
    public Transform openedPosition;

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

    private HingeJoint _joint;
    private bool _ejectedSinceOpen;
    private int _shotsSinceTriggerReset;
    private bool _allowInsert;

    public Transform lockAxis;
    public Transform lockLockedPosition;
    public Transform lockUnlockedPosition;

    public Transform ejectorAxis;
    public Transform ejectorClosedPosition;
    public Transform ejectorOpenedPosition;
    public float ejectorMoveStartPercentage = 0.8f;

    public FiremodeSelector chamberSetSelector;
    public List<string> editorChamberSets;
    public Dictionary<int, List<int>> ChamberSets = new();
    public List<Trigger> triggers;
    public List<string> targetHandPoses;

    // ReSharper disable once CollectionNeverUpdated.Local - assigned in Unity
    private readonly List<HandPoseData> _targetHandPoseData = new();
    public List<string> defaultAmmoItems;

    private SaveNodeValueArray<CartridgeSaveData> _data;
    private SaveNodeValueArray<ItemSaveData> _savedItems;

    private int _currentChamber;
    private int _currentChamberSet;

    public enum Axes
    {
        X,
        Y,
        Z
    }

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
        firearm.actualHitscanMuzzle = muzzles.First();

        if (editorChamberSets.Any())
        {
            var i = 0;
            foreach (var s in editorChamberSets)
            {
                ChamberSets.Add(i, new List<int>());
                ChamberSets[i].AddRange(s.Split(',').Select(cs => int.Parse(cs)));
                i++;
            }
        }
        else
        {
            ChamberSets.Add(0, new List<int>());
            for (var i = 0; i < mountPoints.Count; i++)
            {
                ChamberSets[0].Add(i);
            }
        }
    }

    public void InvokedStart()
    {
        _loadedCartridges = new Cartridge[mountPoints.Count];
        actualMuzzles = muzzles;
        state = BoltState.Locked;
        rb.gameObject.AddComponent<CollisionRelay>().OnCollisionEnterEvent += OnCollisionEvent;
        firearm.OnMuzzleCalculatedEvent += Firearm_OnMuzzleCalculatedEvent;
        firearm.SavedAmmoItemChangedEvent += FirearmOnSavedAmmoItemChangedEvent;

        _savedItems = firearm.SaveNode.GetOrAddValue("DoubleBarrelSavedItem", new SaveNodeValueArray<ItemSaveData>(), out var addedNewSaves);
        if (addedNewSaves)
        {
            _savedItems.Value = defaultAmmoItems.Select(x => string.IsNullOrWhiteSpace(x) ? null : new ItemSaveData { ItemID = x }).ToArray();
        }

        if (ChamberSets.Any())
        {
            if (chamberSetSelector)
            {
                chamberSetSelector.OnFiremodeChanged += ChamberSetSelectorOnFireModeChanged;
            }
            SetChamberSet(0);

            if (triggers.Any() && triggers.Count > _currentChamberSet)
            {
                foreach (var t in triggers)
                {
                    t.triggerEnabled = false;
                }
                triggers[_currentChamberSet].triggerEnabled = true;
            }
        }

        Initialize();
        _data = firearm.SaveNode.GetOrAddValue("DoubleBarrelSave", new SaveNodeValueArray<CartridgeSaveData>(), out var addedNew);
        if (!addedNew)
        {
            for (var i = 0; i < _data.Value.Length; i++)
            {
                if (_data.Value[i] is not null)
                {
                    var index = i;
                    Util.SpawnItem(_data.Value[index]?.ItemId, "Bolt Chamber", ci =>
                    {
                        var c = ci.GetComponent<Cartridge>();
                        _data.Value[index].Apply(c);
                        LoadChamber(index, c, false);
                    }, transform.position + Vector3.up * 3);
                }
            }
            UpdateChamberedRounds();
        }
        else
        {
            _data.Value = new CartridgeSaveData[_loadedCartridges.Length];
        }
        _allowInsert = true;
        UpdateChamberedRounds();
    }

    private void FirearmOnSavedAmmoItemChangedEvent()
    {
        _savedItems.Value[_currentChamberSet] = firearm.GetAmmoItem(true);
    }

    private void ChamberSetSelectorOnFireModeChanged(FirearmBase.FireModes newMode)
    {
        if (newMode != FirearmBase.FireModes.Safe)
        {
            _currentChamberSet = chamberSetSelector.currentIndex;
            SetChamberSet(_currentChamberSet);
        }
    }

    private void SetChamberSet(int set)
    {
        _currentChamber = ChamberSets[set].First();

        foreach (var t in triggers)
        {
            t.triggerEnabled = false;
        }

        if (triggers.Count > set)
        {
            triggers[set].triggerEnabled = true;
        }

        if (_targetHandPoseData.Any())
        {
            foreach (var h in firearm.AllTriggerHandles().Where(h => h).SelectMany(h => h.orientations))
            {
                h.targetHandPoseData = _targetHandPoseData[set];
            }
        }

        if (_savedItems.Value.Length > set)
        {
            firearm.SetOverideAmmoItem(_savedItems.Value[set], this);
        }
    }

    private void Firearm_OnMuzzleCalculatedEvent()
    {
        if (firearm.actualHitscanMuzzle.TryGetComponent(out BreakActionMuzzleOverride over))
        {
            actualMuzzles = over.newMuzzles;
            actualMuzzleFlashes = over.newMuzzleFlashes;
            firearm.actualHitscanMuzzle = over.newMuzzles.First();
        }
        else
        {
            actualMuzzles = muzzles;
            actualMuzzleFlashes = muzzleFlashes;
            firearm.actualHitscanMuzzle = muzzles.First();
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

    public void TryEjectSingle(int i, bool ignoreFiredState = false, bool silent = false)
    {
        if (_loadedCartridges[i])
        {
            var c = _loadedCartridges[i];
            if (Settings.breakActionsEjectOnlyFired && !(c.Fired || c.Failed) && !ignoreFiredState)
            {
                return;
            }
            if (ChamberSets.Any() && !ChamberSets[_currentChamberSet].Contains(i))
            {
                return;
            }
            if (!silent)
            {
                Util.PlayRandomAudioSource(ejectSounds);
            }
            _loadedCartridges[i] = null;
            if (ejectPoints.Count > i && ejectPoints[i])
            {
                c.transform.position = ejectPoints[i].position;
                c.transform.rotation = ejectPoints[i].rotation;
            }
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.ToggleCollision(true);
            Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
            var itemRb = c.item.physicBody.rigidBody;
            c.item.DisallowDespawn = false;
            c.transform.parent = null;
            c.loaded = false;
            itemRb.isKinematic = false;
            itemRb.WakeUp();
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
        if (state == BoltState.Locked)
        {
            barrel.SetParent(closedPosition.parent);
            barrel.localPosition = closedPosition.localPosition;
            barrel.localEulerAngles = closedPosition.localEulerAngles;
        }
        else
        {
            barrel.SetParent(rb.transform);
            barrel.localPosition = Vector3.zero;
            barrel.localEulerAngles = Vector3.zero;
        }

        if (state != BoltState.Locked)
        {
            if (CompareEulers(rb.transform, openedPosition) && !_ejectedSinceOpen)
            {
                if (ejector)
                {
                    EjectRound();
                }
                else
                {
                    _ejectedSinceOpen = true;
                }
            }
            if (CompareEulers(rb.transform, closedPosition) && _ejectedSinceOpen)
            {
                Lock();
            }

            if (!ejector)
            {
                for (var i = 0; i < ejectDirections.Count; i++)
                {
                    if (CheckEjectionGravity(ejectDirections[i]))
                    {
                        TryEjectSingle(i);
                    }
                }
            }
        }

        CalculateCyclePercentage();
        UpdateEjector();

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

    private bool CheckEjectionGravity(Transform t)
    {
        var angle = Vector3.Angle(t.forward, Vector3.down);
        return angle < 50f;
    }

    private bool CompareEulers(Transform ta, Transform tb)
    {
        float a, b;
        if (foldAxis == Axes.X)
        {
            a = ta.localEulerAngles.x;
            b = tb.localEulerAngles.x;
        }
        else if (foldAxis == Axes.Y)
        {
            a = ta.localEulerAngles.y;
            b = tb.localEulerAngles.y;
        }
        else
        {
            a = ta.localEulerAngles.z;
            b = tb.localEulerAngles.z;
        }
        var angle = Mathf.Abs(a - b);
        return angle <= 2f;
    }

    public override void TryFire()
    {
        if (state == BoltState.Locked)
        {
            var hammerCocked = hammers.Count - 1 < _currentChamber || !hammers[_currentChamber] || hammers[_currentChamber].cocked;
            var cartridge = _loadedCartridges[_currentChamber] && !_loadedCartridges[_currentChamber].Fired && !_loadedCartridges[_currentChamber].Failed;

            _shotsSinceTriggerReset++;
            if (hammers.Count > _currentChamber && hammers[_currentChamber])
            {
                hammers[_currentChamber].Fire();
            }

            if (hammerCocked && cartridge)
            {
                foreach (var hand in firearm.item.handlers)
                {
                    if (hand.playerHand || hand.playerHand.controlHand is not null)
                    {
                        hand.playerHand.controlHand.HapticShort(50f);
                    }
                }
                var muzzle = muzzles.Count < 2 ? firearm.actualHitscanMuzzle : actualMuzzles[_currentChamber];
                var loadedCartridge = _loadedCartridges[_currentChamber];
                IncrementBreachSmokeTime();
                firearm.PlayFireSound(loadedCartridge);
                if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                {
                    if (actualMuzzleFlashes is not null && actualMuzzleFlashes.Count > _currentChamber && actualMuzzleFlashes[_currentChamber] && muzzles.Count > 1)
                    {
                        actualMuzzleFlashes[_currentChamber].Play();
                    }
                    else
                    {
                        firearm.PlayMuzzleFlash(loadedCartridge);
                    }
                }
                FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
                FireMethods.Fire(firearm.item, muzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
                loadedCartridge.Fire(hits, trajectories, muzzle, hitCreatures, killedCreatures, !Settings.infiniteAmmo);
                InvokeFireEvent();
                SaveCartridges();
            }
        }

        if (!ChamberSets.Any())
        {
            _currentChamber++;
            if (_currentChamber >= mountPoints.Count)
            {
                _currentChamber = 0;
            }
        }
        else
        {
            _currentChamber++;
            if (_currentChamber >= ChamberSets[_currentChamberSet].Count)
            {
                _currentChamber = ChamberSets[_currentChamberSet].First();
            }
        }

        InvokeFireLogicFinishedEvent();
    }

    public void UpdateEjector()
    {
        if (state == BoltState.Locked || !ejectorAxis || cyclePercentage < ejectorMoveStartPercentage)
        {
            if (ejectorAxis)
            {
                ejectorAxis.SetPositionAndRotation(ejectorClosedPosition.position, ejectorClosedPosition.rotation);
            }
            return;
        }

        var actualPercentage = (cyclePercentage - ejectorMoveStartPercentage) / (1 - ejectorMoveStartPercentage);
        ejectorAxis.position = Vector3.Lerp(ejectorClosedPosition.position, ejectorOpenedPosition.position, actualPercentage);
        ejectorAxis.rotation = Quaternion.Lerp(ejectorClosedPosition.rotation, ejectorOpenedPosition.rotation, actualPercentage);
    }

    public override void TryRelease(bool forced = false)
    {
        if (state == BoltState.Locked)
        {
            Unlock();
        }
        else if (Settings.breakActionsEjectOnlyFired)
        {
            for (var i = 0; i < _loadedCartridges.Length; i++)
            {
                TryEjectSingle(i, true);
            }
        }
    }

    public override void Initialize()
    {
        state = BoltState.Locked;
        InitializeJoint(true);
    }

    public void Lock()
    {
        state = BoltState.Locked;
        if (lockAxis)
        {
            lockAxis.localPosition = lockLockedPosition.localPosition;
            lockAxis.localEulerAngles = lockLockedPosition.localEulerAngles;
        }
        Util.PlayRandomAudioSource(lockSounds);
        foreach (var h in barrel.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
        {
            h.SetTouch(true);
        }
        InitializeJoint(true);
    }

    public void Unlock()
    {
        if (!ChamberSets.Any())
        {
            _currentChamber = 0;
        }
        else
        {
            _currentChamber = ChamberSets[_currentChamberSet].First();
        }
        state = BoltState.Moving;
        if (lockAxis)
        {
            lockAxis.localPosition = lockUnlockedPosition.localPosition;
            lockAxis.localEulerAngles = lockUnlockedPosition.localEulerAngles;
        }
        _ejectedSinceOpen = false;
        Util.PlayRandomAudioSource(unlockSounds);
        foreach (var h in barrel.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
        {
            foreach (var hand in h.handlers.ToArray())
            {
                hand.UnGrab(false);
            }
            h.SetTouch(false);
        }
        InitializeJoint(false);
    }

    public override void EjectRound()
    {
        _ejectedSinceOpen = true;
        TryEject();
    }

    private void InitializeJoint(bool closed)
    {
        if (closed)
        {
            rb.transform.localEulerAngles = closedPosition.localEulerAngles;
            barrel.SetParent(rb.transform);
            barrel.localPosition = Vector3.zero;
            barrel.localEulerAngles = Vector3.zero;
        }
        else
        {
            barrel.SetParent(closedPosition.parent);
            barrel.localPosition = closedPosition.localPosition;
            barrel.localEulerAngles = closedPosition.localEulerAngles;
        }

        if (!_joint)
        {
            _joint = firearm.item.gameObject.AddComponent<HingeJoint>();
            _joint.connectedBody = rb;
            _joint.massScale = 0.00001f;
        }
        _joint.autoConfigureConnectedAnchor = false;
        _joint.anchor = GrandparentLocalPosition(closedPosition.transform, firearm.item.transform);
        _joint.connectedAnchor = Vector3.zero;
        _joint.axis = foldAxis == Axes.X ? Vector3.right : foldAxis == Axes.Y ? Vector3.up : Vector3.forward;
        _joint.useLimits = true;
        _joint.limits = closed ? new JointLimits { min = 0f, max = 0f } : new JointLimits { min = minFoldAngle, max = maxFoldAngle };
    }

    public void SaveCartridges()
    {
        _data.Value = new CartridgeSaveData[_loadedCartridges.Length];
        for (var i = 0; i < _loadedCartridges.Length; i++)
        {
            _data.Value[i] = new CartridgeSaveData(_loadedCartridges[i]?.item.itemId, _loadedCartridges[i]?.Fired, _loadedCartridges[i]?.Failed, _loadedCartridges[i]?.item.contentCustomData);
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

    public void CalculateCyclePercentage()
    {
        var angle = Quaternion.Angle(rb.transform.rotation, closedPosition.rotation);
        var targetAngle = Quaternion.Angle(openedPosition.rotation, closedPosition.rotation);

        cyclePercentage = Mathf.Clamp01(angle / targetAngle);
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

        availableChambers = availableChambers.Where(x => ChamberSets[_currentChamberSet].Contains(x)).ToList();

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
        return calibers.Count(x => x.Equals(GetCaliber()));
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
        foreach (var car in _loadedCartridges)
        {
            if (car)
            {
                car.item.Despawn(0.05f);
            }
        }

        for (var i = 0; i < _loadedCartridges.Length; i++)
        {
            TryEjectSingle(i, true, true);
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