using System;
using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2.UI.GunViceV2;

public class ViceUI : MonoBehaviour
{
    public enum SoundTypes
    {
        Select,
        Interact
    }

    public Collider screenCollider;
    public RectTransform slotContent;
    public RectTransform slotTemplate;
    public RectTransform attachmentCategoryContent;
    public RectTransform attachmentCategoryTemplate;
    public RectTransform attachmentTemplate;
    public RectTransform railAttachmentContent;
    public RectTransform railAttachmentTemplate;
    public Holder holder;
    public Handle[] freezeHandles;
        
    private IAttachmentManager _currentManager;
    private UISlot _currentSlot;
    private UIRailAttachment _currentRailAttachment;

    private readonly List<UISlot> _slots = new();
    private readonly List<UIAttachmentCategory> _attachmentCategories = new();
    private readonly List<UIAttachment> _attachments = new();
    private readonly List<UIRailAttachment> _railAttachments = new();

    public Button removeAttachmentButton;
    public Button moveAttachmentForwardButton;
    public Button moveAttachmentRearwardButton;
    public Button saveAmmoItemButton;
    public TextMeshProUGUI saveAmmoItemButtonText;
    public TextMeshProUGUI slotDisplay;
    public RectTransform slotDisplayButton;

    public AudioSource selectSound;
    public AudioSource interactSound;

    private bool _allowSwitchingSlots = true;
    private Item _item;
    private bool _frozen;

    private void Start()
    {
        removeAttachmentButton.onClick.AddListener(RemoveAttachment);
        moveAttachmentForwardButton.onClick.AddListener(delegate { MoveAttachment(true); });
        moveAttachmentRearwardButton.onClick.AddListener(delegate { MoveAttachment(false); });
        saveAmmoItemButton.onClick.AddListener(delegate { SaveAmmoItem(); });
            
        removeAttachmentButton.onClick.AddListener(delegate { PlaySound(SoundTypes.Interact); });
        moveAttachmentForwardButton.onClick.AddListener(delegate { PlaySound(SoundTypes.Interact); });
        moveAttachmentRearwardButton.onClick.AddListener(delegate { PlaySound(SoundTypes.Interact); });
        saveAmmoItemButton.onClick.AddListener(delegate { PlaySound(SoundTypes.Interact); });
            
        holder.Snapped += HolderOnSnapped;
        holder.UnSnapped += HolderOnUnSnapped;

        _item = GetComponentInParent<Item>();
        if (_item)
            _item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
    }

    private void OnDestroy()
    {
        removeAttachmentButton.onClick.RemoveAllListeners();
        moveAttachmentForwardButton.onClick.RemoveAllListeners();
        moveAttachmentRearwardButton.onClick.RemoveAllListeners();
    }

    private void ItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
    {
        if (action == Interactable.Action.UseStart && freezeHandles.Contains(handle))
        {
            _frozen = !_item.DisallowDespawn;
            _item.DisallowDespawn = _frozen;
        }
    }

    private void FixedUpdate()
    {
        if (holder.items.Count > 0 && holder.items[0].TryGetComponent(out Firearm firearm))
        {
            firearm.ChangeTrigger(FirearmClicker.Trigger());
            firearm.triggerState = FirearmClicker.Trigger();
        }

        if (_item && !_item.holder && freezeHandles.Any())
            _item.physicBody.isKinematic = _frozen && !_item.handlers.Any();
    }

    private void HolderOnSnapped(Item item)
    {
        if (item.GetComponent<IAttachmentManager>() is not { } manager)
            return;
            
        screenCollider.enabled = true;
        GetComponent<Canvas>().enabled = true;
        SetupForManager(manager);
    }

    private void HolderOnUnSnapped(Item item)
    {
        screenCollider.enabled = false;
        GetComponent<Canvas>().enabled = false;
        Cleanup();
    }

    private void Cleanup()
    {
        foreach (var x in _slots.ToArray()) { x.selectButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _slots.Clear();
        foreach (var x in _attachmentCategories.ToArray()) { x.foldoutButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _attachmentCategories.Clear();
        foreach (var x in _attachments.ToArray()) { x.selectButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _attachments.Clear();
        foreach (var x in _railAttachments.ToArray()) { x.selectButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _railAttachments.Clear();

        _currentManager = null;
        _currentSlot = null;
        _currentRailAttachment = null;
    }

    private void RemoveAttachment()
    {
        if (!_currentSlot)
            return;

        if (_currentSlot.AttachmentPoint.usesRail && _currentRailAttachment && !_currentRailAttachment.IsNewButton)
        {
            if (_currentRailAttachment.CurrentAttachment.Data.RailLength == -1)
                AddRailAttachment(null);
            //UpdateSlots(_currentRailAttachment.CurrentAttachment, true);
            _currentRailAttachment.CurrentAttachment.Detach();
            _railAttachments.Remove(_currentRailAttachment);
            Destroy(_currentRailAttachment.gameObject);
            SelectRailAttachment(_railAttachments.FirstOrDefault());
        }
        else if (_currentSlot.AttachmentPoint.currentAttachments.Any())
        {
            if (_currentSlot.AttachmentPoint.currentAttachments.FirstOrDefault() is not { } attachment) return;
            //_currentSlot.SetAttachment(null);
            //UpdateSlots(attachment, true);
            attachment.Detach();
        }
        _attachments.ForEach(x => x.selectionOutline.SetActive(false));
        UpdateSlots(/*null, true*/);
    }

    private void MoveAttachment(bool forward)
    {
        if (_currentRailAttachment?.CurrentAttachment is not { } attachment || attachment.Data.RailLength < 0)
            return;
        _currentRailAttachment?.CurrentAttachment?.MoveOnRail(forward);
        UpdateSlotCounter();
    }

    private void UpdateSlotCounter()
    {
        slotDisplay.text = "Slot: " + _currentRailAttachment?.CurrentAttachment?.RailPosition;
    }

    private void UpdateSaveAmmoButton()
    {
        var f = GetSelectedFirearm();
        if (f)
        {
            saveAmmoItemButton.gameObject.SetActive(true);
            saveAmmoItemButtonText.text = f.GetType() == typeof(AttachmentFirearm) ? "Save held item as ammo\n(For current attachment)" : "Save held item as ammo\n(For firearm base)";
        }
        else
        {
            saveAmmoItemButton.gameObject.SetActive(false);
        }
    }

    public void SetupForManager(IAttachmentManager manager)
    {
        _currentManager = manager;
        SetupSlots();
    }
        
    private void SetupSlots()
    {
        foreach (var x in _slots.ToArray()) { x.selectButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _slots.Clear();
            
        foreach (var point in _currentManager.AttachmentPoints)
        {
            AddSlot(point, _currentManager.Item.data.iconAddress);
            point.currentAttachments.ForEach(x => AddSlotsFromAttachment(x));
        }
    }

    // private void UpdateSlots(Attachment attachment, bool remove)
    // {
    //     if (!attachment)
    //         return;
    //     
    //     if (remove)
    //     {
    //         List<Attachment> attachments = new() { attachment };
    //         foreach (var point in attachment.attachmentPoints)
    //         {
    //             GetAllAttachmentsRecurve(point, ref attachments);
    //         }
    //         var attachmentPoints = attachments.SelectMany(x => x.attachmentPoints).ToList();
    //         var toDelete = _slots.Where(x => attachmentPoints.Contains(x.AttachmentPoint)).ToList();
    //         foreach (var uiSlot in toDelete)
    //         {
    //             _slots.Remove(uiSlot);
    //             Destroy(uiSlot.gameObject);
    //         }
    //     }
    //     else
    //     {
    //         var tr = _slots.FirstOrDefault(x => x.AttachmentPoint == attachment.attachmentPoint)?.transform;
    //         var start = tr?.GetSiblingIndex();
    //         AddSlotsFromAttachment(attachment, start + 1);
    //     }
    // }

    private void UpdateSlots()
    {
        var currentSlot = _currentSlot.AttachmentPoint;
            
        foreach (var x in _slots.ToArray()) { x.selectButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _slots.Clear();
            
        foreach (var point in _currentManager.AttachmentPoints.Where(x => x.gameObject.activeInHierarchy))
        {
            AddSlot(point, _currentManager.Item.data.iconAddress);
            point.currentAttachments.ForEach(x => AddSlotsFromAttachment(x));
        }

        SelectSlot(_slots.FirstOrDefault(x => x.AttachmentPoint == currentSlot), true);
            
        UpdateSaveAmmoButton();
    }

    private void AddSlotsFromAttachment(Attachment attachment, int? startIndex = null)
    {
        if (attachment == null)
            return;
            
        var address = attachment.Data.IconAddress;
            
        foreach (var point in attachment.attachmentPoints)
        {
            AddSlot(point, address, startIndex);
            point.currentAttachments.ForEach(x => AddSlotsFromAttachment(x, startIndex + 1));
        }
    }

    private void AddSlot(AttachmentPoint attachmentPoint, string iconAddress, int? setIndex = null)
    {
        if (!attachmentPoint.gameObject.activeInHierarchy || !attachmentPoint.gameObject.activeSelf)
            return;
        var slot = Instantiate(slotTemplate, slotContent).GetComponent<UISlot>();
        slot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        slot.Setup(attachmentPoint, iconAddress, this);
        slot.gameObject.SetActive(true);
        if (setIndex != null)
            slot.transform.SetSiblingIndex(setIndex.Value);
        _slots.Add(slot);
    }

    public void SelectSlot(UISlot slot, bool fromUpdate)
    {
        if (!_allowSwitchingSlots)
            return;
            
        _currentSlot = slot;
        if (!fromUpdate)
        {
            SetupRailAttachmentList(slot);
            SetupAttachmentList(slot);
            SetButtonVisibility(slot.AttachmentPoint.usesRail);
        }
            
        UpdateSaveAmmoButton();
    }

    private void SetButtonVisibility(bool visible)
    {
        moveAttachmentForwardButton.gameObject.SetActive(visible);
        moveAttachmentRearwardButton.gameObject.SetActive(visible);
        slotDisplayButton.gameObject.SetActive(visible);
    }

    public void SelectCategory(UIAttachmentCategory category)
    {
        var cs = _attachmentCategories.ToList();
        cs.Remove(category);
        cs.ForEach(x => x.Collapse());
        category.Expand();
    }

    public void SelectAttachment(UIAttachment attachment)
    {
        _allowSwitchingSlots = false;
        var a = _attachments.ToList();
        a.Remove(attachment);
        a.ForEach(x => x.selectionOutline.SetActive(false));
        attachment.selectionOutline.SetActive(true);

        try
        {
            if (_currentSlot.AttachmentPoint.usesRail)
            {
                if (_currentRailAttachment.IsNewButton)
                {
                    attachment.Data.SpawnAndAttach(_currentSlot.AttachmentPoint, newAttachment =>
                    {
                        _currentRailAttachment.Convert(attachment.Data, newAttachment);
                        PostAttachmentSpawnCallback();
                    });
                    if (attachment.Data.RailLength != -1)
                        AddRailAttachment(null);
                }
                else
                {
                    UpdateSlots(/*_currentSlot.AttachmentPoint.currentAttachments.FirstOrDefault(), true*/);

                    var railPos = _currentRailAttachment.CurrentAttachment.RailPosition;
                    _currentRailAttachment.CurrentAttachment.Detach();
                    attachment.Data.SpawnAndAttach(_currentSlot.AttachmentPoint, newAttachment =>
                    {
                        _currentRailAttachment.Convert(attachment.Data, newAttachment);
                        PostAttachmentSpawnCallback();
                    }, railPos);
                }
            }
            else
            {
                UpdateSlots(/*_currentSlot.AttachmentPoint.currentAttachments.FirstOrDefault(), true*/);

                _currentSlot.AttachmentPoint.currentAttachments.FirstOrDefault()?.Detach();
                attachment.Data.SpawnAndAttach(_currentSlot.AttachmentPoint, _ => { PostAttachmentSpawnCallback(); });
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            _allowSwitchingSlots = true;
        }
    }

    private void PostAttachmentSpawnCallback()
    {
        _allowSwitchingSlots = true;
        //_currentSlot.SetAttachment(attachment);
        UpdateSlots(/*attachment, false*/);
        UpdateSlotCounter();
    }

    public void SelectRailAttachment(UIRailAttachment attachment)
    {
        if (!_allowSwitchingSlots)
            return;
            
        _currentRailAttachment = attachment;
        UpdateSlotCounter();
            
        _attachments.ForEach(x => x.selectionOutline.SetActive(false));
        _attachments.FirstOrDefault(x => x.Data.id == _currentRailAttachment?.CurrentAttachment?.Data.id)?.selectionOutline.SetActive(true);
            
        var attachments = _railAttachments.ToList();
        attachments.Remove(attachment);
        attachments.ForEach(x => x.selectionOutline.gameObject.SetActive(false));
        attachment.selectionOutline.gameObject.SetActive(true);
            
        UpdateSaveAmmoButton();
    }

    private void SetupAttachmentList(UISlot slot)
    {
        foreach (var x in _attachmentCategories.ToArray()) { x.foldoutButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _attachmentCategories.Clear();
        foreach (var x in _attachments.ToArray()) { x.selectButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _attachments.Clear();

        var data = slot.AttachmentPoint.usesRail ?
            AttachmentData.AllOfType(slot.AttachmentPoint.railType, slot.AttachmentPoint.alternateTypes).Where(x => x.RailLength <= slot.AttachmentPoint.railSlots.Count).ToList() :
            AttachmentData.AllOfType(slot.AttachmentPoint.type, slot.AttachmentPoint.alternateTypes).ToList();
        data.ForEach(AddAttachment);

        if (_attachmentCategories.FirstOrDefault(x => x.headerText.text.Equals("Default")) is { } category)
        {
            category.transform.SetAsFirstSibling();
            _attachmentCategories.Remove(category);
            _attachmentCategories.Insert(0, category);
        }
        _attachmentCategories.FirstOrDefault()?.Expand();
    }

    private void SetupRailAttachmentList(UISlot slot)
    {
        foreach (var x in _railAttachments.ToArray()) { x.selectButton.onClick.RemoveAllListeners(); Destroy(x.gameObject); }
        _railAttachments.Clear();
            
        if (!slot.AttachmentPoint.usesRail)
            return;

        var addNewButton = true;
        foreach (var attachment in slot.AttachmentPoint.currentAttachments)
        {
            if (attachment.Data.RailLength == -1)
                addNewButton = false;
            AddRailAttachment(attachment);
        }
        if (addNewButton)
            AddRailAttachment(null);
            
        SelectRailAttachment(_railAttachments.First());
    }

    private void AddAttachment(AttachmentData data)
    {
        var category = GetOrAddCategory(data.CategoryName);
        var attachment = Instantiate(attachmentTemplate, category.foldoutContent).GetComponent<UIAttachment>();
        category.Attachments.Add(attachment);
        attachment.gameObject.SetActive(true);
        attachment.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        attachment.Setup(data, this);
        _attachments.Add(attachment);
        if (_currentSlot.AttachmentPoint.currentAttachments.Any(x => x.Data.id == data.id && (!_currentSlot.AttachmentPoint.usesRail || _currentRailAttachment.CurrentAttachment.Data.id.Equals(data.id))))
        {
            attachment.selectionOutline.SetActive(true);
        }
    }

    private void AddRailAttachment(Attachment attachment)
    {
        var a = Instantiate(railAttachmentTemplate, railAttachmentContent).GetComponent<UIRailAttachment>();
        a.gameObject.SetActive(true);
        a.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        a.Setup(attachment, this);
        _railAttachments.Add(a);
    }

    private UIAttachmentCategory GetOrAddCategory(string category)
    {
        var c = _attachmentCategories.FirstOrDefault(x => x.headerText.text.Equals(category));
        if (c)
            return c;
            
        c = Instantiate(attachmentCategoryTemplate, attachmentCategoryContent).GetComponent<UIAttachmentCategory>();
        c.gameObject.SetActive(true);
        c.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        c.Setup(category, this);
        _attachmentCategories.Add(c);
        return c;
    }

    public void SaveAmmoItem()
    {
        var heldHandleDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.grabbedHandle;
        var heldHandleNonDominant = Player.local.GetHand(Handle.dominantHand).ragdollHand.otherHand.grabbedHandle;
        var item = heldHandleDominant?.item ?? heldHandleNonDominant?.item;

        var firearm = GetSelectedFirearm();

        firearm.SetSavedAmmoItem(item?.data.id, item?.contentCustomData.CloneJson().ToArray());
    }

    private FirearmBase GetSelectedFirearm()
    {
        FirearmBase firearm = null;

        if (_currentManager is Firearm f)
            firearm = f;

        if (_currentSlot?.AttachmentPoint?.usesRail != true)
        {
            if (_currentSlot?.AttachmentPoint?.currentAttachments.FirstOrDefault()?.GetComponent<AttachmentFirearm>() is { } attachmentFirearm)
            {
                firearm = attachmentFirearm;
            }
        }
        else
        {
            if (_currentRailAttachment?.CurrentAttachment?.GetComponent<AttachmentFirearm>() is { } attachmentFirearm)
            {
                firearm = attachmentFirearm;
            }
        }

        return firearm;
    }

    public void PlaySound(SoundTypes soundType)
    {
        switch (soundType)
        {
            case SoundTypes.Interact:
                if (interactSound)
                    interactSound.Play();
                break;
            case SoundTypes.Select:
                if (selectSound)
                    selectSound.Play();
                break;
        }
    }
}