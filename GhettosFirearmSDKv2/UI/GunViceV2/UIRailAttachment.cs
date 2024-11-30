using System;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace GhettosFirearmSDKv2.UI.GunViceV2
{
    public class UIRailAttachment : MonoBehaviour
    {
        public Button selectButton;
        public Image icon;
        public RectTransform selectionOutline;
        [NonSerialized]
        public Attachment CurrentAttachment;
        [NonSerialized]
        public bool IsNewButton;
        private bool _loadedIconFromAddressable;

        public void Setup(Attachment attachment, ViceUI vice)
        {
            CurrentAttachment = attachment;
            IsNewButton = !attachment;
            selectButton.onClick.AddListener(delegate { vice.SelectRailAttachment(this); });
            selectButton.onClick.AddListener(delegate { vice.PlaySound(ViceUI.SoundTypes.Select); });
            if (attachment?.Data?.IconAddress?.IsNullOrEmptyOrWhitespace() ?? true)
                return;
            Catalog.LoadAssetAsync<Sprite>(attachment.Data.IconAddress, t => { _loadedIconFromAddressable = true; icon.sprite = t; }, "UI Rail Attachment Icon");
        }

        public void Convert(AttachmentData data, Attachment attachment)
        {
            CurrentAttachment = attachment;
            IsNewButton = false;
            if (data.IconAddress.IsNullOrEmptyOrWhitespace())
                return;
            Catalog.LoadAssetAsync<Sprite>(data.IconAddress, t =>
            {
                _loadedIconFromAddressable = true;
                icon.sprite = t;
            }, "UI Rail Attachment Icon");
        }

        private void OnDestroy()
        {
            if (!_loadedIconFromAddressable)
                return;
            Catalog.ReleaseAsset(icon.sprite);
        }
    }
}