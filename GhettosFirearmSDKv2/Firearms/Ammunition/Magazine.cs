using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class Magazine : MonoBehaviour, IAmmunitionLoadable
{
    public static List<Magazine> all = new();

    public bool ejectOnLastRoundFired;
    public bool infinite;
    public string magazineType;
    public string caliber;
    public List<string> alternateCalibers;
    public bool forceCorrectCaliber;
    public List<Cartridge> cartridges;
    public int maximumCapacity;
    public bool canEjectRounds;
    public bool destroyOnEject;
    public Collider roundInsertCollider;
    public AudioSource[] roundInsertSounds;
    public Transform roundEjectPoint;
    public AudioSource[] roundEjectSounds;
    public AudioSource[] magazineEjectSounds;
    public AudioSource[] magazineInsertSounds;
    public Collider mountCollider;
    public Transform overrideMountPoint;
    public bool canBeGrabbedInWell;
    public List<Handle> handles;
    public MagazineWell currentWell;
    public Transform nullCartridgePosition;
    public Transform[] cartridgePositions;
    public Transform[] oddCountCartridgePositions;
    public Item item;
    public MagazineLoad defaultLoad;
    public bool hasOverrideLoad;
    public Item overrideItem;
    public Attachment overrideAttachment;
    public List<Collider> colliders;
    private List<Renderer> _originalRenderers;
    private MagazineSaveData _saveData;
    private SaveNodeValueMagazineContents _firearmSave;
    public List<GameObject> feederObjects;
    public bool loadable;
    public bool partOfPrebuilt;
    public float lastEjectTime;
    public BoltBase bolt;
    public bool onlyAllowLoadWhenBoltIsBack;
    public List<MagazinePositionSet> positionSets;
    private List<ColliderGroup> _colliderGroups = new();
    public bool addHandlesToParentMagazine;
    public string overrideMagazineAttachmentType;

    public int ActualCapacity
    {
        get
        {
            return cartridges.Any() ? positionSets.FirstOrDefault(x => x.caliber.Equals(cartridges[0].caliber))?.capacity ?? maximumCapacity : maximumCapacity;
        }
    }

    private void Update()
    {
        if (currentWell && currentWell.actualFirearm && CanGrab)
        {
            foreach (var handle in handles)
            {
                handle.SetTouch(!currentWell.actualFirearm.item.holder);
                handle.SetTelekinesis(!currentWell);
            }
        }

        foreach (var obj in feederObjects)
        {
            obj.SetActive(false);
        }
        if (feederObjects.Count > cartridges.Count && feederObjects[cartridges.Count])
        {
            feederObjects[cartridges.Count].SetActive(true);
        }
    }

    public void InvokeLoadFinished()
    {
        OnLoadFinished?.Invoke(this);
    }

    private void Start()
    {
        if (overrideAttachment)
        {
            Util.GetParent(overrideAttachment.gameObject, null).GetInitialization((_ , _) => Init());
        }
        else if (currentWell?.mountCurrentMagazine == true)
        {
            Util.GetParent(currentWell.firearm, null).GetInitialization((_ , _) => Init());
        }
        else
        {
            item.OnSpawnEvent += OnItemSpawn;
        }
    }

    private void OnItemSpawn(EventTime eventTime)
    {
        if (eventTime != EventTime.OnEnd) return;
        Init();
        item.OnSpawnEvent -= OnItemSpawn;
    }

    public void Init()
    {
        cartridges = [];
        if (!overrideItem)
        {
            item = GetComponent<Item>();
        }
        else if (overrideItem)
        {
            item = overrideItem;
        }
        if (!item && !overrideAttachment)
        {
            return;
        }
        if (!overrideItem && !overrideAttachment)
        {
            item.SetPhysicBodyAndMainCollisionHandler();
        }
        if (!overrideAttachment)
        {
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnDespawnEvent += Item_OnDespawnEvent;
            item.OnSetColliderLayerEvent += ItemOnOnSetColliderLayerEvent;
            item.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;
        }

        OnLoadFinished += OnOnLoadFinished;
        foreach (var handle in handles)
        {
            handle.Grabbed += HandleOnGrabbed;
        }
        if (overrideItem)
        {
            overrideItem.GetComponent<FirearmBase>().OnCollisionEvent += OnCollisionEnter;
            if (overrideItem.TryGetComponent(out Firearm f))
            {
                _firearmSave = f.SaveData.FirearmNode.GetOrAddValue("MagazineSaveData", new SaveNodeValueMagazineContents(), out var addedNew);
                if (addedNew && defaultLoad)
                {
                    defaultLoad.Load(this);
                    return;
                }
                _firearmSave.Value.ApplyToMagazine(this);
            }
            else
            {
                InvokeLoadFinished();
            }
        }
        else if (overrideAttachment)
        {
            overrideAttachment.attachmentPoint.ConnectedManager.OnCollision += OnCollisionEnter;
            _firearmSave = overrideAttachment.Node.GetOrAddValue("MagazineSaveData", new SaveNodeValueMagazineContents(), out var addedNew);
            if (addedNew && defaultLoad)
            {
                defaultLoad.Load(this);
                return;
            }
            _firearmSave.Value.ApplyToMagazine(this);

            item = GetComponentInParent<Item>();
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnDespawnEvent += Item_OnDespawnEvent;
            item.OnSetColliderLayerEvent += ItemOnOnSetColliderLayerEvent;
            item.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;
        }
        else
        {
            if (item.TryGetCustomData(out _saveData))
            {
                _saveData.ApplyToMagazine(this);
            }
            else
            {
                _saveData = new MagazineSaveData();
                item.AddCustomData(_saveData);
                if (defaultLoad)
                {
                    defaultLoad.Load(this);
                }
                else
                {
                    InvokeLoadFinished();
                    loadable = true;
                }
            }
        }

        var renderersToBeAdded = new List<MeshRenderer>();
        foreach (var feederObject in feederObjects)
        {
            var renderers = feederObject.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Any())
            {
                renderersToBeAdded.AddRange(renderers);
            }
        }
        if (renderersToBeAdded.Any())
        {
            item.renderers.AddRange(renderersToBeAdded.Where(x => !item.renderers.Contains(x)));
            item.lightVolumeReceiver.SetRenderers(item.renderers);
        }

        if (!overrideItem && !overrideAttachment)
        {
            all.Add(this);
        }
    }

    private void ItemOnOnSetColliderLayerEvent(Item item1, int layer)
    {
        foreach (var handle in handles)
        {
            handle.touchCollider.gameObject.layer = LayerMask.NameToLayer("TouchObject");
        }
    }

    private void HandleOnGrabbed(RagdollHand ragdollhand, Handle handle, EventTime eventTime)
    {
        if (CanGrab && eventTime == EventTime.OnStart)
        {
            Eject();
        }
    }

    private void OnOnLoadFinished(Magazine mag)
    {
        OnLoadFinished -= OnOnLoadFinished;

        if (item.isHidden)
        {
            cartridges.ForEach(c => c.item.Hide(true));
        }
    }

    private void Item_OnUnSnapEvent(Holder holder)
    {
        foreach (var car in cartridges)
        {
            car.DisableCull();
        }
    }

    private void Item_OnDespawnEvent(EventTime eventTime)
    {
        if (eventTime == EventTime.OnEnd)
        {
            return;
        }

        foreach (var c in cartridges)
        {
            if (c)
            {
                c.item.Despawn();
            }
        }

        item.OnUnSnapEvent -= Item_OnUnSnapEvent;
        item.OnHeldActionEvent -= Item_OnHeldActionEvent;
        item.OnDespawnEvent -= Item_OnDespawnEvent;
        item.lightVolumeReceiver.onVolumeChangeEvent -= UpdateAllLightVolumeReceivers;
        item.OnSetColliderLayerEvent -= ItemOnOnSetColliderLayerEvent;
        OnLoadFinished -= OnOnLoadFinished;
        foreach (var handle in handles)
        {
            handle.Grabbed -= HandleOnGrabbed;
        }
        all.Remove(this);
    }

    private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (action == Interactable.Action.AlternateUseStart && canEjectRounds)
        {
            EjectRound();
        }
    }

    public Cartridge EjectRound()
    {
        Cartridge c = null;
        if (cartridges.Count > 0)
        {
            lastEjectTime = Time.time;
            Util.PlayRandomAudioSource(roundEjectSounds);
            c = cartridges[0];
            c.ToggleCollision(true);
            c.ToggleHandles(true);
            cartridges.RemoveAt(0);
            Util.DelayIgnoreCollision(gameObject, c.gameObject, false, 1f, item);
            c.loaded = false;
            c.transform.position = roundEjectPoint.position;
            c.transform.rotation = roundEjectPoint.rotation;
            c.GetComponent<Rigidbody>().isKinematic = false;
            c.item.DisallowDespawn = false;
            c.transform.parent = null;
        }
        UpdateCartridgePositions();
        SaveCustomData();
        return c;
    }

    private bool BoltExistsAndIsPulled()
    {
        return !onlyAllowLoadWhenBoltIsBack || !bolt || bolt.state == BoltBase.BoltState.Back || bolt.state == BoltBase.BoltState.LockedBack;
    }

    public void InsertRound(Cartridge c, bool silent, bool forced, bool save = true, bool atBottom = false)
    {
        if (!partOfPrebuilt && cartridges.Count < ActualCapacity && !cartridges.Contains(c) && (Util.AllowLoadCartridge(c, this) || forced) && ((!c.loaded && BoltExistsAndIsPulled()) || forced))
        {
            c.item.DisallowDespawn = true;
            c.loaded = true;
            c.ToggleHandles(false);
            c.ToggleCollision(false);
            if (!atBottom)
            {
                cartridges.Insert(0, c);
            }
            else
            {
                cartridges.Add(c);
            }
            c.UngrabAll();
            Util.IgnoreCollision(c.gameObject, gameObject, true);
            if (!silent)
            {
                Util.PlayRandomAudioSource(roundInsertSounds);
            }
            c.GetComponent<Rigidbody>().isKinematic = true;
            c.transform.parent = nullCartridgePosition;
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Util.RandomCartridgeRotation();
        }
        UpdateCartridgePositions();
        if (save)
        {
            SaveCustomData();
        }
    }

    public Cartridge ConsumeRound()
    {
        Cartridge c = null;
        if (cartridges.Count > 0 && !Util.DoMalfunction(Settings.malfunctionFailureToFeed, Settings.failureToFeedChance, 1, currentWell?.actualFirearm.HeldByAI() ?? false))
        {
            c = cartridges[0];
            OnConsumeEvent?.Invoke(c);
            Util.IgnoreCollision(c.gameObject, gameObject, false);
            cartridges.RemoveAt(0);
            if (infinite || Settings.infiniteAmmo)
            {
                Util.SpawnItem(c.item.itemId, "[Loaded round in magazine]", car =>
                {
                    var newC = car.GetComponent<Cartridge>();
                    InsertRound(newC, true, true, true, true);
                }, transform.position + Vector3.up * 10, null, null, false);
            }
        }
        UpdateCartridgePositions();
        SaveCustomData();
        return c;
    }

    public IEnumerator DelayedMount(MagazineWell well, Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);

        Mount(well, rb);
    }

    public void Mount(MagazineWell well, Rigidbody rb, bool silent = false)
    {
        if (!overrideItem && !overrideAttachment)
        {
            item.DisallowDespawn = true;
        }

        #region Fix dungeon lighting

        if (!overrideItem && !overrideAttachment)
        {
            if (_originalRenderers is null)
            {
                _originalRenderers = item.renderers.ToList();
            }
            foreach (var ren in _originalRenderers)
            {
                well.actualFirearm.item.renderers.Add(ren);
                item.renderers.Remove(ren);
            }
            well.actualFirearm.item.lightVolumeReceiver.SetRenderers(well.actualFirearm.item.renderers);
            item.lightVolumeReceiver.SetRenderers(item.renderers);
        }

        #endregion

        currentWell = well;
        currentWell.currentMagazine = this;

        if (!overrideAttachment && !overrideItem)
        {
            var hands = item.handlers.Where(h => handles.Contains(h.grabbedHandle)).ToArray();
            foreach (var hand in hands)
            {
                hand.UnGrab(false);
            }
        }

        foreach (var c in cartridges)
        {
            Util.IgnoreCollision(c.gameObject, currentWell.firearm.gameObject, true);
        }
        if (!silent)
        {
            Util.PlayRandomAudioSource(magazineInsertSounds);
        }
        Util.IgnoreCollision(gameObject, currentWell.firearm.gameObject, true);

        #region Parent to firearm

        if (!overrideItem && !overrideAttachment)
        {
            item.physicBody.isKinematic = true;
        }

        transform.SetParent(well.mountPoint);
        transform.position = well.mountPoint.position;
        transform.rotation = well.mountPoint.rotation;

        #endregion

        #region Collider fix

        if (!overrideAttachment && !overrideItem)
        {
            _colliderGroups = item.colliderGroups.ToList();
            foreach (var group in _colliderGroups)
            {
                group.transform.SetParent(currentWell.mountPoint);
            }
            item.colliderGroups.RemoveAll(x => _colliderGroups.Contains(x));
            currentWell.actualFirearm.item.colliderGroups.AddRange(_colliderGroups);
            currentWell.actualFirearm.item.RefreshCollision();
        }

        #endregion

        foreach (var handle in handles)
        {
            if (!CanGrab)
            {
                handle.SetTouch(false);
            }

            handle.SetTelekinesis(false);
        }

        // save mag to firearm
        if (!overrideItem && !overrideAttachment)
        {
            _firearmSave = FirearmSaveData.GetNode(currentWell.actualFirearm).GetOrAddValue(currentWell.SaveID, new SaveNodeValueMagazineContents());
            _firearmSave.Value.GetContentsFromMagazine(this);
            _firearmSave.Value.ItemID = item.itemId;
        }

        partOfPrebuilt = false;

        UpdateCartridgePositions();
        OnInsertEvent?.Invoke(currentWell);
        Invoke(nameof(ResetRagdollCollision), 0.2f);
    }

    private void ResetRagdollCollision()
    {
        if (currentWell)
        {
            currentWell.actualFirearm.item.RefreshCollision();
        }
    }

    public void Eject()
    {
        if (currentWell)
        {
            var lastWell = currentWell;
            OnEjectEvent?.Invoke(lastWell);

            if (!overrideItem && !overrideAttachment)
            {
                item.DisallowDespawn = false;
            }

            //Revert dungeon lighting fix
            foreach (var ren in _originalRenderers)
            {
                lastWell.actualFirearm.item.renderers.Remove(ren);
                item.renderers.Add(ren);
            }
            lastWell.actualFirearm.item.lightVolumeReceiver.SetRenderers(lastWell.actualFirearm.item.renderers);
            item.lightVolumeReceiver.SetRenderers(item.renderers);

            //// Collider fix attempt
            lastWell.actualFirearm.item.colliderGroups.RemoveAll(x => _colliderGroups.Contains(x));
            item.colliderGroups.AddRange(_colliderGroups);
            foreach (var group in _colliderGroups)
            {
                group.transform.SetParent(item.transform);
            }

            Util.PlayRandomAudioSource(magazineEjectSounds);
            Util.DelayIgnoreCollision(gameObject, lastWell.firearm.gameObject, false, 0.5f, item);
            foreach (var c in cartridges)
            {
                if (c && lastWell && lastWell.firearm)
                {
                    Util.DelayIgnoreCollision(c.gameObject, lastWell.firearm.gameObject, false, 0.5f, item);
                }
            }
            _firearmSave.Value.Clear();
            lastWell.currentMagazine = null;
            currentWell = null;
            foreach (var handle in handles)
            {
                handle.SetTouch(true);
                handle.SetTelekinesis(true);
            }
            //Destroy(joint);
            item.transform.SetParent(null);
            if (!overrideItem && !overrideAttachment)
            {
                item.physicBody.isKinematic = false;
                item.physicBody.rigidBody.WakeUp();
                item.physicBody.velocity = lastWell.actualFirearm.item.physicBody.velocity * 0.7f;
            }
            if (destroyOnEject && !overrideItem && !overrideAttachment)
            {
                item.Despawn();
            }
            //if (FirearmsSettings.magazinesHaveNoCollision) ToggleCollision(true);
        }
        UpdateCartridgePositions();
        item.lastInteractionTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<Cartridge>() is { } car && Util.CheckForCollisionWithThisCollider(collision, roundInsertCollider) && Time.time - lastEjectTime > 1f)
        {
            InsertRound(car, false, false);
        }
    }

    public void UpdateCartridgePositions()
    {
        foreach (var c in cartridges)
        {
            if (c && c.transform)
            {
                var positions = positionSets.FirstOrDefault(x => x.caliber.Equals(c.caliber))?.positions ?? cartridgePositions;
                var oddPositions = positionSets.FirstOrDefault(x => x.caliber.Equals(c.caliber))?.oddCountPositions ?? oddCountCartridgePositions;
                if (oddPositions?.Any() == true && cartridges.Count % 2 != 0)
                {
                    positions = oddPositions;
                }

                if (positions.Length - 1 < cartridges.IndexOf(c) || !positions[cartridges.IndexOf(c)])
                {
                    c.transform.parent = nullCartridgePosition;
                    c.transform.localPosition = Vector3.zero;
                    c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
                else
                {
                    c.transform.parent = positions[cartridges.IndexOf(c)];
                    c.transform.localPosition = Vector3.zero;
                    c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
            }
        }
    }

    public void UpdateFeeders()
    {
        var feeders = feederObjects;

        if (cartridges.Any() && positionSets.FirstOrDefault(x => x.caliber.Equals(cartridges[0].caliber)) is { } set)
        {
            feeders = set.feeders;
        }

        if (feeders.Count > cartridges.Count && feeders[cartridges.Count])
        {
            feeders[cartridges.Count].SetActive(true);
        }
    }

    public void ToggleCollision(bool active)
    {
        foreach (var c in colliders)
        {
            c.enabled = active;
        }
    }

    public void SaveCustomData()
    {
        if (!overrideItem && !overrideAttachment)
        {
            _saveData.ItemID = item.itemId;
            _saveData.GetContentsFromMagazine(this);

            if (_firearmSave is not null)
            {
                _saveData.CloneTo(_firearmSave.Value);
            }
        }
        else
        {
            _firearmSave.Value.GetContentsFromMagazine(this);
        }
    }

    private void UpdateAllLightVolumeReceivers(LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
    {
        foreach (var lvr in GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != item.lightVolumeReceiver))
        {
            Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
        }
    }

    public bool CanGrab
    {
        get
        {
            return canBeGrabbedInWell || (currentWell && currentWell.forceCanGrab);
        }
    }

    public delegate void LoadFinished(Magazine mag);

    public event LoadFinished OnLoadFinished;

    public delegate void OnConsume(Cartridge c);

    public event OnConsume OnConsumeEvent;

    public delegate void OnEject(MagazineWell well);

    public event OnEject OnEjectEvent;

    public delegate void OnInsert(MagazineWell well);

    public event OnInsert OnInsertEvent;

    public string GetCaliber()
    {
        return caliber;
    }

    public int GetCapacity()
    {
        return ActualCapacity;
    }

    public List<Cartridge> GetLoadedCartridges()
    {
        return cartridges.ToList();
    }

    public void LoadRound(Cartridge cartridge)
    {
        InsertRound(cartridge, true, true);
    }

    public void ClearRounds()
    {
        foreach (var car in cartridges)
        {
            car.item.Despawn(0.05f);
        }
        cartridges.Clear();

        SaveCustomData();
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public bool GetForceCorrectCaliber()
    {
        return forceCorrectCaliber;
    }

    public List<string> GetAlternativeCalibers()
    {
        return alternateCalibers;
    }
}