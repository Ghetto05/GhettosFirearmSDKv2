using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class Magazine : MonoBehaviour
    {
        public bool ejectOnLastRoundFired = false;
        public bool infinite = false;
        public string magazineType;
        public string caliber;
        public List<string> alternateCalibers;
        public bool forceCorrectCaliber = false;
        public List<Cartridge> cartridges;
        public int maximumCapacity;
        public bool canEjectRounds;
        public bool destroyOnEject = false;
        public Collider roundInsertCollider;
        public AudioSource[] roundInsertSounds;
        public Transform roundEjectPoint;
        public AudioSource[] roundEjectSounds;
        public AudioSource[] magazineEjectSounds;
        public AudioSource[] magazineInsertSounds;
        public Collider mountCollider;
        public bool canBeGrabbedInWell;
        public List<Handle> handles;
        public MagazineWell currentWell;
        FixedJoint joint;
        public Transform nullCartridgePosition;
        public Transform[] cartridgePositions;
        public Item item;
        public MagazineLoad defaultLoad;
        public bool hasOverrideLoad;
        public Item overrideItem;
        public List<Collider> colliders;
        private List<Renderer> originalRenderers;
        MagazineSaveData saveData;
        SaveNodeValueMagazineContents firearmSave;
        public List<GameObject> feederObjects;
        public bool loadable = false;
        public float lastEjectTime = 0f;
        public BoltBase bolt;
        public bool onlyAllowLoadWhenBoltIsBack;
        private List<ColliderGroup> colliderGroups = new List<ColliderGroup>();

        private void Update()
        {
            //if (FirearmsSettings.magazinesHaveNoCollision && currentWell != null) ToggleCollision(false);
            //UpdateCartridgePositions();
            if (currentWell != null && currentWell.firearm != null && canBeGrabbedInWell)
            {
                foreach (Handle handle in handles)
                {
                    handle.SetTouch(currentWell.firearm.item.holder == null);
                    handle.SetTelekinesis(currentWell == null);
                }
            }

            foreach (GameObject obj in feederObjects) obj.SetActive(false);
            if (feederObjects.Count > cartridges.Count && feederObjects[cartridges.Count] != null) feederObjects[cartridges.Count].SetActive(true);
        }

        public void InvokeLoadFinished() => OnLoadFinished?.Invoke(this);

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            cartridges = new List<Cartridge>();
            if (overrideItem == null)
                item = GetComponent<Item>();
            else
                item = overrideItem;
            if (item == null)
                return;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnDespawnEvent += Item_OnDespawnEvent;
            item.lightVolumeReceiver.onVolumeChangeEvent += UpdateAllLightVolumeReceivers;
            if (overrideItem == null)
            {
                if (item.TryGetCustomData(out saveData))
                {
                    saveData.ApplyToMagazine(this);
                }
                else
                {
                    saveData = new MagazineSaveData();
                    item.AddCustomData(saveData);
                    if (defaultLoad != null)
                        defaultLoad.Load(this);
                    else
                    {
                        InvokeLoadFinished();
                        loadable = true;
                    }
                }
            }
            else
            {
                overrideItem.GetComponent<FirearmBase>().OnCollisionEvent += OnCollisionEnter;
                if (overrideItem.TryGetComponent(out Firearm f))
                {
                    firearmSave = f.saveData.firearmNode.GetOrAddValue("MagazineSaveData", new SaveNodeValueMagazineContents());
                    if (defaultLoad != null)
                    {
                        defaultLoad.Load(this);
                        return;
                    }
                    firearmSave.value.ApplyToMagazine(this);
                }
                else InvokeLoadFinished();
            }
        }

        private void Item_OnUnSnapEvent(Holder holder)
        {
            foreach (Cartridge car in cartridges)
            {
                car.DisableCull();
            }
        }

        private void Item_OnDespawnEvent(EventTime eventTime)
        {
            foreach (Cartridge c in cartridges)
            {
                if (c!=null) c.item.Despawn();
            }
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart && canEjectRounds)
            {
                EjectRound();
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            if (canBeGrabbedInWell) Eject();
        }

        public Cartridge EjectRound()
        {
            Cartridge c = null;
            if (cartridges.Count > 0)
            {
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
                c.item.disallowDespawn = false;
                c.transform.parent = null;
            }
            UpdateCartridgePositions();
            SaveCustomData();
            return c;
        }

        private bool BoltExistsAndIsPulled() => !onlyAllowLoadWhenBoltIsBack || bolt == null || bolt.state == BoltBase.BoltState.Back || bolt.state == BoltBase.BoltState.LockedBack;

        public void InsertRound(Cartridge c, bool silent, bool forced, bool save = true, bool atBottom = false)
        {
            if (cartridges.Count < maximumCapacity && !cartridges.Contains(c) && (Util.AllowLoadCatridge(c, this) || forced) && (!c.loaded && BoltExistsAndIsPulled() || forced))
            {
                c.item.disallowDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                if (!atBottom)
                    cartridges.Insert(0, c);
                else
                    cartridges.Add(c);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, gameObject, true);
                if (!silent) Util.PlayRandomAudioSource(roundInsertSounds);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = nullCartridgePosition;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Util.RandomCartridgeRotation();
            }
            UpdateCartridgePositions();
            if (save) SaveCustomData();
        }

        public Cartridge ConsumeRound()
        {
            Cartridge c = null;
            if (cartridges.Count > 0)
            {
                c = cartridges[0];
                OnConsumeEvent?.Invoke(c);
                Util.IgnoreCollision(c.gameObject, gameObject, false);
                cartridges.RemoveAt(0);
                if (infinite || FirearmsSettings.infiniteAmmo)
                {
                    Util.SpawnItem(c.item.itemId, "[Loaded round in magazine]",car =>
                    {
                        Cartridge newC = car.GetComponent<Cartridge>();
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
            if (overrideItem == null) item.disallowDespawn = true;

            //renderers reassignment to fix dungeon lighting
            if (originalRenderers == null) originalRenderers = item.renderers.ToList();
            foreach (Renderer ren in originalRenderers)
            {
                well.firearm.item.renderers.Add(ren);
                item.renderers.Remove(ren);
            }
            well.firearm.item.lightVolumeReceiver.SetRenderers(well.firearm.item.renderers);
            item.lightVolumeReceiver.SetRenderers(item.renderers);

            //if (FirearmsSettings.magazinesHaveNoCollision) ToggleCollision(false);
            currentWell = well;
            currentWell.currentMagazine = this;
            RagdollHand[] hands = item.handlers.Where(h => handles.Contains(h.grabbedHandle)).ToArray();
            foreach (RagdollHand hand in hands)
            {
                hand.UnGrab(false);
            }
            foreach (Cartridge c in cartridges)
            {
                Util.IgnoreCollision(c.gameObject, currentWell.firearm.gameObject, true);
            }
            if (!silent) Util.PlayRandomAudioSource(magazineInsertSounds);
            Util.IgnoreCollision(gameObject, currentWell.firearm.gameObject, true);
            if (overrideItem == null)
                item.physicBody.isKinematic = true;
            
            transform.SetParent(well.mountPoint);
            transform.position = well.mountPoint.position;
            transform.rotation = well.mountPoint.rotation;
            
            //// Collider fix attempt
            colliderGroups = item.colliderGroups.ToList();
            foreach (ColliderGroup group in colliderGroups)
            {
                group.transform.SetParent(currentWell.mountPoint);
            }
            item.colliderGroups.RemoveAll(x => colliderGroups.Contains(x));
            currentWell.firearm.item.colliderGroups.AddRange(colliderGroups);
            currentWell.firearm.item.RefreshCollision();
            
            foreach (Handle handle in handles)
            {
                if (!canBeGrabbedInWell)
                {
                    handle.SetTouch(false);
                }
                handle.SetTelekinesis(false);
            }

            if (overrideItem == null)
            {
                //Saving firearm's magazine
                firearmSave = FirearmSaveData.GetNode(currentWell.firearm).GetOrAddValue("MagazineSaveData", new SaveNodeValueMagazineContents());
                firearmSave.value.GetContentsFromMagazine(this);
                firearmSave.value.itemID = item.itemId;
            }

            UpdateCartridgePositions();
            OnInsertEvent?.Invoke(currentWell);
            Invoke(nameof(ResetRagdollCollision), 0.2f);
        }

        public void Eject()
        {
            if (currentWell != null)
            {
                MagazineWell lastWell = currentWell;
                OnEjectEvent?.Invoke(lastWell);
                
                if (overrideItem == null) item.disallowDespawn = false;

                //Revert dungeon lighting fix
                foreach (Renderer ren in originalRenderers)
                {
                    lastWell.firearm.item.renderers.Remove(ren);
                    item.renderers.Add(ren);
                }
                lastWell.firearm.item.lightVolumeReceiver.SetRenderers(lastWell.firearm.item.renderers);
                item.lightVolumeReceiver.SetRenderers(item.renderers);
                
                //// Collider fix attempt
                lastWell.firearm.item.colliderGroups.RemoveAll(x => colliderGroups.Contains(x));
                item.colliderGroups.AddRange(colliderGroups);
                foreach (ColliderGroup group in colliderGroups)
                {
                    group.transform.SetParent(item.transform);
                }

                Util.PlayRandomAudioSource(magazineEjectSounds);
                Util.DelayIgnoreCollision(gameObject, lastWell.firearm.gameObject, false, 0.5f, item);
                foreach (Cartridge c in cartridges)
                {
                    if (c != null && lastWell != null && lastWell.firearm != null)
                        Util.DelayIgnoreCollision(c.gameObject, lastWell.firearm.gameObject, false, 0.5f, item);
                }
                firearmSave.value.Clear();
                lastWell.currentMagazine = null;
                currentWell = null;
                foreach (Handle handle in handles)
                {
                    handle.SetTouch(true);
                    handle.SetTelekinesis(true);
                }
                //Destroy(joint);
                item.transform.SetParent(null);
                if (overrideItem == null)
                {
                    item.physicBody.isKinematic = false;
                    item.physicBody.rigidBody.WakeUp();
                    item.physicBody.velocity = lastWell.firearm.item.physicBody.velocity * 0.7f;
                    
                }
                if (destroyOnEject && overrideItem == null)
                    item.Despawn();
                //if (FirearmsSettings.magazinesHaveNoCollision) ToggleCollision(true);
                lastEjectTime = Time.time;
            }
            UpdateCartridgePositions();
        }

        private void ResetRagdollCollision()
        {
            if (currentWell != null)
                currentWell.firearm.item.RefreshCollision();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.GetComponentInParent<Cartridge>() is Cartridge car && Util.CheckForCollisionWithThisCollider(collision, roundInsertCollider) && Time.time - lastEjectTime > 1f)
            {
                InsertRound(car, false, false);
            }
        }

        public void UpdateCartridgePositions()
        {
            foreach (Cartridge c in cartridges)
            {
                if (c != null && c.transform != null)
                {
                    if (cartridgePositions.Length - 1 < cartridges.IndexOf(c) || cartridgePositions[cartridges.IndexOf(c)] == null)
                    {
                        c.transform.parent = nullCartridgePosition;
                        c.transform.localPosition = Vector3.zero;
                        c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                    }
                    else
                    {
                        c.transform.parent = cartridgePositions[cartridges.IndexOf(c)];
                        c.transform.localPosition = Vector3.zero;
                        c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                    }
                }
            }
        }

        public void ToggleCollision(bool active)
        {
            foreach (Collider c in colliders)
            {
                c.enabled = active;
            }
        }

        public void SaveCustomData()
        {
            if (overrideItem == null)
            {
                saveData.itemID = item.itemId;
                saveData.GetContentsFromMagazine(this);

                if (firearmSave != null)
                {
                    saveData.CloneTo(firearmSave.value);
                }
            }
            else
            {
                firearmSave.value.GetContentsFromMagazine(this);
            }
        }

        private void UpdateAllLightVolumeReceivers(LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
        {
            foreach (LightVolumeReceiver lvr in GetComponentsInChildren<LightVolumeReceiver>().Where(lvr => lvr != item.lightVolumeReceiver))
            {
                Util.UpdateLightVolumeReceiver(lvr, currentLightProbeVolume, lightProbeVolumes);
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
    }
}
