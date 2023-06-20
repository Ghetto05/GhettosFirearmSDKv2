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

        public Holder holder;

        public void SaveWeapon()
        {
            if (holder.items.Count == 0) return; 
            DateTime today = DateTime.Now;
            GunLockerSaveData newData = new GunLockerSaveData
            {
                id = ((DateTimeOffset)today).ToUnixTimeSeconds().ToString(),
                displayName = today.ToString(),
                itemId = holder.items[0].itemId,
                category = holder.items[0].data.displayName,
                dataList = holder.items[0].contentCustomData.ToList()
            };
            FirearmsSettings.CreateSaveFolder();
            string path = FirearmsSettings.GetSaveFolderPath() + "\\Saves\\" + newData.id + ".json";
            string content = JsonConvert.SerializeObject(newData, Catalog.jsonSerializerSettings);
            Catalog.LoadJson(newData, content, path, FirearmsSettings.saveFolderName + "\\Saves");
            File.WriteAllText(path, content);
            SetupCategoryList();
        }

        private void Awake()
        {
            SetupCategoryList();
        }

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
            Catalog.GetData<ItemData>(data.itemId).SpawnAsync(gun => 
            {
                holder.Snap(gun);
            }, holder.transform.position, holder.transform.rotation, null, true, data.dataList.CloneJson());
        }

        private void DeleteSave(string saveId)
        {
            GunLockerSaveData data = Catalog.GetData<GunLockerSaveData>(saveId);
            string path = FirearmsSettings.GetSaveFolderPath() + "\\Saves\\" + data.id + ".json";
            if (File.Exists(path)) File.Delete(path);
            Catalog.data[(int)Catalog.GetCategory(data.GetType())].catalogDatas.Remove(data);
            SetCategory(currentCategory);
        }
    }
}
