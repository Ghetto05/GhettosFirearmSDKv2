using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ThunderRoad;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GhettosFirearmSDKv2
{
    public class ViceUI : MonoBehaviour
    {
        public bool AlwaysFrozen;

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

        private void Awake()
        {
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
            if (!AlwaysFrozen && item != null) item.OnHeldActionEvent += Item_OnHeldActionEvent;
            if (AlwaysFrozen && item != null)
            {
                item.rb.isKinematic = true;
                item.disallowDespawn = true;
            }
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (!AlwaysFrozen && action == Interactable.Action.UseStart || action == Interactable.Action.AlternateUseStart)
            {
                item.rb.isKinematic = !item.rb.isKinematic;
                item.disallowDespawn = item.rb.isKinematic;
            }
        }

        private void Holder_UnSnapped(Item item)
        {
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
            cat.GetChild(1).GetChild(0).GetComponent<Text>().text = id;
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
                    AddPoint(point, parentFirearm.icon, point.id);
                    if (point.currentAttachment != null) FromAttachment(point.currentAttachment);
                }
            }
        }

        private void FromAttachment(Attachment attachment)
        {
            foreach (AttachmentPoint point in attachment.attachmentPoints)
            {
                if (point.gameObject.activeInHierarchy)
                {
                    AddPoint(point, attachment.icon, point.id);
                    if (point.currentAttachment != null) FromAttachment(point.currentAttachment);
                }
            }
        }

        public void AddPoint(AttachmentPoint slot, Texture icon, string name)
        {
            GameObject obj = Instantiate(slotButtonPrefab);
            obj.transform.SetParent(slotContentReference);
            obj.transform.localScale = Vector3.one;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localPosition = Vector3.zero;
            obj.SetActive(true);

            ViceUIAttachmentSlot slotUIElement = obj.GetComponent<ViceUIAttachmentSlot>();
            slotUIElement.slotName.text = name;
            slotUIElement.image.texture = icon;
            if (slot.currentAttachment != null)
            {
                slotUIElement.selectedAttachmentBackground.enabled = true;
                slotUIElement.selectedAttachmentIcon.enabled = true;
                Catalog.LoadAssetAsync<Texture2D>(slot.currentAttachment.data.iconAddress, tex =>
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

            if (currentSlot != null && currentSlot.currentAttachment != null && attach.id.Equals(currentSlot.currentAttachment.data.id)) attach.rim.enabled = true;
            else attach.rim.enabled = false;
            PositionCategories();
        }

        public void SetCurrentSlot(AttachmentPoint point)
        {
            currentSlot = point;
            SetupAttachmentList(point.type, point.alternateTypes);
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
            if (data.type.Equals(type)) return true;
            foreach (string s in alternateTypes)
            {
                if (data.type.Equals(s)) return true;
            }
            return false;
        }

        IEnumerator delayedRefresh()
        {
            yield return new WaitForSeconds(0.3f);
            SetupFirearm();
        }

        public void SpawnAttachment(string id)
        {
            if (currentSlot.currentAttachment != null) currentSlot.currentAttachment.Detach();
            if (!id.Equals("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING")) Catalog.GetData<AttachmentData>(id).SpawnAndAttach(currentSlot);
            foreach (ViceUIAttachment att in attachmentButtons)
            {
                if (!att.id.Equals("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING") && id.Equals(att.id)) att.rim.enabled = true;
                else if (!att.id.Equals("NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING_NOTHING")) att.rim.enabled = false;
            }
            StartCoroutine(delayedRefresh());
        }
    }
}
