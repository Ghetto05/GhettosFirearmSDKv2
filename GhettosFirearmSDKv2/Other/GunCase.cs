using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
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
        private bool _open;
        private bool _moving;
        private GunCaseSaveData _data;
        private bool _frozen;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
            if (isStatic)
            {
                item.physicBody.isKinematic = true;
                foreach (var handle in freezeHandles)
                {
                    handle.SetTouch(false);
                    handle.SetTelekinesis(false);
                }
                foreach (var obj in nonStaticObjects)
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
                if (string.IsNullOrWhiteSpace(_data.Firearm))
                    return;

                Util.SpawnItem(_data.Firearm, $"[Gun case - Save {_data.Firearm}]", f =>
                {
                    f.physicBody.isKinematic = true;
                    f.SetOwner(Item.Owner.Player);
                    StartCoroutine(DelayedSnap(f));
                }, holder.transform.position - Vector3.down * 10, holder.transform.rotation, null, true, _data.FirearmData.CloneJson());
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
                _data.Firearm = "";
                _data.FirearmData = null;
            }
        }

        private void Holder_Snapped(Item f)
        {
            f.Hide(_closed);
            _data.Firearm = f.itemId;
            _data.FirearmData = f.contentCustomData.CloneJson();
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                if (freezeHandles.Contains(handle))
                {
                    _frozen = !_frozen;
                    item.DisallowDespawn = _frozen;
                }
                if (toggleHandles.Contains(handle))
                {
                    if (_open) Close();
                    else Open();
                }
            }
        }

        private void FixedUpdate()
        {
            var frozen = _frozen && !item.handlers.Any(x => !freezeHandles.Contains(x.grabbedHandle));
            item.physicBody.isKinematic = frozen;
        }

        public void Open()
        {
            if (_open || _moving) return;
            StartCoroutine(OpenIE());
        }

        public void Close()
        {
            if (_closed || _moving) return;
            StartCoroutine(CloseIE());
        }

        private IEnumerator OpenIE()
        {
            foreach (var i in holder.items)
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
            foreach (var i in holder.items)
            {
                i.Hide(true);
            }
        }
    }
}
