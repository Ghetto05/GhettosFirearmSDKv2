using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2
{
    public class GunLockerUI : MonoBehaviour
    {
        public Transform categoryContent;
        public GameObject categoryPrefab;
        public List<string> categories;
        private List<GameObject> _categoryButtons;
        private string _currentCategory;

        public Transform savesContent;
        public GameObject savesPrefab;
        public List<string> saves;
        private List<GameObject> _saveButtons;

        public List<Button> keys;
        public List<Button> keysCaps;
        public List<Button> keysNonCaps;
        public TextMeshProUGUI typingField;
        public GameObject typingPanel;
        public Button cancelButton;
        public Button confirmButton;

        public Holder holder;

        private bool _typing;
        private string _typingName = "";
        public bool shift = true;
        public bool caps = true;
        private float _lastCursorShift; 

        private void Awake()
        {
            SetupCategoryList();

            foreach (var b in keys)
            {
                b.onClick.AddListener(delegate { Type(b.GetComponentInChildren<TextMeshProUGUI>().text); });
            }
            Cancel();

            cancelButton.onClick.AddListener(delegate { Cancel(); });
            confirmButton.onClick.AddListener(delegate { SaveWeaponWithName(_typingName); });
        }

        #region Typing
        public void Type(string key)
        {
            if (_typing)
            {
                if (_typingName.EndsWith("|")) _typingName = _typingName.Remove(_typingName.Length - 1, 1);
                if (_typingName.Equals("Type here...")) _typingName = "";
                if (key.Equals("BACKSPACE") && _typingName.Length > 0) _typingName = _typingName.Remove(_typingName.Length - 1);
                else if (key.Equals("SHIFT")) shift = !shift;
                else if (key.Equals("CAPS")) caps = !caps;
                else
                {
                    _typingName += key;
                    shift = false;
                }
                typingField.text = _typingName;
                UpdateShift();
            }
        }

        public void Update()
        {
            if (_typing && !_typingName.Equals("Type here...") && Time.time - _lastCursorShift > 0.6f)
            {
                _lastCursorShift = Time.time;
                if (_typingName.EndsWith("|"))
                {
                    _typingName = _typingName.Remove(_typingName.Length - 1, 1);
                }
                else
                {
                    _typingName += "|";
                }
                typingField.text = _typingName;
            }
        }

        public void UpdateShift()
        {
            foreach (var capsKey in keysCaps)
            {
                capsKey.gameObject.SetActive(shift || caps);
            }
            foreach (var nonCapsKey in keysNonCaps)
            {
                nonCapsKey.gameObject.SetActive(!shift && ! caps);
            }
        }

        public void OpenTypingPanel()
        {
            _typingName = "Type here...";
            typingField.text = _typingName;
            typingPanel.SetActive(true);
            _typing = true;
            shift = true;
            caps = false;
            UpdateShift();
        }

        public void Cancel()
        {
            _typingName = "Type here...";
            typingField.text = _typingName;
            typingPanel.SetActive(false);
            _typing = false;
        }
        #endregion

        #region Saving
        public void SaveWeapon()
        {
            if (holder.items.Count == 0 || _typing) return;
            OpenTypingPanel();
        }

        public void SaveWeaponWithName(string weaponName)
        {
            if (!_typing) return;
            if (holder.items.Count == 0)
            {
                Cancel();
                return;
            }

            if (weaponName.EndsWith("|")) weaponName = weaponName.Remove(weaponName.Length - 1, 1);
            var idString = weaponName.Replace(" ", "");
            idString = idString.Replace(",", "");
            idString = idString.Replace(".", "");
            idString = idString.Replace("-", "");
            idString = idString.Replace("_", "");
            idString = idString.Replace("/", "");
            idString = idString.Replace("#", "");
            idString = idString.Replace("&", "");
            idString = idString.Replace("|", "");
            var preb = Settings.saveAsPrebuilt;

            DeleteSave(idString);
            
            var newData = new GunLockerSaveData
                          {
                              id = preb ? "PREBUILT_" + idString : "SAVE_" + idString,
                              DisplayName = weaponName,
                              ItemId = holder.items[0].itemId,
                              Category = preb ? "Prebuilts" : holder.items[0].data.displayName,
                              DataList = holder.items[0].contentCustomData.CloneJson()
                          };

            Settings.CreateSaveFolder();
            var path = Settings.GetSaveFolderPath() + "\\Saves\\" + newData.id + ".json";
            var content = JsonConvert.SerializeObject(newData, Catalog.jsonSerializerSettings);
            Catalog.LoadJson(newData, content, path, Settings.SaveFolderName + "\\Saves");
            File.WriteAllText(path, content);
            SetCategory(_currentCategory);
            Cancel();
        }
        #endregion

        #region Locker Actions

        public void SetupCategoryList()
        {
            categories = GunLockerSaveData.GetAllCategories();
            categories.Sort();
            categories.Insert(0, "Prebuilts");

            if (_categoryButtons != null)
            {
                foreach (var obj in _categoryButtons)
                {
                    Destroy(obj);
                }
                _categoryButtons.Clear();
            }
            else _categoryButtons = new List<GameObject>();

            foreach (var cat in categories)
            {
                var buttonObj = Instantiate(categoryPrefab, categoryContent);
                buttonObj.SetActive(true);
                buttonObj.transform.localPosition = Vector3.zero;
                buttonObj.transform.localEulerAngles = Vector3.zero;
                var categoryComp = buttonObj.GetComponent<GunLockerUICategory>();
                categoryComp.button.onClick.AddListener(delegate { SetCategory(cat); });
                categoryComp.textName.text = cat;
                _categoryButtons.Add(buttonObj);
                if (_currentCategory != null && _currentCategory.Equals(cat))
                {
                    categoryComp.selectionOutline.SetActive(true);
                }
            }
        }

        private void SetCategory(string category)
        {
            _currentCategory = category;
            if (_saveButtons != null)
            {
                foreach (var obj in _saveButtons)
                {
                    Destroy(obj);
                }
                _saveButtons.Clear();
            }
            else _saveButtons = new List<GameObject>();

            foreach (var data in Catalog.GetDataList<GunLockerSaveData>().Where(i => i.Category.Equals(_currentCategory)))
            {
                var buttonObj = Instantiate(savesPrefab, savesContent);
                buttonObj.SetActive(true);
                buttonObj.transform.localPosition = Vector3.zero;
                buttonObj.transform.localEulerAngles = Vector3.zero;
                var saveComp = buttonObj.GetComponent<GunLockerUISave>();
                if (data.Category.Equals("Prebuilts")) saveComp.deleteButton.gameObject.SetActive(false);
                saveComp.button.onClick.AddListener(delegate { SpawnSave(data.id); });
                saveComp.deleteButton.onClick.AddListener(delegate { DeleteSave(data.id); });
                saveComp.textName.text = data.DisplayName;
                _saveButtons.Add(buttonObj);
            }
            SetupCategoryList();
        }

        private void SpawnSave(string saveId)
        {
            if (holder.items.Count > 0 && holder.UnSnapOne() is { } i) i.Despawn();
            var data = Catalog.GetData<GunLockerSaveData>(saveId);
            Util.SpawnItem(data.ItemId, $"[Gun Locker - Save {saveId}]", gun => 
            {
                holder.Snap(gun);
                gun.SetOwner(Item.Owner.Player);
                gun.data.displayName = data.DisplayName;
            }, holder.transform.position, holder.transform.rotation, null, true, data.DataList.CloneJson());
        }

        private void DeleteSave(string saveId)
        {
            var data = Catalog.GetData<GunLockerSaveData>(saveId);
            if (data == null) return;
            var path = Settings.GetSaveFolderPath() + "\\Saves\\" + data.id + ".json";
            if (File.Exists(path))
                File.Delete(path);
            var category = Catalog.GetCategoryData(Catalog.GetCategory(data.GetType()));
            category.catalogDatas.Remove(data);
            SetCategory(_currentCategory);
        }
        #endregion
    }
}
