using System;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace GhettosFirearmSDKv2.UI.GunViceV2
{
    public class UIAttachment : MonoBehaviour
    {
        [NonSerialized]
        public AttachmentData Data;
        public Image icon;
        public TextMeshProUGUI nameText;
        public Button selectButton;
        public GameObject selectionOutline;

        public void Setup(AttachmentData data, ViceUI vice)
        {
            nameText.text = data.DisplayName;
            Data = data.CloneJson();
            Catalog.LoadAssetAsync<Sprite>(data.IconAddress, t => { icon.sprite = t; }, "UI Attachment Icon Load");
            selectButton.onClick.AddListener(delegate { vice.SelectAttachment(this); });
            selectButton.onClick.AddListener(delegate { vice.PlaySound(ViceUI.SoundTypes.Interact); });
        }

        private void OnDestroy()
        {
            Catalog.ReleaseAsset(icon?.sprite);
        }
    }
}