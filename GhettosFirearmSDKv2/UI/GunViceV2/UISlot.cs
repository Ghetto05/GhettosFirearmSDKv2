using System;
using System.Linq;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace GhettosFirearmSDKv2.UI.GunViceV2
{
    public class UISlot : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public Image icon;
        public Button selectButton;
        public Image attachmentIcon;
        [NonSerialized]
        public AttachmentPoint AttachmentPoint;

        private string _iconAddress;

        public void Setup(AttachmentPoint attachmentPoint, string iconAddress, ViceUI vice)
        {
            _iconAddress = iconAddress;
            icon.sprite = null;
            AttachmentPoint = attachmentPoint;
            nameText.text = attachmentPoint.id;
            Catalog.LoadAssetAsync<Sprite>(_iconAddress, t => { icon.sprite = t; }, "UI Slot icon load");
            selectButton.onClick.AddListener(delegate { vice.SelectSlot(this, false); });
            selectButton.onClick.AddListener(delegate { vice.PlaySound(ViceUI.SoundTypes.Select); });

            if (AttachmentPoint.currentAttachments.FirstOrDefault() is { } attachment)
                SetAttachment(attachment);

            Invoke(nameof(RetrySettingIcon), 1f);
        }

        private void RetrySettingIcon()
        {
            if (!icon.sprite)
                return;

            Catalog.LoadAssetAsync<Sprite>(_iconAddress, t => { icon.sprite = t; }, "UI Slot icon retry load");
        }

        public void SetAttachment(Attachment attachment)
        {
            if (attachmentIcon.sprite)
            {
                Catalog.ReleaseAsset(icon.sprite);
            }
            attachmentIcon.transform.parent.gameObject.SetActive(attachment);

            if (!attachment)
                return;
            
            Catalog.LoadAssetAsync<Sprite>(attachment.Data.IconAddress, t => { attachmentIcon.sprite = t; }, "UI Slot attachment icon Load");
        }

        private void OnDestroy()
        {
            Catalog.ReleaseAsset(icon.sprite);
            if (attachmentIcon.sprite)
            {
                Catalog.ReleaseAsset(icon.sprite);
            }
        }
    }
}