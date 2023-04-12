using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Collections;

namespace GhettosFirearmSDKv2
{
    public class Speedloader : MonoBehaviour
    {
        public Item item;

        public List<Transform> mountPoints;
        public List<Collider> loadColliders;
        public List<string> calibers;
        public List<AudioSource> insertSounds;

        public Cartridge[] loadedCartridges;
        private MagazineSaveData data;
        private bool allowInsert = false;
        private bool startRemoving = false;

        private void Awake()
        {
            loadedCartridges = new Cartridge[mountPoints.Count];
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            StartCoroutine(Delayed());
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

        private IEnumerator Delayed()
        {
            yield return new WaitForSeconds(0.03f);
            if (item.TryGetCustomData(out data))
            {
                for (int i = 0; i < loadedCartridges.Length; i++)
                {
                    int i2 = i;
                    if (data.contents[i2] != null) Catalog.GetData<ItemData>(data.contents[i]).SpawnAsync(ci => { Cartridge c = ci.GetComponent<Cartridge>(); LoadSlot(i2, c, false); }, transform.position + Vector3.up * 3);
                }
            }
            else
            {
                item.AddCustomData(new MagazineSaveData());
                item.TryGetCustomData(out data);
            }
            allowInsert = true;
            yield return new WaitForSeconds(0.1f);
            if (item.holder != null) Item_OnSnapEvent(item.holder);
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
            if (loadedCartridges[index] == null && Util.AllowLoadCatridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(insertSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.disallowDespawn = true;
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
                if (startRemoving && loadedCartridges[i] != null && (loadedCartridges[i].fired || loadedCartridges[i].transform.parent != mountPoints[i])) loadedCartridges[i] = null;
            }
        }

        public void SaveCartridges()
        {
            data.contents = new string[loadedCartridges.Length];
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                data.contents[i] = loadedCartridges[i]?.item.itemId;
            }
        }
    }
}
