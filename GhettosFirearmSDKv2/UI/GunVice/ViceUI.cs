using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

namespace GhettosFirearmSDKv2
{
    public class ViceUI : MonoBehaviour
    {
        public bool AlwaysFrozen;
        public bool allowFreeze = true;

        public Item item;

        public Collider screenCollider;

        public Canvas canvas;
        public AttachmentPoint currentSlot;
        public AttachmentPoint lastSlot = null;
        public Holder holder;

        public Transform slotContentReference;
        public GameObject slotButtonPrefab;
        public List<ViceUIAttachmentSlot> slotButtons;

        public Transform categoriesContentReference;
        public GameObject categoryButtonPrefab;
        public List<Transform> categories;

        public List<ViceUIAttachment> attachmentButtons;
        public GameObject attachmentButtonPrefab;

        public float categoryElementHeight;
        public float categroyHeaderHeight;
        public float categoryGapHeight;

        private bool _triggeredLastFrame;

        private void Awake()
        {
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            if (!AlwaysFrozen && allowFreeze && item != null) item.OnHeldActionEvent += Item_OnHeldActionEvent;
            if (AlwaysFrozen && item != null)
            {
                item.physicBody.isKinematic = true;
                item.DisallowDespawn = true;
            }
        }

        private void FixedUpdate()
        {
            if (holder.items.Count > 0 && holder.items[0].TryGetComponent(out Firearm firearm))
            {
                firearm.ChangeTrigger(FirearmClicker.Trigger());
                firearm.triggerState = FirearmClicker.Trigger();
                _triggeredLastFrame = FirearmClicker.Trigger();
            }
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (!AlwaysFrozen && item != null && action == Interactable.Action.UseStart || action == Interactable.Action.AlternateUseStart)
            {
                item.physicBody.isKinematic = !item.physicBody.isKinematic;
                item.DisallowDespawn = item.physicBody.isKinematic;
            }
        }

        private void Holder_UnSnapped(Item item)
        {
            if (item.TryGetComponent(out Firearm firearm))
            {
                firearm.triggerState = false;
            }
            canvas.enabled = false;
            screenCollider.enabled = false;
            currentSlot = null;
            foreach (ViceUIAttachmentSlot button in slotButtons)
            {
                Destroy(button.gameObject);
            }
            slotButtons.Clear();
            foreach (ViceUIAttachment button in attachmentButtons)
            {
                Destroy(button.gameObject);
            }
            attachmentButtons.Clear();
            foreach (Transform button in categories)
            {
                Destroy(button.gameObject);
            }
            categories.Clear();
        }

        private Transform GetOrAddCategory(string id)
        {
            foreach (Transform t in categories)
            {
                if (t.name.Equals(id)) return t;
            }
            Transform cat = Instantiate(categoryButtonPrefab).transform;
            cat.SetParent(categoriesContentReference);
            cat.localScale = Vector3.one;
            cat.localEulerAngles = Vector3.zero;
            cat.localPosition = Vector3.zero;
            cat.gameObject.SetActive(true);
            cat.name = id;
            cat.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = id;
            if (id.Equals("Default"))
            {
                cat.SetAsFirstSibling();
                categories.Insert(0, cat);
            }
            else categories.Add(cat);
            return cat;
        }

        private void Holder_Snapped(Item item)
        {
            SetupFirearm();
        }

        [EasyButtons.Button]
        public void SetupFirearm()
        {
            if (holder.items.Count > 0)
            {
                if (holder.items[0].TryGetComponent(out Firearm firearm) && firearm.attachmentPoints.Count > 0)
                {
                    canvas.enabled = true;
                    screenCollider.enabled = true;

                    if (slotButtons != null)
                    {
                        foreach (ViceUIAttachmentSlot button in slotButtons)
                        {
                            Destroy(button.gameObject);
                        }
                        slotButtons.Clear();
                    }

                    AddAttachmentSlots(firearm);
                }
            }
        }

        private void AddAttachmentSlots(Firearm parentFirearm)
        {
            foreach (AttachmentPoint point in parentFirearm.attachmentPoints)
            {
                if (point.gameObject.activeInHierarchy)
                {
                    AddPoint(point, parentFirearm.item.data?.iconAddress, point.id);
                    point.currentAttachments.ForEach(AddSlotsFromAttachment);
                }
            }
        }

        private void AddSlotsFromAttachment(Attachment attachment)
        {
            foreach (AttachmentPoint point in attachment.attachmentPoints)
            {
                if (point.gameObject.activeInHierarchy)
                {
                    AddPoint(point, attachment.Data.iconAddress, point.id);
                    point.currentAttachments.ForEach(AddSlotsFromAttachment);
                }
            }
        }

        private void AddPoint(AttachmentPoint slot, string iconAddress, string name)
        {
            GameObject obj = Instantiate(slotButtonPrefab, slotContentReference, true);
            obj.transform.localScale = Vector3.one;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localPosition = Vector3.zero;
            obj.SetActive(true);

            ViceUIAttachmentSlot slotUIElement = obj.GetComponent<ViceUIAttachmentSlot>();
            Catalog.LoadAssetAsync<Texture2D>(iconAddress, t =>
            {
                slotUIElement.image.texture = t;
            }, "Vice UI Attachment Point Icon Loader");
            slotUIElement.slotName.text = name;
            if (slot.currentAttachments.Any())
            {
                slotUIElement.selectedAttachmentBackground.enabled = true;
                slotUIElement.selectedAttachmentIcon.enabled = true;
                Catalog.LoadAssetAsync<Texture2D>(slot.currentAttachments.First().Data.iconAddress, tex =>
                {
                    slotUIElement.selectedAttachmentIcon.texture = tex;
                }, "ViceUI");
            }
            else
            {
                slotUIElement.selectedAttachmentBackground.enabled = false;
                slotUIElement.selectedAttachmentIcon.enabled = false;
            }
            //Debug.Log("Is texture of " + slot.parentFirearm + " null? " + icon);
            slotUIElement.button.onClick.AddListener(delegate { SetCurrentSlot(slot); });
            slotButtons.Add(slotUIElement);
        }

        public void AddAttachment(string id, Texture2D icon, string name, string category)
        {
            GameObject obj = Instantiate(attachmentButtonPrefab);
            obj.transform.SetParent(GetOrAddCategory(category).GetChild(0));
            obj.transform.localScale = Vector3.one;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localPosition = Vector3.zero;
            obj.SetActive(true);

            ViceUIAttachment attach = obj.GetComponent<ViceUIAttachment>();
            attach.attachmentName.text = name;
            attach.id = id;
            attach.button.onClick.AddListener(delegate { SpawnAttachment(id); });
            attachmentButtons.Add(attach);

            if (id.Equals("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING"))
            {
                obj.transform.SetAsFirstSibling();
                attach.background.enabled = false;
                attach.icon.color = Color.red;
            }
            else attach.icon.texture = icon;

            if (currentSlot != null && currentSlot.currentAttachments.Any() && attach.id.Equals(currentSlot.currentAttachments.First().Data.id))
                attach.rim.enabled = true;
            else
                attach.rim.enabled = false;
            PositionCategories();
        }

        public void SetCurrentSlot(AttachmentPoint point)
        {
            currentSlot = point;
            if (!point.usesRail)
                SetupAttachmentList(point.type, point.alternateTypes);
            else
                SetupAttachmentList(point.railType, null);
        }

        public void PositionCategories()
        {
            foreach (Transform cat in categories)
            {
                if (categories.IndexOf(cat) == 0) cat.localPosition = new Vector3(0, 0, 0);
                else
                {
                    int indexOfThis = categories.IndexOf(cat);

                    int previousHeaders = 0;
                    foreach (Transform prevCat in categories)
                    {
                        if (categories.IndexOf(prevCat) < indexOfThis)
                        {
                            previousHeaders++;
                        }
                    }
                    int previousGaps = previousHeaders;
                    int previousRows = previousHeaders;
                    foreach (Transform prevCat in categories)
                    {
                        if (categories.IndexOf(prevCat) < indexOfThis)
                        {
                            previousRows += (prevCat.GetChild(0).childCount - 1) / 6;
                            previousGaps += (prevCat.GetChild(0).childCount - 1) / 6;
                            previousGaps++;
                            if ((prevCat.GetChild(0).childCount - 1) % 6 == 0)
                            {
                                previousRows--;
                                previousGaps--;
                            }
                        }
                    }
                    float newY = -previousRows * categoryElementHeight - previousHeaders * categroyHeaderHeight - previousGaps * categoryGapHeight;

                    cat.localPosition = new Vector3(0, newY, 0);
                }
            }

            RectTransform trans = (RectTransform)categoriesContentReference;
            int headers = 0;
            foreach (Transform prevCat in categories)
            {
                headers++;
            }
            int gaps = headers;
            int rows = headers;
            foreach (Transform prevCat in categories)
            {
                rows += (prevCat.GetChild(0).childCount - 1) / 6;
                gaps += (prevCat.GetChild(0).childCount - 1) / 6;
                gaps++;
                if ((prevCat.GetChild(0).childCount - 1) % 6 == 0)
                {
                    rows--;
                    gaps--;
                }
            }
            float Y = rows * categoryElementHeight + headers * categroyHeaderHeight + gaps * categoryGapHeight;
            trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Y);
        }

        private void SetupAttachmentList(string attachmentType, List<string> alternateTypes)
        {
            foreach (ViceUIAttachment button in attachmentButtons)
            {
                Destroy(button.gameObject);
            }
            attachmentButtons.Clear();
            foreach (Transform button in categories)
            {
                Destroy(button.gameObject);
            }
            categories.Clear();

            List<AttachmentData> datas = new List<AttachmentData>();
            foreach (AttachmentData data in Catalog.GetDataList<AttachmentData>())
            {
                if (TypeMatch(attachmentType, alternateTypes, data))
                {
                    datas.Add(data);
                }
            }

            AddAttachment("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING", null, "Nothing", "Default");
            foreach (AttachmentData data in datas)
            {
                Catalog.LoadAssetAsync<Texture2D>(data.iconAddress, tex => 
                {
                    AddAttachment(data.id, tex, data.displayName, data.GetID());
                }, "ViceUI");
            }
        }

        private bool TypeMatch(string type, List<string> alternateTypes, AttachmentData data)
        {
            if (data.type.Equals(type))
                return true;
            if (alternateTypes == null || !alternateTypes.Any())
                return false;
            foreach (string s in alternateTypes)
            {
                if (data.type.Equals(s))
                    return true;
            }
            return false;
        }

        IEnumerator DelayedRefresh()
        {
            yield return new WaitForSeconds(0.3f);
            SetupFirearm();
        }

        public void SpawnAttachment(string id)
        {
            currentSlot.parentFirearm.item.lastInteractionTime = Time.time;
            for (int i = 0; i < currentSlot.currentAttachments.Count; i++)
            {
                currentSlot.currentAttachments[0].Detach();
            }
            
            if (!id.Equals("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING"))
                Catalog.GetData<AttachmentData>(Util.GetSubstituteId(id, "Vice UI")).SpawnAndAttach(currentSlot);
            foreach (ViceUIAttachment att in attachmentButtons)
            {
                if (!att.id.Equals("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING") && id.Equals(att.id))
                    att.rim.enabled = true;
                else if (!att.id.Equals("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING"))
                    att.rim.enabled = false;
            }
            StartCoroutine(DelayedRefresh());
        }

        public void MoveAttachment(bool forwards)
        {
            if (currentSlot != null && currentSlot.currentAttachments.Any())
                currentSlot.currentAttachments.First().MoveOnRail(forwards);
        }
    }
}
