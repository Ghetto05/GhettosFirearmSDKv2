using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class GunCase : MonoBehaviour
    {
        public bool isStatic;
        public List<GameObject> nonStaticObjects;
        [Space]
        public Item item;
        public Holder holder;
        [Space]
        public Animator animator;
        public string openingAnimationName;
        public string closingAnimationName;
        [Space]
        public UnityEvent openingEvent;
        public UnityEvent closingEvent;
        public UnityEvent openingStartedEvent;
        public UnityEvent closingFinishedEvent;
        [Space]
        public List<Handle> freezeHandles;
        public List<Handle> toggleHandles;

        private bool _closed = true;
        private bool _open = false;
        private bool _moving = false;
        private GunCaseSaveData _data;

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
            if (isStatic)
            {
                item.physicBody.isKinematic = true;
                foreach (Handle handle in freezeHandles)
                {
                    handle.SetTouch(false);
                    handle.SetTelekinesis(false);
                }
                foreach (GameObject obj in nonStaticObjects)
                {
                    obj.SetActive(false);
                }
            }
        }

        public void InvokedStart()
        {
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            if (item.TryGetCustomData(out _data))
            {
                if (string.IsNullOrWhiteSpace(_data.firearm))
                    return;

                Util.SpawnItem(_data.firearm, $"[Gun case - Save {_data.firearm}]", f =>
                {
                    f.physicBody.isKinematic = true;
                    f.SetOwner(Item.Owner.Player);
                    StartCoroutine(DelayedSnap(f));
                }, holder.transform.position - Vector3.down * 10, holder.transform.rotation, null, true, _data.firearmData.CloneJson());
            }
            else
            {
                _data = new GunCaseSaveData();
                item.AddCustomData(_data);
            }
        }

        private IEnumerator DelayedSnap(Item f)
        {
            yield return new WaitForSeconds(0.5f);
            holder.Snap(f);
        }

        private void Holder_UnSnapped(Item f)
        {
            if (f.despawning)
                return;
            f.Hide(false);
            if (!f.isCulled)
            {
                _data.firearm = "";
                _data.firearmData = null;
            }
        }

        private void Holder_Snapped(Item f)
        {
            f.Hide(_closed);
            _data.firearm = f.itemId;
            _data.firearmData = f.contentCustomData.CloneJson();
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                if (freezeHandles.Contains(handle))
                {
                    item.physicBody.isKinematic = !item.physicBody.isKinematic;
                    item.DisallowDespawn = item.physicBody.isKinematic;
                }
                if (toggleHandles.Contains(handle))
                {
                    if (_open) Close();
                    else Open();
                }
            }
        }

        [EasyButtons.Button]
        public void Open()
        {
            if (_open || _moving) return;
            StartCoroutine(OpenIE());
        }

        [EasyButtons.Button]
        public void Close()
        {
            if (_closed || _moving) return;
            StartCoroutine(CloseIE());
        }

        private IEnumerator OpenIE()
        {
            foreach (Item i in holder.items)
            {
                i.Hide(false);
            }
            _moving = true;
            animator.Play(openingAnimationName);
            openingStartedEvent.Invoke();
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            openingEvent.Invoke();
            _open = true;
            _closed = false;
            _moving = false;
            holder.SetTouch(true);
        }

        private IEnumerator CloseIE()
        {
            holder.SetTouch(false);
            _moving = true;
            closingEvent.Invoke();
            animator.Play(closingAnimationName);
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            closingFinishedEvent.Invoke();
            _open = false;
            _closed = true;
            _moving = false;
            foreach (Item i in holder.items)
            {
                i.Hide(true);
            }
        }
    }
}
