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

        public void Setup(AttachmentPoint attachmentPoint, string iconAddress, ViceUI vice)
        {
            AttachmentPoint = attachmentPoint;
            nameText.text = attachmentPoint.id;
            Catalog.LoadAssetAsync<Sprite>(iconAddress, t => { icon.sprite = t; }, "UI Slot icon Load");
            selectButton.onClick.AddListener(delegate { vice.SelectSlot(this); });
            selectButton.onClick.AddListener(delegate { vice.PlaySound(ViceUI.SoundTypes.Select); });
            
            if (AttachmentPoint.currentAttachments.FirstOrDefault() is { } attachment)
                SetAttachment(attachment);
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
            
            Catalog.LoadAssetAsync<Sprite>(attachment.Data.iconAddress, t => { attachmentIcon.sprite = t; }, "UI Slot attachment icon Load");
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