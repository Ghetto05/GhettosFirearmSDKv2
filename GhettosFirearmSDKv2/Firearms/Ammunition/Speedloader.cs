﻿using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Collections;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class Speedloader : MonoBehaviour, IAmmunitionLoadable
    {
        public Item item;
        public bool deleteIfEmpty = false;
        public List<Transform> mountPoints;
        public List<Collider> loadColliders;
        public List<string> calibers;
        public List<AudioSource> insertSounds;

        public Cartridge[] loadedCartridges;
        private MagazineSaveData data;
        private bool allowInsert = false;
        private bool startRemoving = false;

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            loadedCartridges = new Cartridge[mountPoints.Count];
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;

            if (item.TryGetCustomData(out data))
            {
                for (int i = 0; i < loadedCartridges.Length; i++)
                {
                    int i2 = i;
                    if (data.contents[i2] != null)
                        Util.SpawnItem(data.contents[i], $"[Saved speedloader rounds - Index {i2} on {item?.itemId}]", ci =>
                        {
                            Cartridge c = ci.GetComponent<Cartridge>();
                            LoadSlot(i2, c, false);
                        }, transform.position + Vector3.up * 3);
                }
            }
            else
            {
                item.AddCustomData(new MagazineSaveData());
                item.TryGetCustomData(out data);
            }
            allowInsert = true;
            if (item.holder != null) Item_OnSnapEvent(item.holder);
        }

        private void Item_OnUnSnapEvent(Holder holder)
        {
            foreach (Cartridge c in loadedCartridges)
            {
                if (c != null) c.ToggleCollision(true);
            }
        }

        private void Item_OnSnapEvent(Holder holder)
        {
            foreach (Cartridge c in loadedCartridges)
            {
                if (c != null) c.ToggleCollision(false);
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            UpdateCartridges();
            startRemoving = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!allowInsert) return;
            if (collision.collider.GetComponentInParent<Cartridge>() is Cartridge car && !car.loaded)
            {
                foreach (Collider insertCollider in loadColliders)
                {
                    if (Util.CheckForCollisionWithThisCollider(collision, insertCollider))
                    {
                        int index = loadColliders.IndexOf(insertCollider);
                        LoadSlot(index, car);
                    }
                }
            }
        }

        public void LoadSlot(int index, Cartridge cartridge, bool overrideSave = true)
        {
            if (loadedCartridges[index] == null && Util.AllowLoadCartridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(insertSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.DisallowDespawn = true;
                cartridge.ToggleHandles(false);
                //cartridge.ToggleCollision(false);
                cartridge.UngrabAll();
                Util.IgnoreCollision(cartridge.gameObject, gameObject, true);
                cartridge.GetComponent<Rigidbody>().isKinematic = true;
                cartridge.transform.parent = mountPoints[index];
                cartridge.transform.localPosition = Vector3.zero;
                cartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
                if (overrideSave) SaveCartridges();
            }
            UpdateCartridges();
        }

        private void UpdateCartridges()
        {
            for (int i = 0; i < mountPoints.Count; i++)
            {
                if (loadedCartridges[i] != null)
                {
                    loadedCartridges[i].GetComponent<Rigidbody>().isKinematic = true;
                    loadedCartridges[i].transform.parent = mountPoints[i];
                    loadedCartridges[i].transform.localPosition = Vector3.zero;
                    loadedCartridges[i].transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                foreach (Cartridge c in loadedCartridges)
                {
                    if (c != null && !c.loaded)
                        c.ToggleCollision(item.holder == null);
                }

                if (loadedCartridges[i] != null && loadedCartridges[i].transform.parent == null)
                    UpdateCartridges();

                if (startRemoving && loadedCartridges[i] != null && loadedCartridges[i].loaded)
                {
                    loadedCartridges[i] = null;
                    if (Empty() && deleteIfEmpty)
                        item.Despawn();
                }
            }
        }

        private bool Empty()
        {
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                if (loadedCartridges[i] != null) return false;
            }
            return true;
        }

        public void SaveCartridges()
        {
            data.contents = new string[loadedCartridges.Length];
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                data.contents[i] = loadedCartridges[i]?.item.itemId;
            }
        }

        public string GetCaliber()
        {
            return calibers.First();
        }

        public int GetCapacity()
        {
            return mountPoints.Count;
        }

        public List<Cartridge> GetLoadedCartridges()
        {
            return loadedCartridges.ToList();
        }

        public void LoadRound(Cartridge cartridge)
        {
            if (FirstFreeSlot(out int free))
                LoadSlot(free, cartridge);
        }

        private bool FirstFreeSlot(out int firstFree)
        {
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                firstFree = i;
                if (loadedCartridges[i] == null)
                    return true; 
            }

            firstFree = 0;
            return false;
        }

        public void ClearRounds()
        {
            foreach (Cartridge c in loadedCartridges.Where(x => x != null && x.item != null))
            {
                c.item.Despawn(0.5f);
            }

            loadedCartridges = new Cartridge[mountPoints.Count];
            
            SaveCartridges();
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
