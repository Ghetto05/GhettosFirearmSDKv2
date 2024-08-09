using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace GhettosFirearmSDKv2.UI.GunViceV2
{
    public class UIAttachmentCategory : MonoBehaviour
    {
        public Button foldoutButton;
        public RectTransform foldoutContent;
        public RectTransform headerContent;
        public TextMeshProUGUI headerText;
        private bool _collapsed;
        [NonSerialized]
        public readonly List<UIAttachment> Attachments = new();

        private RectTransform T => GetComponent<RectTransform>();

        public void Setup(string title, ViceUI vice)
        {
            headerText.text = title;
            foldoutButton.onClick.AddListener(delegate { vice.SelectCategory(this); });
            foldoutButton.onClick.AddListener(delegate { vice.PlaySound(ViceUI.SoundTypes.Select); });
            Collapse();
        }

        private void UpdateLayout()
        {
            if (!_collapsed)
            {
                var spacing = 10f;
                var cellHeight = 300f;
                var cellCount = Attachments.Count / 7 + (Attachments.Count % 7 == 0 ? 0 : 1);
                var space = headerContent.rect.size.y + cellCount * spacing + cellCount * cellHeight;
                
                T.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, space);
            }
            else
            {
                T.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, headerContent.rect.size.y);
            }
            LayoutUpdate();
        }

        private void LayoutUpdate()
        {
            var layout = GetComponentInParent<VerticalLayoutGroup>();
            layout.enabled = false;
            layout.enabled = true;
        }

        public void Expand()
        {
            _collapsed = false;
            foldoutContent.gameObject.SetActive(true);
            UpdateLayout();
        }

        public void Collapse()
        {
            _collapsed = true;
            foldoutContent.gameObject.SetActive(false);
            UpdateLayout();
        }
    }
}