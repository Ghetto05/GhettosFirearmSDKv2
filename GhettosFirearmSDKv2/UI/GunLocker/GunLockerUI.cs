using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.UI;
using System.Linq;
using IngameDebugConsole;
using System;
using System.IO;
using Newtonsoft.Json;
using TMPro;

namespace GhettosFirearmSDKv2
{
    public class GunLockerUI : MonoBehaviour
    {
        public Transform categoryContent;
        public GameObject categoryPrefab;
        public List<string> categories;
        private List<GameObject> categoryButtons;
        private string currentCategory;

        public Transform savesContent;
        public GameObject savesPrefab;
        public List<string> saves;
        private List<GameObject> saveButtons;

        public List<Button> keys;
        public List<Button> keysCaps;
        public List<Button> keysNonCaps;
        public TextMeshProUGUI typingField;
        public GameObject typingPanel;
        public Button cancelButton;
        public Button confirmButton;

        public Holder holder;

        private bool typing = false;
        private string typingName = "";
        public bool shift = true;
        public bool caps = true;
        private float lastCursorShift = 0.0f; 

        private void Awake()
        {
            SetupCategoryList();

            foreach (Button b in keys)
            {
                b.onClick.AddListener(delegate { Type(b.GetComponentInChildren<TextMeshProUGUI>().text); });
            }
            Cancel();

            cancelButton.onClick.AddListener(delegate { Cancel(); });
            confirmButton.onClick.AddListener(delegate { SaveWeaponWithName(typingName); });
        }

        #region Typing
        public void Type(string key)
        {
            if (typing)
            {
                if (typingName.EndsWith("|")) typingName = typingName.Remove(typingName.Length - 1, 1);
                if (typingName.Equals("Type here...")) typingName = "";
                if (key.Equals("BACKSPACE") && typingName.Length > 0) typingName = typingName.Remove(typingName.Length - 1);
                else if (key.Equals("SHIFT")) shift = !shift;
                else if (key.Equals("CAPS")) caps = !caps;
                else
                {
                    typingName += key;
                    shift = false;
                }
                typingField.text = typingName;
                UpdateShift();
            }
        }

        public void Update()
        {
            if (typing && !typingName.Equals("Type here...") && Time.time - lastCursorShift > 0.6f)
            {
                lastCursorShift = Time.time;
                if (typingName.EndsWith("|"))
                {
                    typingName = typingName.Remove(typingName.Length - 1, 1);
                }
                else
                {
                    typingName += "|";
                }
                typingField.text = typingName;
            }
        }

        public void UpdateShift()
        {
            foreach (Button capsKey in keysCaps)
            {
                capsKey.gameObject.SetActive(shift || caps);
            }
            foreach (Button nonCapsKey in keysNonCaps)
            {
                nonCapsKey.gameObject.SetActive(!shift && ! caps);
            }
        }

        public void OpenTypingPanel()
        {
            typingName = "Type here...";
            typingField.text = typingName;
            typingPanel.SetActive(true);
            typing = true;
            shift = true;
            caps = false;
            UpdateShift();
        }

        public void Cancel()
        {
            typingName = "Type here...";
            typingField.text = typingName;
            typingPanel.SetActive(false);
            typing = false;
        }
        #endregion

        #region Saving
        public void SaveWeapon()
        {
            if (holder.items.Count == 0 || typing) return;
            OpenTypingPanel();
        }

        public void SaveWeaponWithName(string name)
        {
            if (!typing) return;
            if (holder.items.Count == 0)
            {
                Cancel();
                return;
            }

            if (name.EndsWith("|")) name = name.Remove(name.Length - 1, 1);
            string idString = name.Replace(" ", "");
            idString = idString.Replace(",", "");
            idString = idString.Replace(".", "");
            idString = idString.Replace("-", "");
            idString = idString.Replace("_", "");
            idString = idString.Replace("/", "");
            idString = idString.Replace("#", "");
            idString = idString.Replace("&", "");
            idString = idString.Replace("|", "");
            bool preb = FirearmsSettings.saveAsPrebuilt;

            DeleteSave(idString);
            
            GunLockerSaveData newData = new GunLockerSaveData
            {
                id = preb ? "PREBUILT_" + idString : "SAVE_" + idString,
                displayName = name,
                itemId = holder.items[0].itemId,
                category = preb ? "Prebuilts" : holder.items[0].data.displayName,
                dataList = holder.items[0].contentCustomData.CloneJson()
            };

            FirearmsSettings.CreateSaveFolder();
            string path = FirearmsSettings.GetSaveFolderPath() + "\\Saves\\" + newData.id + ".json";
            string content = JsonConvert.SerializeObject(newData, Catalog.jsonSerializerSettings);
            Catalog.LoadJson(newData, content, path, FirearmsSettings.saveFolderName + "\\Saves");
            File.WriteAllText(path, content);
            SetCategory(currentCategory);
            Cancel();
        }
        #endregion

        #region Locker Actions
        [EasyButtons.Button]
        public void SetupCategoryList()
        {
            categories = GunLockerSaveData.GetAllCategories();
            categories.Sort();
            categories.Insert(0, "Prebuilts");

            if (categoryButtons != null)
            {
                foreach (GameObject obj in categoryButtons)
                {
                    Destroy(obj);
                }
                categoryButtons.Clear();
            }
            else categoryButtons = new List<GameObject>();

            foreach (string cat in categories)
            {
                GameObject buttonObj = Instantiate(categoryPrefab, categoryContent);
                buttonObj.SetActive(true);
                buttonObj.transform.localPosition = Vector3.zero;
                buttonObj.transform.localEulerAngles = Vector3.zero;
                GunLockerUICategory categoryComp = buttonObj.GetComponent<GunLockerUICategory>();
                categoryComp.button.onClick.AddListener(delegate { SetCategory(cat); });
                categoryComp.textName.text = cat;
                categoryButtons.Add(buttonObj);
                if (currentCategory != null && currentCategory.Equals(cat))
                {
                    categoryComp.SelectionOutline.SetActive(true);
                }
            }
        }

        private void SetCategory(string category)
        {
            currentCategory = category;
            if (saveButtons != null)
            {
                foreach (GameObject obj in saveButtons)
                {
                    Destroy(obj);
                }
                saveButtons.Clear();
            }
            else saveButtons = new List<GameObject>();

            foreach (GunLockerSaveData data in Catalog.GetDataList<GunLockerSaveData>().Where(i => i.category.Equals(currentCategory)))
            {
                GameObject buttonObj = Instantiate(savesPrefab, savesContent);
                buttonObj.SetActive(true);
                buttonObj.transform.localPosition = Vector3.zero;
                buttonObj.transform.localEulerAngles = Vector3.zero;
                GunLockerUISave saveComp = buttonObj.GetComponent<GunLockerUISave>();
                if (data.category.Equals("Prebuilts")) saveComp.deleteButton.gameObject.SetActive(false);
                saveComp.button.onClick.AddListener(delegate { SpawnSave(data.id); });
                saveComp.deleteButton.onClick.AddListener(delegate { DeleteSave(data.id); });
                saveComp.textName.text = data.displayName;
                saveButtons.Add(buttonObj);
            }
            SetupCategoryList();
        }

        private void SpawnSave(string saveId)
        {
            if (holder.items.Count > 0 && holder.UnSnapOne() is Item i) i.Despawn();
            GunLockerSaveData data = Catalog.GetData<GunLockerSaveData>(saveId);
            Util.SpawnItem(data.itemId, $"[Gun Locker - Save {saveId}]", gun => 
            {
                holder.Snap(gun);
                gun.SetOwner(Item.Owner.Player);
                gun.data.displayName = data.displayName;
            }, holder.transform.position, holder.transform.rotation, null, true, data.dataList.CloneJson());
        }

        private void DeleteSave(string saveId)
        {
            GunLockerSaveData data = Catalog.GetData<GunLockerSaveData>(saveId);
            if (data == null) return;
            string path = FirearmsSettings.GetSaveFolderPath() + "\\Saves\\" + data.id + ".json";
            if (File.Exists(path))
                File.Delete(path);
            CatalogCategory category = Catalog.GetCategoryData(Catalog.GetCategory(data.GetType()));
            category.catalogDatas.Remove(data);
            SetCategory(currentCategory);
        }
        #endregion
    }
}
