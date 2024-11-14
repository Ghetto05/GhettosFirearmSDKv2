using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class Speedloader : MonoBehaviour, IAmmunitionLoadable
    {
        public Item item;
        public bool deleteIfEmpty;
        public List<Transform> mountPoints;
        public List<Collider> loadColliders;
        public List<string> calibers;
        public List<AudioSource> insertSounds;

        public Cartridge[] loadedCartridges;
        private MagazineSaveData _data;
        private bool _allowInsert;
        private bool _startRemoving;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        public void InvokedStart()
        {
            loadedCartridges = new Cartridge[mountPoints.Count];
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;

            if (item.TryGetCustomData(out _data))
            {
                for (var i = 0; i < loadedCartridges.Length; i++)
                {
                    var i2 = i;
                    if (_data.Contents[i2] != null)
                    {
                        var i1 = i;
                        Util.SpawnItem(_data.Contents[i]?.ItemId, $"[Saved speedloader rounds - Index {i2} on {item?.itemId}]", ci =>
                        {
                            var c = ci.GetComponent<Cartridge>();
                            _data.Contents[i1].Apply(c);
                            LoadSlot(i2, c, false);
                        }, transform.position + Vector3.up * 3);
                    }
                }
            }
            else
            {
                item.AddCustomData(new MagazineSaveData());
                item.TryGetCustomData(out _data);
            }
            _allowInsert = true;
            if (item != null && item.holder != null) Item_OnSnapEvent(item.holder);
        }

        private void Item_OnUnSnapEvent(Holder holder)
        {
            foreach (var c in loadedCartridges)
            {
                if (c != null) c.ToggleCollision(true);
            }
        }

        private void Item_OnSnapEvent(Holder holder)
        {
            foreach (var c in loadedCartridges)
            {
                if (c != null) c.ToggleCollision(false);
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            UpdateCartridges();
            _startRemoving = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_allowInsert) return;
            if (collision.collider.GetComponentInParent<Cartridge>() is { } car && !car.loaded)
            {
                foreach (var insertCollider in loadColliders)
                {
                    if (Util.CheckForCollisionWithThisCollider(collision, insertCollider))
                    {
                        var index = loadColliders.IndexOf(insertCollider);
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
            for (var i = 0; i < mountPoints.Count; i++)
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
            for (var i = 0; i < loadedCartridges.Length; i++)
            {
                foreach (var c in loadedCartridges)
                {
                    if (c != null && !c.loaded)
                        c.ToggleCollision(item.holder == null);
                }

                if (loadedCartridges[i] != null && loadedCartridges[i].transform.parent == null)
                    UpdateCartridges();

                if (_startRemoving && loadedCartridges[i] != null && loadedCartridges[i].loaded)
                {
                    loadedCartridges[i] = null;
                    if (Empty() && deleteIfEmpty)
                        item.Despawn();
                }
            }
        }

        private bool Empty()
        {
            for (var i = 0; i < loadedCartridges.Length; i++)
            {
                if (loadedCartridges[i] != null) return false;
            }
            return true;
        }

        public void SaveCartridges()
        {
            _data.Contents = new CartridgeSaveData[loadedCartridges.Length];
            for (var i = 0; i < loadedCartridges.Length; i++)
            {
                _data.Contents[i] = new CartridgeSaveData(loadedCartridges[i]?.item.itemId, loadedCartridges[i]?.Fired ?? false);
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
            if (FirstFreeSlot(out var free))
                LoadSlot(free, cartridge);
        }

        private bool FirstFreeSlot(out int firstFree)
        {
            for (var i = 0; i < loadedCartridges.Length; i++)
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
            foreach (var c in loadedCartridges.Where(x => x != null && x.item != null))
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
