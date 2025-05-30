using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class FlintLock : BoltBase, IAmmunitionLoadable
{
    [Header("Firing")]
    public float fireDelay;

    public ParticleSystem panEffect;
    public PowderReceiver mainReceiver;
    public float baseRecoil = 20;

    [Header("Hammer")]
    public Transform hammer;

    public Transform hammerIdlePosition;
    public Transform hammerCockedPosition;
    private bool _hammerState;

    [Header("Pan")]
    public Transform pan;

    public Transform panOpenedPosition;
    public Transform panClosedPosition;
    public PowderReceiver panReceiver;
    private bool _panClosed;

    [Header("Round loading")]
    public string caliber;

    public Collider roundInsertCollider;
    public Transform roundMountPoint;
    public Cartridge loadedCartridge;
    private float _lastRoundPosition;
    public Transform roundEjectDir;
    public Transform roundEjectPoint;
    public float roundEjectForce;

    [Header("Ram rod")]
    public Transform rodFrontEnd;

    public Transform rodRearEnd;
    public string ramRodItem;
    private Item _currentRamRod;
    public Collider ramRodInsertCollider;
    private ConfigurableJoint _joint;
    private bool _rodAwayFromBreach;

    [Header("Ram rod store")]
    public Transform rodStoreFrontEnd;

    public Transform rodStoreRearEnd;
    private Item _currentStoredRamRod;
    public Collider ramRodStoreInsertCollider;
    private ConfigurableJoint _storeJoint;
    private bool _rodAwayFromStoreEnd;

    [Header("Audio")]
    public AudioSource[] sizzleSound;

    [Space]
    public AudioSource[] hammerCockSounds;

    public AudioSource[] hammerFireSounds;

    [Space]
    public AudioSource[] panOpenSounds;

    public AudioSource[] panCloseSounds;

    [Space]
    public AudioSource[] ramRodInsertSound;

    public AudioSource[] ramRodExtractSound;

    [Space]
    public AudioSource[] ramRodStoreInsertSound;

    public AudioSource[] ramRodStoreExtractSound;

    [Space]
    public AudioSource[] roundInsertSounds;

    private ProjectileData _emptyFireData;
    private bool _ramRodLocked;
    private bool _ramRodStoreLocked;

    private SaveNodeValueInt _panFillLevelSaveData;
    private SaveNodeValueInt _barrelFillLevelSaveData;
    private SaveNodeValueBool _rodStoreSaveData;
    private SaveNodeValueBool _hammerStateSaveData;
    private SaveNodeValueBool _panStateSaveData;

    private void Start()
    {
        GenerateFireData();
        OpenPan(true);
        Invoke(nameof(InvokedStart), Settings.invokeTime * 2);
    }

    private void GenerateFireData()
    {
        _emptyFireData = gameObject.AddComponent<ProjectileData>();
        _emptyFireData.recoil = 10;
        _emptyFireData.forceDestabilize = false;
        _emptyFireData.forceIncapitate = false;
        _emptyFireData.isHitscan = true;
        _emptyFireData.lethalHeadshot = false;
        _emptyFireData.penetrationPower = ProjectileData.PenetrationLevels.None;
        _emptyFireData.projectileCount = 30;
        _emptyFireData.projectileRange = 1;
        _emptyFireData.projectileSpread = 25;
        _emptyFireData.damagePerProjectile = 0.3f;
        _emptyFireData.hasBodyImpactEffect = false;
        _emptyFireData.hasImpactEffect = false;
        _emptyFireData.forcePerProjectile = 0f;
        _emptyFireData.drawsImpactDecal = false;
    }

    private void InvokedStart()
    {
        firearm.OnCollisionEvent += FirearmOnOnCollisionEvent;
        firearm.OnCockActionEvent += FirearmOnOnCockActionEvent;
        firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;
        firearm.OnAltActionEvent += FirearmOnOnAltActionEvent;

        var node = FirearmSaveData.GetNode(firearm);
        _panFillLevelSaveData = node.GetOrAddValue("FlintLock_PanPowderFillLevel", new SaveNodeValueInt());
        _barrelFillLevelSaveData = node.GetOrAddValue("FlintLock_BarrelPowderFillLevel", new SaveNodeValueInt());
        var newRodData = new SaveNodeValueBool();
        newRodData.Value = true;
        _rodStoreSaveData = node.GetOrAddValue("FlintLock_RodStored", newRodData);
        _hammerStateSaveData = node.GetOrAddValue("FlintLock_HammerCockState", new SaveNodeValueBool());
        _panStateSaveData = node.GetOrAddValue("FlintLock_PanOpenState", new SaveNodeValueBool());

        if (_hammerStateSaveData.Value)
        {
            CockHammer(true);
        }
        if (_panStateSaveData.Value)
        {
            ClosePan(true);
        }
        mainReceiver.currentAmount = _barrelFillLevelSaveData.Value;
        panReceiver.currentAmount = _panFillLevelSaveData.Value;
        mainReceiver.UpdatePositions();

        if (_rodStoreSaveData.Value && !string.IsNullOrWhiteSpace(ramRodItem))
        {
            Util.SpawnItem(ramRodItem, "Flint lock rod", rod =>
            {
                InitializeRamRodJoint(rod, true);
                _currentStoredRamRod = rod;
                _ramRodStoreLocked = true;
                _storeJoint.anchor = GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform);
                _storeJoint.zMotion = ConfigurableJointMotion.Locked;
                _storeJoint.angularZMotion = ConfigurableJointMotion.Locked;
            }, rodStoreRearEnd.position, rodStoreRearEnd.rotation);
        }
        ChamberSaved();
    }

    private void FirearmOnOnCockActionEvent()
    {
        CockHammer();
    }

    private void FirearmOnOnAltActionEvent(bool longpress)
    {
        if (!longpress)
        {
            if (_panClosed)
            {
                OpenPan();
            }
            else
            {
                ClosePan();
            }
        }
    }

    private void FirearmOnOnTriggerChangeEvent(bool ispulled)
    {
        if (ispulled)
        {
            TryFire();
        }
    }

    private void FirearmOnOnCollisionEvent(Collision collision)
    {
        if (collision.rigidbody && collision.rigidbody.TryGetComponent(out Item hitItem))
        {
            if (!_currentRamRod && (!_currentStoredRamRod || hitItem != _currentStoredRamRod) && hitItem.itemId.Equals(ramRodItem) && Util.CheckForCollisionWithThisCollider(collision, ramRodInsertCollider))
            {
                InitializeRamRodJoint(hitItem);
                _currentRamRod = hitItem;
                _currentRamRod.DisallowDespawn = true;
                _rodAwayFromBreach = false;
                Util.PlayRandomAudioSource(ramRodInsertSound);
            }
            if (!_currentStoredRamRod && (!_currentRamRod || hitItem != _currentRamRod) && hitItem.itemId.Equals(ramRodItem) && Util.CheckForCollisionWithThisCollider(collision, ramRodStoreInsertCollider))
            {
                InitializeRamRodJoint(hitItem, true);
                _currentStoredRamRod = hitItem;
                _currentStoredRamRod.DisallowDespawn = true;
                _rodAwayFromStoreEnd = false;
                Util.PlayRandomAudioSource(ramRodStoreInsertSound);
            }
            else if (hitItem.TryGetComponent(out Cartridge c) && Util.CheckForCollisionWithThisCollider(collision, roundInsertCollider))
            {
                _nextLoadIsMuzzle = true;
                LoadChamber(c, false);
            }
        }
    }

    public override void TryFire()
    {
        if (!_hammerState)
        {
            InvokeFireLogicFinishedEvent();
            return;
        }

        Util.PlayRandomAudioSource(hammerFireSounds);
        hammer.SetPositionAndRotation(hammerIdlePosition.position, hammerIdlePosition.rotation);
        _hammerState = false;
        _hammerStateSaveData.Value = false;

        if (!_panClosed)
        {
            InvokeFireLogicFinishedEvent();
            return;
        }

        OpenPan();

        if (!panReceiver.Sufficient())
        {
            InvokeFireLogicFinishedEvent();
            return;
        }

        if (!Settings.infiniteAmmo)
        {
            panReceiver.currentAmount = 0;
        }
        Util.PlayRandomAudioSource(sizzleSound);
        if (panEffect)
        {
            panEffect.Play();
        }

        Invoke(nameof(DelayedFire), fireDelay);

        base.TryFire();
    }

    public void DelayedFire()
    {
        if (!mainReceiver.Sufficient())
        {
            EjectRound();
            InvokeFireLogicFinishedEvent();
            return;
        }

        if (!Settings.infiniteAmmo)
        {
            mainReceiver.currentAmount = 0;
        }
        if (loadedCartridge)
        {
            if (Vector3.Distance(loadedCartridge.transform.position, rodRearEnd.position) < Settings.boltPointThreshold)
            {
                firearm.PlayFireSound(loadedCartridge);
                if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                {
                    firearm.PlayMuzzleFlash(loadedCartridge);
                }
                FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hitPoints, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
                FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
                loadedCartridge.Fire(hitPoints, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, !firearm.HeldByAI() && !Settings.infiniteAmmo);
                SaveChamber(loadedCartridge?.item.itemId, loadedCartridge?.Fired, loadedCartridge?.Failed, loadedCartridge?.item.contentCustomData);
            }
        }
        else
        {
            firearm.PlayFireSound(null);
            FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, _emptyFireData, out _, out _, out _, out _, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
            FireMethods.ApplyRecoil(firearm.transform, firearm.item, baseRecoil, 1, firearm.recoilModifier, firearm.RecoilModifiers);
            firearm.PlayMuzzleFlash(null);
        }
        IncrementBreachSmokeTime();

        if (_currentRamRod)
        {
            InitializeRamRodJoint(null);
            Util.DisableCollision(_currentRamRod, false);
            _currentRamRod.DisallowDespawn = false;
            _currentRamRod = null;
            Util.PlayRandomAudioSource(ramRodExtractSound);
        }
        EjectRound();
    }

    public void CockHammer(bool forced = false)
    {
        if (_hammerState)
        {
            return;
        }
        if (!forced)
        {
            Util.PlayRandomAudioSource(hammerCockSounds);
        }
        hammer.SetPositionAndRotation(hammerCockedPosition.position, hammerCockedPosition.rotation);
        _hammerState = true;
        _hammerStateSaveData.Value = true;
    }

    public void OpenPan(bool forced = false)
    {
        if (!_panClosed && !forced)
        {
            return;
        }
        if (!forced)
        {
            Util.PlayRandomAudioSource(panOpenSounds);
        }
        pan.SetPositionAndRotation(panOpenedPosition.position, panOpenedPosition.rotation);
        _panClosed = false;
        if (!forced)
        {
            _panStateSaveData.Value = false;
        }
    }

    public void ClosePan(bool forced = false)
    {
        if (_panClosed || !_hammerState)
        {
            return;
        }
        if (!forced)
        {
            Util.PlayRandomAudioSource(panCloseSounds);
        }
        pan.SetPositionAndRotation(panClosedPosition.position, panClosedPosition.rotation);
        _panClosed = true;
        _panStateSaveData.Value = true;
    }

    private void FixedUpdate()
    {
        panReceiver.blocked = _panClosed || !_hammerState;
        mainReceiver.blocked = loadedCartridge || _currentRamRod;

        if (_barrelFillLevelSaveData is not null)
        {
            _barrelFillLevelSaveData.Value = mainReceiver.currentAmount;
        }
        if (_panFillLevelSaveData is not null)
        {
            _panFillLevelSaveData.Value = panReceiver.currentAmount;
        }

        if (_currentRamRod && loadedCartridge)
        {
            var currentPos = Vector3.Distance(rodFrontEnd.position, _currentRamRod.transform.position);
            var targetPos = Vector3.Distance(rodFrontEnd.position, rodRearEnd.position);
            var posTime = currentPos / targetPos;
            if (posTime > _lastRoundPosition)
            {
                _lastRoundPosition = posTime;
            }
            loadedCartridge.transform.position = Vector3.LerpUnclamped(rodFrontEnd.position, rodRearEnd.position, _lastRoundPosition);
        }

        #region Ram rod movement

        if (_currentRamRod && !_rodAwayFromBreach &&
            Vector3.Distance(_currentRamRod.transform.position, rodRearEnd.position) < Settings.boltPointThreshold)
        {
            _rodAwayFromBreach = true;
        }

        if (_currentRamRod && _rodAwayFromBreach &&
            Vector3.Distance(_currentRamRod.transform.position, rodFrontEnd.position) < Settings.boltPointThreshold)
        {
            InitializeRamRodJoint(null);
            Util.DisableCollision(_currentRamRod, false);
            _currentRamRod.DisallowDespawn = false;
            _currentRamRod = null;
            Util.PlayRandomAudioSource(ramRodExtractSound);
        }

        if (_currentRamRod && _currentRamRod.handlers.Count == 0 && !_ramRodLocked)
        {
            _ramRodLocked = true;
            _joint.anchor = new Vector3(GrandparentLocalPosition(rodRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).y, GrandparentLocalPosition(_currentRamRod.transform, firearm.item.transform).z);
            _joint.zMotion = ConfigurableJointMotion.Locked;
        }
        else if (_currentRamRod && _currentRamRod.handlers.Count > 0 && _ramRodLocked)
        {
            _ramRodLocked = false;
            _joint.anchor = new Vector3(GrandparentLocalPosition(rodRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).y, GrandparentLocalPosition(rodRearEnd, firearm.item.transform).z + (rodFrontEnd.localPosition.z - rodRearEnd.localPosition.z) / 2);
            _joint.zMotion = ConfigurableJointMotion.Limited;
        }

        #endregion

        #region Ram rod store movement

        if (_currentStoredRamRod && !_rodAwayFromStoreEnd &&
            Vector3.Distance(_currentStoredRamRod.transform.position, rodStoreRearEnd.position) < Settings.boltPointThreshold)
        {
            _rodAwayFromStoreEnd = true;
        }

        if (_currentStoredRamRod && _rodAwayFromStoreEnd &&
            Vector3.Distance(_currentStoredRamRod.transform.position, rodStoreFrontEnd.position) < Settings.boltPointThreshold)
        {
            InitializeRamRodJoint(null, true);
            Util.DisableCollision(_currentStoredRamRod, false);
            _currentStoredRamRod.DisallowDespawn = false;
            _currentStoredRamRod = null;
            Util.PlayRandomAudioSource(ramRodStoreExtractSound);
        }

        if (_currentStoredRamRod && _currentStoredRamRod.handlers.Count == 0 && !_ramRodStoreLocked)
        {
            _ramRodStoreLocked = true;
            _storeJoint.anchor = new Vector3(GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).y, GrandparentLocalPosition(_currentStoredRamRod.transform, firearm.item.transform).z);
            _storeJoint.zMotion = ConfigurableJointMotion.Locked;
            _storeJoint.angularZMotion = ConfigurableJointMotion.Locked;
        }
        else if (_currentStoredRamRod && _currentStoredRamRod.handlers.Count > 0 && _ramRodStoreLocked)
        {
            _ramRodStoreLocked = false;
            _storeJoint.anchor = new Vector3(GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).x, GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).y, GrandparentLocalPosition(rodStoreRearEnd, firearm.item.transform).z + (rodStoreFrontEnd.localPosition.z - rodStoreRearEnd.localPosition.z) / 2);
            _storeJoint.zMotion = ConfigurableJointMotion.Limited;
            _storeJoint.angularZMotion = ConfigurableJointMotion.Free;
        }

        #endregion
    }

    private void Update()
    {
        BaseUpdate();
    }

    private void InitializeRamRodJoint(Item item, bool store = false)
    {
        var j = store ? _storeJoint : _joint;
        var frontEnd = store ? rodStoreFrontEnd : rodFrontEnd;
        var rearEnd = store ? rodStoreRearEnd : rodRearEnd;

        if (j)
        {
            Destroy(j);
        }
        if (!item)
        {
            if (store)
            {
                _rodStoreSaveData.Value = false;
            }
            return;
        }

        if (store)
        {
            _rodStoreSaveData.Value = true;
        }
        var oldHandlers = item.handlers.ToArray();
        foreach (var handle in item.handles)
        {
            handle.Release();
        }
        j = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
        j.massScale = 0.00001f;
        j.linearLimit = new SoftJointLimit
                        {
                            limit = Vector3.Distance(frontEnd.position, rearEnd.position) / 2
                        };
        j.autoConfigureConnectedAnchor = false;
        j.connectedAnchor = Vector3.zero;
        j.anchor = new Vector3(GrandparentLocalPosition(rearEnd, firearm.item.transform).x, GrandparentLocalPosition(rearEnd, firearm.item.transform).y, GrandparentLocalPosition(rearEnd, firearm.item.transform).z + (frontEnd.localPosition.z - rearEnd.localPosition.z) / 2);
        j.xMotion = ConfigurableJointMotion.Locked;
        j.yMotion = ConfigurableJointMotion.Locked;
        j.zMotion = ConfigurableJointMotion.Limited;
        j.angularXMotion = ConfigurableJointMotion.Locked;
        j.angularYMotion = ConfigurableJointMotion.Locked;
        if (!store)
        {
            j.angularZMotion = ConfigurableJointMotion.Free;
        }
        else
        {
            j.angularZMotion = ConfigurableJointMotion.Locked;
        }
        item.transform.position = frontEnd.position;
        item.transform.eulerAngles = new Vector3(frontEnd.eulerAngles.x, frontEnd.eulerAngles.y, item.transform.localEulerAngles.z);
        j.connectedBody = item.physicBody.rigidBody;
        foreach (var handler in oldHandlers)
        {
            handler.Grab(item.GetMainHandle(handler.side));
        }
        Util.DisableCollision(item, true);

        if (store)
        {
            _storeJoint = j;
        }
        else
        {
            _joint = j;
        }
    }

    public override Cartridge GetChamber()
    {
        return loadedCartridge;
    }

    private void SetPositionToPowder()
    {
        if (loadedCartridge)
        {
            loadedCartridge.transform.position = Vector3.LerpUnclamped(rodFrontEnd.position, rodRearEnd.position, mainReceiver.currentAmount / (float)mainReceiver.grainCapacity);
        }
    }

    private bool _nextLoadIsMuzzle;

    public override bool LoadChamber(Cartridge c, bool forced)
    {
        if (!loadedCartridge && (Util.AllowLoadCartridge(c, caliber) || forced))
        {
            if (!forced)
            {
                Util.PlayRandomAudioSource(roundInsertSounds);
            }
            _lastRoundPosition = 0f;
            loadedCartridge = c;
            c.item.DisallowDespawn = true;
            c.loaded = true;
            c.ToggleHandles(false);
            c.ToggleCollision(false);
            c.UngrabAll();
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.item.physicBody.isKinematic = true;
            c.transform.parent = rodFrontEnd;
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Util.RandomCartridgeRotation();
            SaveChamber(c.item.itemId, c.Fired, c.Failed, c.item.contentCustomData);
            Invoke(nameof(Rechamber), 1f);
            if (!_nextLoadIsMuzzle)
            {
                Invoke(nameof(SetPositionToPowder), 1.2f);
                _nextLoadIsMuzzle = false;
            }

            return true;
        }
        return false;
    }

    private void Rechamber()
    {
        if (loadedCartridge)
        {
            loadedCartridge.transform.parent = rodFrontEnd;
            loadedCartridge.transform.localPosition = Vector3.zero;
            loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
        }
    }

    public override void EjectRound()
    {
        if (!loadedCartridge)
        {
            return;
        }
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
        var rb = c.item.physicBody.rigidBody;
        c.item.DisallowDespawn = false;
        c.transform.parent = null;
        c.loaded = false;
        rb.isKinematic = false;
        rb.WakeUp();
        if (roundEjectDir)
        {
            AddForceToCartridge(c, roundEjectDir, roundEjectForce);
            AddTorqueToCartridge(c);
        }
        c.ToggleHandles(true);
        InvokeEjectRound(c);
    }

    public string GetCaliber()
    {
        return caliber;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public int GetCapacity()
    {
        return 1;
    }

    public List<Cartridge> GetLoadedCartridges()
    {
        return loadedCartridge ? [loadedCartridge] : [];
    }

    public void LoadRound(Cartridge cartridge)
    {
        LoadChamber(cartridge, false);
    }

    public void ClearRounds()
    {
        if (!loadedCartridge)
        {
            return;
        }
        SaveChamber(null, false, false, null);
        loadedCartridge.item.Despawn();
        loadedCartridge = null;
    }

    public bool GetForceCorrectCaliber()
    {
        return false;
    }

    public List<string> GetAlternativeCalibers()
    {
        return [];
    }
}