using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.Events;

namespace GhettosFirearmSDKv2
{
    public class GunCase : MonoBehaviour
    {
        public Item item;
        public Holder holder;
        [Space]
        public Animator animator;
        public string openingAnimationName;
        public string closingAnimationName;
        [Space]
        public UnityEvent openingEvent;
        public UnityEvent closingEvent;
        [Space]
        public List<Handle> freezeHandles;
        public List<Handle> toggleHandles;

        private bool closed = true;
        private bool open = false;
        private bool moving = false;
        private GunCaseSaveData data;

        void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            if (item.TryGetCustomData(out data))
            {
                Catalog.GetData<ItemData>(data.firearm).SpawnAsync(f =>
                {
                    f.physicBody.isKinematic = true;
                    StartCoroutine(DelayedSnap(f));
                }, holder.transform.position - (Vector3.down * 10), holder.transform.rotation, null, true, data.firearmData.CloneJson());
            }
            else
            {
                data = new GunCaseSaveData();
                item.AddCustomData(data);
            }
        }

        private IEnumerator DelayedSnap(Item f)
        {
            yield return new WaitForSeconds(0.5f);
            holder.Snap(f);
        }

        private void Holder_UnSnapped(Item f)
        {
            f.Hide(false);
            if (!f.isCulled)
            {
                data.firearm = "";
                data.firearmData = null;
            }
        }

        private void Holder_Snapped(Item f)
        {
            f.Hide(closed);
            data.firearm = f.itemId;
            data.firearmData = f.contentCustomData;
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                if (freezeHandles.Contains(handle))
                {
                    item.physicBody.isKinematic = !item.physicBody.isKinematic;
                    item.disallowDespawn = item.physicBody.isKinematic;
                }
                if (toggleHandles.Contains(handle))
                {
                    if (open) Close();
                    else Open();
                }
            }
        }

        [EasyButtons.Button]
        public void Open()
        {
            if (open || moving) return;
            StartCoroutine(OpenIE());
        }

        [EasyButtons.Button]
        public void Close()
        {
            if (closed || moving) return;
            StartCoroutine(CloseIE());
        }

        private IEnumerator OpenIE()
        {
            foreach (Item item in holder.items)
            {
                item.Hide(false);
            }
            moving = true;
            animator.Play(openingAnimationName);
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            openingEvent.Invoke();
            open = true;
            closed = false;
            moving = false;
            holder.SetTouch(true);
        }

        private IEnumerator CloseIE()
        {
            holder.SetTouch(false);
            moving = true;
            closingEvent.Invoke();
            animator.Play(closingAnimationName);
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            open = false;
            closed = true;
            moving = false;
            foreach (Item item in holder.items)
            {
                item.Hide(true);
            }
        }
    }
}
