using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class Firearm : MonoBehaviour
    {
        public enum FireModes
        {
            Safe,
            Semi,
            Burst,
            Auto
        }

        public Item item;
        public bool triggerState;
        public BoltBase bolt;
        public Handle boltHandle;
        public MagazineWell magazineWell;
        public Transform hitscanMuzzle;
        public Transform actualHitscanMuzzle;
        public bool integrallySuppressed;
        public AudioSource[] fireSounds;
        public AudioSource[] suppressedFireSounds;
        public FireModes fireMode;
        public ParticleSystem defaultMuzzleFlash;
        public int burstSize = 3;
        public int roundsPerMinute;
        float lastPressTime = 0f;
        public float longPressTime;
        public float recoilModifier = 1f;
        public List<AttachmentPoint> attachmentPoints;
        bool countingForLongpress = false;
        public List<Attachment> allAttachments;
        private SaveData.AttachmentTree attachmentTree;
        public Texture icon;
        private List<Handle> preSnapActiveHandles;

        private void Awake()
        {
            item = this.GetComponent<Item>();
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.OnUngrabEvent += Item_OnUngrabEvent;
            Settings_LevelModule.OnValueChangedEvent += Settings_LevelModule_OnValueChangedEvent;
            Settings_LevelModule_OnValueChangedEvent();
            allAttachments = new List<Attachment>();
            foreach (AttachmentPoint ap in attachmentPoints)
            {
                ap.parentFirearm = this;
            }
            StartCoroutine(DelayedLoadAttachments());
        }

        private void CalculateMuzzle()
        {
            Transform t = hitscanMuzzle;
            foreach (Attachment a in allAttachments)
            {
                if (a.minimumMuzzlePosition != null && Vector3.Distance(hitscanMuzzle.position, a.minimumMuzzlePosition.position) > Vector3.Distance(hitscanMuzzle.position, t.position)) t = a.minimumMuzzlePosition;
            }
            actualHitscanMuzzle = t;
        }

        public void UpdateAttachments(bool initialSetup = false)
        {
            allAttachments = new List<Attachment>();
            AddAttachments(attachmentPoints);
            CalculateMuzzle();
            if (!initialSetup)
            {
                attachmentTree = new SaveData.AttachmentTree();
                attachmentTree.GetFromFirearm(this);
                item.RemoveCustomData<SaveData.AttachmentTree>();
                item.AddCustomData(attachmentTree);
            }
        }

        private void AddAttachments(List<AttachmentPoint> points)
        {
            foreach (AttachmentPoint point in points)
            {
                if (point.currentAttachment != null)
                {
                    allAttachments.Add(point.currentAttachment);
                    AddAttachments(point.currentAttachment.attachmentPoints);
                }
            }
        }

        IEnumerator DelayedLoadAttachments()
        {
            yield return new WaitForSeconds(1f);

            Addressables.LoadAssetAsync<Texture>(item.data.iconAddress).Completed += (System.Action<AsyncOperationHandle<Texture>>)(handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    icon = handle.Result;
                }
                else
                {
                    Debug.LogWarning((object)("Unable to load icon texture from location " + item.data.iconAddress));
                    Addressables.Release<Texture>(handle);
                }
            });

            item.TryGetCustomData(out SaveData.AttachmentTree tree);
            if (tree != null)
            {
                attachmentTree = tree;
                tree.ApplyToFirearm(this);
            }
            else
            {
                foreach (AttachmentPoint ap in attachmentPoints)
                {
                    ap.SpawnDefaultAttachment();
                }
            }
            CalculateMuzzle();
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (triggerState)
            {
                OnTriggerChangeEvent?.Invoke(false);
                triggerState = false;
            }
        }

        public AttachmentPoint GetSlotFromId(string id)
        {
            foreach (AttachmentPoint point in attachmentPoints)
            {
                if (point.id.Equals(id)) return point;
            }
            return null;
        }

        private void Settings_LevelModule_OnValueChangedEvent()
        {
        }

        private void Item_OnUnSnapEvent(Holder holder)
        {
            OnColliderToggleEvent?.Invoke(true);
            foreach (Handle han in preSnapActiveHandles)
            {
                han.SetTouch(true);
            }
        }

        private void Item_OnSnapEvent(Holder holder)
        {
            OnColliderToggleEvent?.Invoke(false);
            if (triggerState)
            {
                OnTriggerChangeEvent?.Invoke(false);
                triggerState = false;
            }
            preSnapActiveHandles = new List<Handle>();
            foreach (Handle han in item.handles)
            {
                if (han.enabled && han.touchCollider.enabled && han != item.mainHandleLeft && han != item.mainHandleRight)
                {
                    preSnapActiveHandles.Add(han);
                    han.SetTouch(false);
                }
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            if (boltHandle != null && handle == boltHandle && item.mainHandleLeft.handlers.Count == 0)
            {
                //if (heldNotBoltHandle() is Handle han) han.SetJointModifier(this, 1, 1, 100, 1);
                if (heldNotBoltHandle() is Handle han) han.SetJointToTwoHanded(han.handlers[0].side);
            }
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (handle == item.mainHandleLeft)
            {
                if (action == Interactable.Action.UseStart)
                {
                    OnTriggerChangeEvent?.Invoke(true);
                    triggerState = true;
                }
                else if (action == Interactable.Action.UseStop)
                {
                    OnTriggerChangeEvent?.Invoke(false);
                    triggerState = false;
                }

                if (action == Interactable.Action.AlternateUseStart)
                {
                    lastPressTime = Time.time;
                    countingForLongpress = true;
                }
                if (action == Interactable.Action.AlternateUseStop && countingForLongpress)
                {
                    countingForLongpress = false;
                    if (Time.time - lastPressTime >= longPressTime) LongPress();
                    else ShortPress();
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnCollisionEvent?.Invoke(collision);
        }

        private bool isSuppressed()
        {
            if (integrallySuppressed) return true;
            foreach (Attachment at in allAttachments)
            {
                if (at.isSuppressing) return true;
            }
            return false;
        }

        public void PlayFireSound(bool overrideSuppressedbool = false, bool suppressed = false)
        {
            bool supp = isSuppressed();
            if (overrideSuppressedbool) supp = suppressed;
            if (!supp)
            {
                Util.PlayRandomAudioSource(fireSounds);
                Util.AlertAllCreaturesInRange(hitscanMuzzle.position, 50);
            }
            else
            {
                Util.PlayRandomAudioSource(suppressedFireSounds);
            }
        }

        public void PlayMuzzleFlash()
        {
            bool overridden = false;
            foreach (Attachment at in allAttachments)
            {
                if (at.overridesMuzzleFlash) overridden = true;
                if (at.overridesMuzzleFlash && NoMuzzleFlashOverridingAttachmentChildren(at))
                {
                    if (at.newFlash != null)
                    {
                        at.newFlash.Play();
                    }
                }
            }

            //default
            if (!overridden && defaultMuzzleFlash is ParticleSystem mf) mf.Play();
        }

        private bool NoMuzzleFlashOverridingAttachmentChildren(Attachment attachment)
        {
            foreach (AttachmentPoint p in attachment.attachmentPoints)
            {
                if (p.currentAttachment != null)
                {
                    if (!NMFOACrecurve(p.currentAttachment)) return false;
                }
            }
            return true;
        }

        private bool NMFOACrecurve(Attachment attachment)
        {
            foreach (AttachmentPoint p in attachment.attachmentPoints)
            {
                if (p.currentAttachment != null)
                {
                    if (!NMFOACrecurve(p.currentAttachment)) return false;
                }
            }
            if (attachment.overridesMuzzleFlash) return false;
            return true;
        }

        private bool NoAttachments(Attachment attachment)
        {
            foreach (AttachmentPoint p in attachment.attachmentPoints)
            {
                if (p.currentAttachment != null) return false;
            }
            return true;
        }

        void ShortPress()
        {
            OnAltActionEvent?.Invoke(false);
            if (magazineWell != null && magazineWell.canEject)
            {
                if (!bolt.caught || (bolt.caught && magazineWell.IsEmptyAndHasMagazine())) magazineWell.Eject();
                else if (bolt.caught && !magazineWell.IsEmpty()) bolt.TryRelease();
            }
            else bolt.TryRelease();
        }

        void LongPress()
        {
            OnAltActionEvent?.Invoke(true);
        }

        public void SetFiremode(FireModes mode)
        {
            fireMode = mode;
            OnFiremodeChangedEvent?.Invoke();
        }

        //EVENTS
        public delegate void OnTriggerChange(bool isPulled);
        public event OnTriggerChange OnTriggerChangeEvent;

        public delegate void OnCollision(Collision collision);
        public event OnCollision OnCollisionEvent;

        public delegate void OnAction(Interactable.Action action);
        public event OnAction OnActionEvent;

        public delegate void OnAltAction(bool longPress);
        public event OnAltAction OnAltActionEvent;

        public delegate void OnToggleColliders(bool active);
        public event OnToggleColliders OnColliderToggleEvent;

        public delegate void OnFiremodeChanged();
        public event OnFiremodeChanged OnFiremodeChangedEvent;

        Handle heldNotBoltHandle()
        {
            foreach (Handle han in item.handles)
            {
                if (han != boltHandle && han.handlers.Count > 0) return han;
            }
            return null;
        }
    }
}
