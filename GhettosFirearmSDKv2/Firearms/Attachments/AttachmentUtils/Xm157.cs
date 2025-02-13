using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2
{
    [ExecuteInEditMode]
    public class Xm157 : MonoBehaviour
    {
        public enum Pages
        {
            MainMenu,
            UiColorSelection
        }

        public enum UiColors
        {
            Yellow,
            Red,
            Orange
        }

        private SaveNodeValueBool _compassSaveData;
        private SaveNodeValueBool _rangeFinderSaveData;
        private SaveNodeValueBool _visualLaserSaveData;
        private SaveNodeValueInt _colorSaveData;

        public Scope scope;
        public float distanceToCamera;
        public Laser rangeFinder;
        public Laser visualLaser;
        public Transform scaleRoot;
        public Handle menuHandle;
        public AudioSource buttonSound;
        
        public int monospaceSize = 100;
        public TextMeshProUGUI ui;
        public Pages currentPage;
        public int currentOption;

        public bool compassEnabled;
        public bool rangeFinderEnabled;
        public UiColors uiColor;
        
        public TextMeshProUGUI rangeFinderDisplay;
        public TextMeshProUGUI compassDisplay;
        public Graphic redDot;

        public Graphic[] colorElements;

        private int _currentRowCount;
        private bool _grabbed;
        private bool _lastNonUiState = true;
        private bool _lastRedDotState = true;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime);
        }

        private void InvokedStart()
        {
            _compassSaveData = scope.connectedAttachment.Node.GetOrAddValue("XM157_Compass", new SaveNodeValueBool { Value = true });
            _rangeFinderSaveData = scope.connectedAttachment.Node.GetOrAddValue("XM157_RangeFinder", new SaveNodeValueBool { Value = true });
            _visualLaserSaveData = scope.connectedAttachment.Node.GetOrAddValue("XM157_VisualLaser", new SaveNodeValueBool { Value = false });
            _colorSaveData = scope.connectedAttachment.Node.GetOrAddValue("XM157_Color", new SaveNodeValueInt { Value = (int)UiColors.Yellow });

            compassEnabled = _compassSaveData.Value;
            rangeFinderEnabled = _rangeFinderSaveData.Value;
            visualLaser.physicalSwitch = _visualLaserSaveData.Value;
            uiColor = (UiColors)_colorSaveData.Value;
            ApplyColor();
            scope.connectedAttachment.OnHeldActionEvent += ConnectedAttachmentOnOnHeldActionEvent;
            scope.connectedAttachment.attachmentPoint.parentManager.item.OnGrabEvent += MenuHandleOnGrabbed;
        }

        private void ConnectedAttachmentOnOnHeldActionEvent(RagdollHand hand, Handle handle, Interactable.Action action)
        {
            if (handle != menuHandle)
                return;
            
            if (action == Interactable.Action.Ungrab)
                UnGrab();
            if (action == Interactable.Action.UseStart)
                Trigger();
            if (action == Interactable.Action.AlternateUseStart)
                AlternateUse();
        }

        private void OnDestroy()
        {
            if (menuHandle == null)
                return;
            scope.connectedAttachment.attachmentPoint.parentManager.item.OnGrabEvent -= MenuHandleOnGrabbed;
            scope.connectedAttachment.OnHeldActionEvent -= ConnectedAttachmentOnOnHeldActionEvent;
        }

        private void MenuHandleOnGrabbed(Handle handle, RagdollHand hand)
        {
            if (handle == menuHandle)
                Grab();
        }

        public void Grab()
        {
            ui.enabled = true;
            _grabbed = true;
            DrawPage();
        }

        public void UnGrab()
        {
            ui.enabled = false;
            _grabbed = false;
        }

        public void AlternateUse()
        {
            //cycle through options
            if (buttonSound != null)
                buttonSound.Play();
            currentOption++;
            if (currentOption >= _currentRowCount)
                currentOption = 0;
            DrawPage();
        }

        public void Trigger()
        {
            //confirm options
            if (buttonSound != null)
                buttonSound.Play();
            ApplyOption();
            DrawPage();
        }

        private void DrawPage()
        {
            GetPageInfo(out var text, out var rowCount);
            _currentRowCount = rowCount;
            var vars = PopulateArray(rowCount, " ");
            vars[currentOption] = ">";
            ui.text = string.Format($"<mspace=mspace={monospaceSize}>" + text + "</mspace>", vars);
            ui.enabled = false;
            ui.enabled = true;
        }

        private void GetPageInfo(out string text, out int rowCount)
        {
            switch (currentPage)
            {
                case Pages.MainMenu:
                    text = "{0} Compass (" + (compassEnabled ? "On" : "Off") + ")<br>" +
                           "{1} Range finder (" + (rangeFinderEnabled ? "On" : "Off") + ")<br>" +
                           "{2} Visual laser (" + (visualLaser == null ? "[ERROR]" : visualLaser.physicalSwitch ? "On" : "Off") + ")<br>" +
                           "{3} HUD color (" + uiColor + ")";
                    rowCount = 4;
                    break;
                case Pages.UiColorSelection:
                    text = "{0} Yellow<br>" +
                           "{1} Red<br>" +
                           "{2} Orange";
                    rowCount = 3;
                    break;
                default:
                    text = "[ERROR]";
                    rowCount = 0;
                    break;
            }
        }

        private void ApplyOption()
        {
            switch (currentPage)
            {
                case Pages.MainMenu:
                    switch (currentOption)
                    {
                        case 0:
                            ToggleCompass();
                            break;
                        case 1:
                            ToggleRangeFinder();
                            break;
                        case 2:
                            ToggleVisualLaser();
                            break;
                        case 3:
                            currentPage = Pages.UiColorSelection;
                            currentOption = 0;
                            break;
                    }
                    break;
                case Pages.UiColorSelection:
                    uiColor = currentOption switch
                    {
                        0 => UiColors.Yellow,
                        1 => UiColors.Red,
                        2 => UiColors.Orange,
                        _ => uiColor
                    };
                    ApplyColor();
                    currentPage = Pages.MainMenu;
                    break;
            }
        }

        private object[] PopulateArray(int count, string defaultValue)
        {
            var output = new object[count];
            for (var i = 0; i < output.Length; i++)
                output[i] = defaultValue;
            return output;
        }

        private void Update()
        {
            UpdateScale();
            ToggleNonUiComponents();
            ToggleRedDot();
            UpdateCompass();
            if (rangeFinder != null)
                rangeFinderDisplay.SetText(rangeFinder.lastHitDistance.ToString("0.00") + "m"); 
        }

        #region Options

        private void ToggleCompass()
        {
            compassEnabled = !compassEnabled;
            SaveOptions();
        }

        private void ToggleRangeFinder()
        {
            rangeFinderEnabled = !rangeFinderEnabled;
            SaveOptions();
        }

        private void ToggleVisualLaser()
        {
            if (visualLaser != null)
                visualLaser.physicalSwitch = !visualLaser.physicalSwitch;
            SaveOptions();
        }

        private void ApplyColor()
        {
            foreach (var element in colorElements)
            {
                element.color = 
                    uiColor switch
                    {
                        UiColors.Orange => new Color(1, 0.29f, 0),
                        UiColors.Red => Color.red,
                        UiColors.Yellow => Color.yellow,
                        _ => Color.black
                    };
            }
            SaveOptions();
        }

        private void ToggleNonUiComponents()
        {
            var a = !_grabbed && (scope == null || scope.currentIndex != 0);
            if (_lastNonUiState != a)
            {
                compassDisplay.enabled = a && compassEnabled;
                rangeFinderDisplay.enabled = a && rangeFinderEnabled;
            }
            _lastNonUiState = a;
        }

        private void ToggleRedDot()
        {
            var a = !_grabbed && (scope == null || scope.currentIndex == 0);
            if (_lastRedDotState != a)
            {
                redDot.enabled = a;
            }
            _lastRedDotState = a;
        }

        private void UpdateCompass()
        {
            var angle = transform.eulerAngles.y;
            compassDisplay.text = Heading(angle) + angle.ToString("0.00") + "°";
        }

        private void UpdateScale()
        {
            scaleRoot.localScale = Vector3.one * 2.0f * distanceToCamera * Mathf.Tan(scope.cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        private string Heading(float angle)
        {
            return angle switch
            {
                > 22.5f and <= 67.5f => "NE ",
                > 67.5f and <= 112.5f => "E ",
                > 112.5f and <= 157.5f => "SE ",
                > 157.5f and <= 202.5f => "S ",
                > 202.5f and <= 247.5f => "SW" ,
                > 247.5f and <= 292.5f => "W ",
                > 292.5f and <= 337.5f => "NW ",
                _ => "N "
            };
        }

        private void SaveOptions()
        {
            _compassSaveData.Value = compassEnabled;
            _rangeFinderSaveData.Value = rangeFinderEnabled;
            _visualLaserSaveData.Value = visualLaser.physicalSwitch;
            _colorSaveData.Value = (int)uiColor;
        }

        #endregion
    }
}
