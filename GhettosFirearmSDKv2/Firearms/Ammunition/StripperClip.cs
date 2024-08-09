using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class StripperClip : MonoBehaviour, IAmmunitionLoadable
    {
        public string caliber;
        public string clipType;
        public int capacity;
        
        public Item item;
        public Transform[] cartridgePositions;
        public GameObject insertColliderRoot;
        public Collider insertCollider;
        public Collider mountCollider;
        public bool insertFromBottom;
        public Collider roundLoadCollider;
        public Transform roundEjectPoint;

        public AudioSource[] pushSounds;
        public AudioSource[] loadSounds;
        public AudioSource[] unloadSounds;
        public AudioSource[] mountSounds;
        public AudioSource[] removeSounds;

        public MagazineLoad defaultLoad;
        
        public List<Cartridge> loadedCartridges;

        public bool loadable = false;
        private StripperClipWell _currentWell;
        private float _lastEjectTime;
        private float _lastRemoveTime;
        private float _lastPushTime;
        private MagazineSaveData _data;

        private void Start()
        {
            loadedCartridges = new List<Cartridge>();
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        private void InvokedStart()
        {
            item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
            item.OnGrabEvent += ItemOnOnGrabEvent;

            if (!item.TryGetCustomData(out _data))
            {
                _data = defaultLoad != null ? defaultLoad.ToSaveData() : new MagazineSaveData();
                item.AddCustomData(_data);
            }
            _data.ApplyToMagazine(this);
        }

        private void ItemOnOnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            if (_currentWell != null) 
                RemoveFromGun();
        }

        private void ItemOnOnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart)
                EjectRound();
        }

        private void FixedUpdate()
        {
            UpdatePositions();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_currentWell == null && collision.contacts[0].otherCollider.GetComponentInParent<StripperClipWell>() != null)
            {
                StripperClipWell well = collision.contacts[0].otherCollider.GetComponentInParent<StripperClipWell>();
                if ((well.clipType.Equals(clipType) || !Settings.doMagazineTypeChecks) && Util.CheckForCollisionWithBothColliders(collision, mountCollider, well.mountCollider))
                    MountToGun(well);
            }
            if (collision.collider.GetComponentInParent<Cartridge>() is Cartridge c && Util.CheckForCollisionWithThisCollider(collision, roundLoadCollider) && Time.time - _lastEjectTime > 1f)
            {
                InsertRound(c);
            }
            if (Util.CheckForCollisionWithThisCollider(collision, insertCollider))
            {
                PushRoundToMagazine();
            }
        }

        private void UpdatePositions(bool rotate = false)
        {
            if (loadedCartridges.Count == 0)
            {
                insertColliderRoot.SetActive(false);
                return;
            }

            Cartridge[] cn = loadedCartridges.Where(c => c == null).ToArray();
            foreach (Cartridge c in cn)
            {
                loadedCartridges.Remove(c);
            }

            insertColliderRoot.SetActive(true);
            insertColliderRoot.transform.SetParent(cartridgePositions[loadedCartridges.Count - 1]);
            insertColliderRoot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Cartridge c in loadedCartridges)
            {
                Quaternion rot = c.transform.localRotation;
                c.transform.SetParent(cartridgePositions[loadedCartridges.IndexOf(c)]);
                if (!rotate)
                    c.transform.SetLocalPositionAndRotation(Vector3.zero, rot);
                else
                    c.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(Util.RandomCartridgeRotation()));
            }
        }

        public void InsertRound(Cartridge c, bool silent = false, bool forced = false, bool save = true)
        {
            if ((loadable || forced) && loadedCartridges.Count < capacity && !c.loaded && (c.caliber.Equals(caliber) || !Settings.doCaliberChecks || forced))
            {
                if (!silent)
                    Util.PlayRandomAudioSource(loadSounds);
                c.UngrabAll();
                c.ToggleCollision(false);
                c.ToggleHandles(false);
                c.item.DisallowDespawn = true;
                c.item.physicBody.isKinematic = true;
                c.item.transform.SetParent(cartridgePositions[0]);
                c.item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(Util.RandomCartridgeRotation()));
                if (insertFromBottom)
                    loadedCartridges.Insert(0, c);
                else
                    loadedCartridges.Add(c);
                if (save)
                    SaveContent();
            }
            UpdatePositions(true);
        }

        public void EjectRound()
        {
            if (loadedCartridges.Count > 0)
            {
                _lastEjectTime = Time.time;
                Util.PlayRandomAudioSource(unloadSounds);
                Cartridge c = insertFromBottom ? loadedCartridges[0] : loadedCartridges.Last();
                loadedCartridges.Remove(c);
                SaveContent();
                c.item.physicBody.isKinematic = false;
                c.item.transform.SetParent(null);
                c.item.DisallowDespawn = false;
                c.ToggleCollision(true);
                c.ToggleHandles(true);
                c.item.transform.SetPositionAndRotation(roundEjectPoint.position, roundEjectPoint.rotation);
            }
            UpdatePositions(true);
        }

        public void PushRoundToMagazine()
        {
            if (Time.time - _lastPushTime < 0.02f)
                return;
            if (_currentWell != null && loadedCartridges.Count > 0 && _currentWell.magazineWell.firearm.magazineWell != null && _currentWell.magazineWell.firearm.magazineWell.currentMagazine != null)
            {
                Magazine mag = _currentWell.magazineWell.firearm.magazineWell.currentMagazine;
                if (mag.cartridges.Count < mag.maximumCapacity)
                {
                    Cartridge c = loadedCartridges[0];
                    loadedCartridges.RemoveAt(0);
                    SaveContent();
                    mag.InsertRound(c, true, true);
                    Util.PlayRandomAudioSource(pushSounds);
                    _lastPushTime = Time.time;
                }
            }
        }

        public void MountToGun(StripperClipWell well)
        {
            if (_currentWell != null ||
                Time.time - _lastRemoveTime < 0.5f ||
                well.currentClip != null ||
                (well.bolt.state != well.allowedState && !well.alwaysAllow) ||
                (!well.clipType.Equals(clipType) && Settings.doMagazineTypeChecks) ||
                well.blockingAttachmentPoints.Any(at => at.currentAttachments.Any()))
            {
                return;
            }

            _currentWell = well;
            _currentWell.currentClip = this;
            foreach (Handle handle in item.handles)
            {
                handle.Release();
            }
            item.DisallowDespawn = true;
            item.physicBody.isKinematic = true;
            Util.IgnoreCollision(item.gameObject, _currentWell.magazineWell.firearm.gameObject, true);
            item.transform.SetParent(_currentWell.mountPoint);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Util.PlayRandomAudioSource(mountSounds);
        }

        public void RemoveFromGun()
        {
            if (_currentWell == null)
                return;

            _lastRemoveTime = Time.time;
            item.DisallowDespawn = false;
            item.physicBody.isKinematic = false;
            item.transform.SetParent(null);
            Util.DelayIgnoreCollision(item.gameObject, _currentWell.magazineWell.firearm.gameObject, false, 0.5f, item);
            _currentWell.currentClip = null;
            _currentWell = null;
            Util.PlayRandomAudioSource(removeSounds);
        }

        private void SaveContent()
        {
            _data.GetContentsFromClip(this);
        }

        public string GetCaliber()
        {
            return caliber;
        }

        public int GetCapacity()
        {
            return capacity;
        }

        public List<Cartridge> GetLoadedCartridges()
        {
            return loadedCartridges.ToList();
        }

        public void LoadRound(Cartridge cartridge)
        {
            InsertRound(cartridge, true, true);
        }

        public void ClearRounds()
        {
            foreach (Cartridge cartridge in loadedCartridges)
            {
                cartridge.item.Despawn(0.01f);
            }
            loadedCartridges.Clear();

            SaveContent();
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
}
