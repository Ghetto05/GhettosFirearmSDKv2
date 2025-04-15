using System;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class PumpAction : BoltBase, IAmmunitionLoadable
{
    public bool actsAsRelay;
    public Rigidbody rb;
    public Transform bolt;
    public Transform startPoint;
    public Transform endPoint;
    public Transform hammerCockPoint;
    public Transform roundEjectPoint;
    public Transform roundLoadPoint;
    public List<AttachmentPoint> onBoltPoints;

    [HideInInspector]
    public List<Handle> boltHandles;

    public AudioSource[] rackSounds;
    public AudioSource[] pullSounds;
    public bool slamFire;
    public float roundEjectForce = 0.6f;
    public Transform roundEjectDir;
    public Transform roundMount;
    public Transform roundReparent;

    [HideInInspector]
    public Cartridge loadedCartridge;

    public Hammer hammer;

    private bool _behindLoadPoint;
    private int _shotsSinceTriggerReset;
    private FixedJoint _lockJoint;
    private FixedJoint _nonHeldLockJoint;
    private ConfigurableJoint _joint;
    private bool _closedSinceLastEject;
    private bool _wentToFrontSinceLastLock;
    private bool _currentRoundRemounted;
    private bool _ready;
    private bool _beforeHammerPoint = true;

    public void Start()
    {
        Util.GetParent(Parent.GameObject, null).GetInitialization(Init);
    }

    public void Init(IAttachmentManager manager, IComponentParent parent)
    {
        firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
        firearm.item.OnGrabEvent += Item_OnGrabEvent;
        firearm.item.OnDespawnEvent += OnDespawn;
        if (firearm is Firearm f)
        {
            f.OnAttachmentAdded += Firearm_OnAttachmentAddedEvent;
            f.OnAttachmentRemoved += Firearm_OnAttachmentRemovedEvent;
        }
        RefreshBoltHandles();
        ChamberSaved();
        if (loadedCartridge)
        {
            Invoke(nameof(DelayedReparent), 0.03f);
        }
        Lock(true);
        _ready = true;
        Invoke(nameof(UpdateChamberedRounds), 1f);
    }

    private void OnDespawn(EventTime eventTime)
    {
        if (eventTime != EventTime.OnStart)
        {
            return;
        }

        firearm.OnTriggerChangeEvent -= Firearm_OnTriggerChangeEvent;
        firearm.item.OnGrabEvent -= Item_OnGrabEvent;
        firearm.item.OnDespawnEvent -= OnDespawn;
        if (firearm is Firearm f)
        {
            f.OnAttachmentAdded -= Firearm_OnAttachmentAddedEvent;
            f.OnAttachmentRemoved -= Firearm_OnAttachmentRemovedEvent;
        }
    }

    private void Firearm_OnAttachmentRemovedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
    {
        RefreshBoltHandles();
    }

    private void Firearm_OnAttachmentAddedEvent(Attachment attachment, AttachmentPoint attachmentPoint)
    {
        RefreshBoltHandles();
    }

    public void RefreshBoltHandles()
    {
        try
        {
            boltHandles = new List<Handle>();

            foreach (var h in rb.gameObject.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
            {
                boltHandles.Add(h);
            }
            foreach (var h in bolt.gameObject.GetComponentsInChildren<Handle>().Where(h => h.item == firearm.item))
            {
                boltHandles.Add(h);
            }
            foreach (var point in onBoltPoints)
            {
                foreach (var attachment in point.GetAllChildAttachments())
                {
                    foreach (var handle in attachment.handles)
                    {
                        if (handle.GetType() == typeof(GhettoHandle))
                        {
                            var hh = (GhettoHandle)handle;
                            hh.type = GhettoHandle.HandleType.PumpAction;
                        }
                        boltHandles.Add(handle);
                    }
                }
            }

            foreach (var h in boltHandles)
            {
                h.customRigidBody = !_lockJoint ? rb : firearm.item.physicBody.rigidBody;
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
    {
        RefreshBoltHandles();
        if (boltHandles.Contains(handle))
        { }
        if (loadedCartridge && roundReparent && _currentRoundRemounted)
        {
            loadedCartridge.transform.SetParent(roundReparent);
        }
    }

    public override void UpdateChamberedRounds()
    {
        base.UpdateChamberedRounds();
        if (!loadedCartridge)
        {
            return;
        }
        loadedCartridge.GetComponent<Rigidbody>().isKinematic = true;
        loadedCartridge.transform.parent = _currentRoundRemounted && roundReparent ? roundReparent : roundMount;
        loadedCartridge.transform.localPosition = Vector3.zero;
        loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
    }

    public void SetStateOnAllHandlers(bool locked)
    {
        foreach (var handle in boltHandles)
        {
            var hands = handle.handlers.ToList();
            //handle.Release();
            handle.customRigidBody = locked ? firearm.item.physicBody.rigidBody : rb;

            foreach (var hand in hands)
            {
                //hand.Grab(handle, false);
                var targetRb = locked ? firearm.item.physicBody.rigidBody : rb;
                hand.gripInfo.joint.connectedAnchor = targetRb.transform.InverseTransformPoint(hand.gripInfo.transform.position);
                hand.gripInfo.joint.connectedBody = targetRb;
            }
        }
    }

    private void Firearm_OnTriggerChangeEvent(bool isPulled)
    {
        if (!isPulled)
        {
            _shotsSinceTriggerReset = 0;
        }
    }

    public override void TryFire()
    {
        if (actsAsRelay || !loadedCartridge || loadedCartridge.Fired || (hammer && !hammer.cocked))
        {
            Lock(false);
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
        _shotsSinceTriggerReset++;
        if (hammer)
        {
            hammer.Fire();
        }
        IncrementBreachSmokeTime();
        firearm.PlayFireSound(loadedCartridge);
        firearm.PlayMuzzleFlash(loadedCartridge);
        FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
        FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
        var fire = false;
        if (!Settings.infiniteAmmo || (Settings.infiniteAmmo && firearm.magazineWell))
        {
            fire = true;
            Lock(false);
        }
        loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, fire);
        SaveChamber(loadedCartridge?.item.itemId, loadedCartridge?.Fired ?? false);
        InvokeFireLogicFinishedEvent();
        InvokeFireEvent();
    }

    public void Lock(bool locked)
    {
        RefreshBoltHandles();
        if (locked)
        {
            Util.DelayedExecute(0.005f, DelayedReparent, this);
            bolt.localPosition = startPoint.localPosition;
            rb.transform.localPosition = startPoint.localPosition;
            if (!_lockJoint)
            {
                _lockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
            }
            _lockJoint.connectedBody = rb;
            _lockJoint.connectedMassScale = 100f;
            _closedSinceLastEject = true;
            _wentToFrontSinceLastLock = false;
            SetStateOnAllHandlers(true);
            Destroy(_joint);
        }
        else if (_lockJoint)
        {
            InitializeJoint();
            Destroy(_lockJoint);
            SetStateOnAllHandlers(false);
        }
    }

    private bool BoltHandleHeld()
    {
        foreach (var h in boltHandles)
        {
            if (h.IsHanded())
            {
                return true;
            }
        }

        return false;
    }

    private void FixedUpdate()
    {
        if (!_ready)
        {
            return;
        }

        //UpdateChamberedRound();
        isHeld = BoltHandleHeld();

        //state check
        if (isHeld)
        {
            if (_nonHeldLockJoint)
            {
                Destroy(_nonHeldLockJoint);
            }

            if (!_lockJoint)
            {
                bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, rb.transform.localPosition.z);
            }
            if (Util.AbsDist(bolt.position, startPoint.position) < Settings.boltPointThreshold && state == BoltState.Moving)
            {
                laststate = BoltState.Moving;
                state = BoltState.Locked;
                if (loadedCartridge && roundReparent)
                {
                    _currentRoundRemounted = true;
                    loadedCartridge.transform.SetParent(roundReparent);
                    loadedCartridge.transform.localPosition = Vector3.zero;
                    loadedCartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
                if (_wentToFrontSinceLastLock)
                {
                    Lock(true);
                }
                Util.PlayRandomAudioSource(rackSounds);
            }
            else if (Util.AbsDist(bolt.position, endPoint.position) < Settings.boltPointThreshold && state == BoltState.Moving)
            {
                laststate = BoltState.Moving;
                state = BoltState.Back;
                Util.PlayRandomAudioSource(pullSounds);
                _wentToFrontSinceLastLock = true;
                if (_closedSinceLastEject)
                {
                    EjectRound();
                }
                _closedSinceLastEject = false;
            }
            else if (state != BoltState.Moving && Util.AbsDist(bolt.position, endPoint.position) > Settings.boltPointThreshold && Util.AbsDist(bolt.position, startPoint.position) > Settings.boltPointThreshold)
            {
                laststate = state;
                state = BoltState.Moving;
            }
            //loading
            if (state == BoltState.Moving && (laststate == BoltState.Back || laststate == BoltState.LockedBack))
            {
                if (roundLoadPoint && _behindLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                {
                    if (!loadedCartridge && !actsAsRelay)
                    {
                        TryLoadRound();
                    }
                    _behindLoadPoint = false;
                }
                else if (roundLoadPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(roundLoadPoint.localPosition, startPoint.localPosition))
                {
                    _behindLoadPoint = true;
                }
            }
            //hammer
            if (state == BoltState.Moving && laststate == BoltState.Locked && !actsAsRelay)
            {
                if (hammer && !hammer.cocked && _beforeHammerPoint && Util.AbsDist(startPoint.localPosition, bolt.localPosition) > Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                {
                    hammer.Cock();
                }
                else if (hammer && Util.AbsDist(startPoint.localPosition, bolt.localPosition) < Util.AbsDist(hammerCockPoint.localPosition, startPoint.localPosition))
                {
                    _beforeHammerPoint = true;
                }
            }
        }
        else
        {
            if (!_nonHeldLockJoint)
            {
                _nonHeldLockJoint = firearm.item.gameObject.AddComponent<FixedJoint>();
                _nonHeldLockJoint.connectedBody = rb;
                _nonHeldLockJoint.connectedMassScale = 100f;
            }
        }

        //firing
        if (state == BoltState.Locked && firearm.triggerState && fireOnTriggerPress && firearm.fireMode != FirearmBase.FireModes.Safe)
        {
            if (firearm.fireMode == FirearmBase.FireModes.Semi && ((slamFire && !Settings.infiniteAmmo) || _shotsSinceTriggerReset == 0 || actsAsRelay))
            {
                TryFire();
            }
        }

        CalculatePercentage();
    }

    private void Update()
    {
        BaseUpdate();
    }

    public override void EjectRound()
    {
        if (actsAsRelay || !loadedCartridge)
        {
            return;
        }
        SaveChamber(null, false);
        _currentRoundRemounted = false;
        var c = loadedCartridge;
        loadedCartridge = null;
        if (roundEjectPoint)
        {
            c.transform.position = roundEjectPoint.position;
            c.transform.rotation = roundEjectPoint.rotation;
        }
        Util.IgnoreCollision(c.gameObject, firearm.item.gameObject, true);
        c.ToggleCollision(true);
        Util.DelayIgnoreCollision(c.gameObject, firearm.item.gameObject, false, 3f, firearm.item);
        var crb = c.item.physicBody.rigidBody;
        c.item.DisallowDespawn = false;
        c.transform.parent = null;
        c.loaded = false;
        crb.isKinematic = false;
        crb.WakeUp();
        if (roundEjectDir)
        {
            AddTorqueToCartridge(c);
            AddForceToCartridge(c, roundEjectDir, roundEjectForce);
        }
        c.ToggleHandles(true);
        if (firearm.magazineWell && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired)
        {
            firearm.magazineWell.Eject();
        }
        InvokeEjectRound(c);
    }

    public override void TryLoadRound()
    {
        if (!actsAsRelay && !loadedCartridge && firearm.magazineWell && firearm.magazineWell.ConsumeRound() is { } c)
        {
            loadedCartridge = c;
            c.GetComponent<Rigidbody>().isKinematic = true;
            c.transform.SetParent(roundMount);
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Util.RandomCartridgeRotation();
            SaveChamber(c.item.itemId, c.Fired);
        }
    }

    private void InitializeJoint()
    {
        _joint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
        _joint.connectedBody = rb;
        //pJoint.massScale = 0.00001f;
        _joint.connectedMassScale = 100f;
        var limit = new SoftJointLimit();
        _joint.anchor = new Vector3(GrandparentLocalPosition(endPoint, firearm.item.transform).x, GrandparentLocalPosition(endPoint, firearm.item.transform).y, GrandparentLocalPosition(endPoint, firearm.item.transform).z + (startPoint.localPosition.z - endPoint.localPosition.z) / 2);
        limit.limit = Vector3.Distance(endPoint.position, startPoint.position) / 2;
        _joint.linearLimit = limit;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Vector3.zero;
        _joint.xMotion = ConfigurableJointMotion.Locked;
        _joint.yMotion = ConfigurableJointMotion.Locked;
        _joint.zMotion = ConfigurableJointMotion.Limited;
        _joint.angularXMotion = ConfigurableJointMotion.Locked;
        _joint.angularYMotion = ConfigurableJointMotion.Locked;
        _joint.angularZMotion = ConfigurableJointMotion.Locked;
        rb.transform.localPosition = startPoint.localPosition;
        rb.transform.localRotation = startPoint.localRotation;
    }

    public override bool LoadChamber(Cartridge c, bool forced)
    {
        if (!loadedCartridge && (state != BoltState.Locked || forced) && !c.loaded)
        {
            loadedCartridge = c;
            c.item.DisallowDespawn = true;
            c.loaded = true;
            c.ToggleHandles(false);
            c.ToggleCollision(false);
            c.UngrabAll();
            Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
            c.GetComponent<Rigidbody>().isKinematic = true;
            c.transform.SetParent(roundMount);
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Util.RandomCartridgeRotation();
            SaveChamber(c.item.itemId, c.Fired);
            return true;
        }
        return false;
    }

    public void DelayedReparent()
    {
        if (!loadedCartridge)
        {
            return;
        }
        loadedCartridge.transform.SetParent(roundReparent ? roundReparent : roundMount);
        _currentRoundRemounted = true;
    }

    public override void TryRelease(bool forced = false)
    {
        if (_lockJoint)
        {
            Lock(false);
        }
    }

    public void CalculatePercentage()
    {
        var distanceStartBolt = Util.AbsDist(bolt, startPoint);
        var totalDistance = Util.AbsDist(startPoint, endPoint);
        cyclePercentage = Mathf.Clamp01(distanceStartBolt / totalDistance);
    }

    public override Cartridge GetChamber()
    {
        return loadedCartridge;
    }

    public string GetCaliber()
    {
        return firearm.magazineWell?.caliber;
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
        LoadChamber(cartridge, true);
    }

    public void ClearRounds()
    {
        SaveChamber(null, false);
        if (!loadedCartridge)
        {
            return;
        }
        loadedCartridge.item.Despawn();
        loadedCartridge = null;
    }

    public bool GetForceCorrectCaliber()
    {
        return false;
    }

    public List<string> GetAlternativeCalibers()
    {
        return firearm.magazineWell?.alternateCalibers;
    }
}