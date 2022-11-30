using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    public class Magazine : MonoBehaviour
    {
        public string magazineType;
        public string caliber;
        public List<string> alternateCalibers;
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
        public Transform overrideItem;
        public List<Collider> colliders;
        private List<Renderer> originalRenderers;

        private void Update()
        {
            if (Settings_LevelModule.local.magazinesHaveNoCollision && currentWell != null) ToggleCollision(false);
            UpdateCartridgePositions();
            if (currentWell != null && currentWell.firearm != null && canBeGrabbedInWell)
            {
                foreach (Handle handle in handles)
                {
                    handle.SetTouch(currentWell.firearm.item.holder == null);
                    handle.SetTelekinesis(currentWell.firearm.item.holder == null);
                }
            }
        }

        private void Awake()
        {
            cartridges = new List<Cartridge>();
            if (overrideItem == null) item = this.GetComponent<Item>();
            else item = overrideItem.GetComponent<Item>();
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnDespawnEvent += Item_OnDespawnEvent;
            if (overrideItem != null) overrideItem.GetComponent<FirearmBase>().OnCollisionEvent += OnCollisionEnter;
            StartCoroutine(DelayedLoad());
        }

        private void Item_OnUnSnapEvent(Holder holder)
        {
            foreach (Cartridge car in cartridges)
            {
                car.DisableCull();
            }
        }

        IEnumerator DelayedLoad()
        {
            yield return new WaitForSeconds(0.03f);
            if (item.TryGetCustomData(out MagazineSaveData data))
            {
                data.ApplyToMagazine(this);
            }
            else
            {
                if (defaultLoad == null) yield break;
                defaultLoad.Load(this);
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
                c.item.disallowRoomDespawn = false;
                c.transform.parent = null;
            }
            UpdateCartridgePositions();
            SaveCustomData();
            return c;
        }

        public void InsertRound(Cartridge c, bool silent)
        {
            if (cartridges.Count < maximumCapacity && !cartridges.Contains(c) && Util.AllowLoadCatridge(c, this))
            {
                c.item.disallowDespawn = true;
                c.item.disallowRoomDespawn = true;
                c.loaded = true;
                c.ToggleHandles(false);
                c.ToggleCollision(false);
                cartridges.Insert(0, c);
                c.UngrabAll();
                Util.IgnoreCollision(c.gameObject, this.gameObject, true);
                if (!silent) Util.PlayRandomAudioSource(roundInsertSounds);
                c.GetComponent<Rigidbody>().isKinematic = true;
                c.transform.parent = nullCartridgePosition;
                c.transform.localPosition = Vector3.zero;
                c.transform.localEulerAngles = Vector3.zero;
            }
            UpdateCartridgePositions();
            SaveCustomData();
        }

        public Cartridge ConsumeRound()
        {
            Cartridge c = null;
            if (cartridges.Count > 0)
            {
                if (Settings_LevelModule.local.infiniteAmmo)
                {
                    GameObject obj = Instantiate(cartridges[0].gameObject, cartridges[0].transform.position, cartridges[0].transform.rotation);
                    c = obj.GetComponent<Cartridge>();
                }
                else
                {
                    c = cartridges[0];
                    cartridges.RemoveAt(0);
                }
            }
            UpdateCartridgePositions();
            SaveCustomData();
            return c;
        }

        public void Mount(MagazineWell well, Rigidbody rb)
        {
            item.disallowDespawn = true;
            item.disallowRoomDespawn = true;

            //renderers reassignment to fix dungeon lighting
            if (originalRenderers == null) originalRenderers = item.renderers.ToList();
            foreach (Renderer ren in originalRenderers)
            {
                well.firearm.item.renderers.Add(ren);
                item.renderers.Remove(ren);
            }
            well.firearm.item.lightVolumeReceiver.SetRenderers(well.firearm.item.renderers);
            item.lightVolumeReceiver.SetRenderers(item.renderers);

            if (Settings_LevelModule.local.magazinesHaveNoCollision) ToggleCollision(false);
            currentWell = well;
            currentWell.currentMagazine = this;
            RagdollHand[] hands = item.handlers.ToArray();
            foreach (RagdollHand hand in hands)
            {
                hand.UnGrab(false);
            }
            foreach (Cartridge c in cartridges)
            {
                Util.IgnoreCollision(c.gameObject, currentWell.firearm.gameObject, true);
            }
            Util.PlayRandomAudioSource(magazineInsertSounds);
            Util.IgnoreCollision(this.gameObject, currentWell.firearm.gameObject, true);
            this.transform.position = well.mountPoint.position;
            this.transform.rotation = well.mountPoint.rotation;
            joint = this.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = rb;
            if (Settings_LevelModule.local.magazinesHaveNoCollision) joint.massScale = 99999f;
            if (!canBeGrabbedInWell)
            {
                foreach (Handle handle in handles)
                {
                    handle.SetTouch(false);
                    handle.SetTelekinesis(false);
                }
            }

            //Saving firearm's magazine 
            if (currentWell.firearm.GetType() != typeof(AttachmentFirearm))
            {
                currentWell.firearm.item.RemoveCustomData<MagazineSaveData>();
                MagazineSaveData data = new MagazineSaveData();
                data.GetContentsFromMagazine(this);
                data.itemID = item.itemId;
                currentWell.firearm.item.AddCustomData(data);
            }

            UpdateCartridgePositions();
        }

        public void Eject()
        {
            if (joint != null)
            {
                item.disallowDespawn = false;
                item.disallowRoomDespawn = false;

                //Revert dungeon lighting fix
                foreach (Renderer ren in originalRenderers)
                {
                    currentWell.firearm.item.renderers.Remove(ren);
                    item.renderers.Add(ren);
                }
                currentWell.firearm.item.lightVolumeReceiver.SetRenderers(currentWell.firearm.item.renderers);
                item.lightVolumeReceiver.SetRenderers(item.renderers);

                Util.PlayRandomAudioSource(magazineEjectSounds);
                Util.DelayIgnoreCollision(this.gameObject, currentWell.firearm.gameObject, false, 0.5f, item);
                foreach (Cartridge c in cartridges)
                {
                    Util.DelayIgnoreCollision(c.gameObject, currentWell.firearm.gameObject, false, 0.5f, item);
                }
                currentWell.firearm.item.RemoveCustomData<MagazineSaveData>();
                currentWell.currentMagazine = null;
                currentWell = null;
                foreach (Handle handle in handles)
                {
                    handle.SetTouch(true);
                    handle.SetTelekinesis(true);
                }
                Destroy(joint);
                item.rb.WakeUp();
                if (destroyOnEject) item.Despawn();
                if (Settings_LevelModule.local.magazinesHaveNoCollision) ToggleCollision(true);
            }
            UpdateCartridgePositions();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.GetComponentInParent<Cartridge>() is Cartridge car && Util.CheckForCollisionWithThisCollider(collision, roundInsertCollider))
            {
                InsertRound(car, false);
            }
        }

        private void UpdateCartridgePositions()
        {
            foreach (Cartridge c in cartridges)
            {
                if (c != null && c.transform != null)
                {
                    if (cartridgePositions.Length - 1 < cartridges.IndexOf(c) || cartridgePositions[cartridges.IndexOf(c)] == null)
                    {
                        c.transform.parent = nullCartridgePosition;
                        c.transform.localPosition = Vector3.zero;
                        c.transform.localEulerAngles = Vector3.zero;
                    }
                    else
                    {
                        c.transform.parent = cartridgePositions[cartridges.IndexOf(c)];
                        c.transform.localPosition = Vector3.zero;
                        c.transform.localEulerAngles = Vector3.zero;
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
            MagazineSaveData data = new MagazineSaveData();
            data.itemID = item.itemId;
            data.GetContentsFromMagazine(this);
            item.RemoveCustomData<MagazineSaveData>();
            item.AddCustomData(data);
        }
    }
}
