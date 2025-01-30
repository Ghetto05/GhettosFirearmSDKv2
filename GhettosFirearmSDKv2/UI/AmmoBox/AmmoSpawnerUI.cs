using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2
{
    public class AmmoSpawnerUI : MonoBehaviour
    {
        #region Values

        public CaliberSortingData SortingData;
        public bool alwaysFrozen;

        [Space]
        public Transform spawnPosition;

        public Item item;
        public Canvas canvas;
        public Collider canvasCollider;
        public Transform categoriesContent;
        public Transform calibersContent;
        public Transform variantContent;

        [Space]
        public string currentCategory;

        public string currentCaliber;
        public string currentVariant;
        public string currentItemId;

        [Space]
        public GameObject categoryPref;

        public GameObject caliberPref;
        public GameObject variantPref;

        [Space]
        public TextMeshProUGUI description;

        public bool locked;

        public List<Transform> categories;
        public List<Transform> calibers;
        public List<Transform> variants;

        #endregion
        
        private void Start()
        {
            description.text = "";
            if (!alwaysFrozen)
            {
                item.OnHeldActionEvent += Item_OnHeldActionEvent;
                canvas.enabled = false;
                canvasCollider.enabled = false;
            }
            else
            {
                locked = false;
                if (item)
                    item.DisallowDespawn = true;
            }

            SortingData = Catalog.GetDataList<CaliberSortingData>().FirstOrDefault();
            
            SetupCategories();
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                locked = !locked;
                if (item)
                    item.DisallowDespawn = locked;
            }
        }

        private void FixedUpdate()
        {
            var frozen = alwaysFrozen || (locked && !item.handlers.Any());
            canvasCollider.enabled = frozen;
            canvas.enabled = frozen;
            if (item)
            {
                item.physicBody.isKinematic = frozen;
            }
        }

        #region Actions

        public T GetHeld<T>() where T : class, ICaliberGettable
        {
            Handle foundHandle = null;
            if (Player.local.handRight.ragdollHand.grabbedHandle)
                foundHandle = Player.local.handRight.ragdollHand.grabbedHandle;
            else if (Player.local.handLeft.ragdollHand.grabbedHandle)
                foundHandle = Player.local.handLeft.ragdollHand.grabbedHandle;

            if (foundHandle)
            {
                T found;

                found = typeof(T) == typeof(IAmmunitionLoadable) ? 
                    foundHandle.GetComponentsInParent<T>().FirstOrDefault(x => ((IAmmunitionLoadable)x).GetCapacity() != 0) : 
                    foundHandle.GetComponentsInParent<T>().FirstOrDefault();

                if (found != null)
                    return found;

                var firearm = foundHandle.GetComponentInParent<FirearmBase>();
                if (firearm != null)
                {
                    if (firearm.magazineWell != null && firearm.magazineWell.currentMagazine != null)
                        return firearm.magazineWell.currentMagazine.GetComponent<T>();

                    var foundAmmunitionLoaders = firearm.GetComponentsInChildren<IAmmunitionLoadable>();
                    if (foundAmmunitionLoaders.Any())
                    {
                        found = (T)foundAmmunitionLoaders.FirstOrDefault(x => x.GetTransform().GetComponentInParent<FirearmBase>() == null);

                        if (found == null)
                            found = (T)foundAmmunitionLoaders.FirstOrDefault(x => x.GetCapacity() != 0);

                        if (found == null)
                            found = (T)foundAmmunitionLoaders.FirstOrDefault();

                        return found;
                    }
                    
                    var foundCaliberGetters = firearm.GetComponentsInChildren<ICaliberGettable>();
                    if (foundCaliberGetters.Any())
                    {
                        found = (T)foundCaliberGetters.FirstOrDefault(x => x.GetTransform().GetComponentInParent<FirearmBase>() == null);

                        if (found == null)
                            found = (T)foundCaliberGetters.FirstOrDefault();

                        return found;
                    }
                }
            }

            return default;
        }

        public void GetCaliberFromGunOrMag()
        {
            var gettable = GetHeld<ICaliberGettable>();
            if (gettable != null)
            {
                currentCaliber = gettable.GetCaliber();
                currentCategory = AmmoModule.GetCaliberCategory(currentCaliber);
                var variantsOfCaliber = AmmoModule.AllVariantsOfCaliber(currentCaliber);
                variantsOfCaliber.Sort(new FirstFourNumbersCompare());
                currentVariant = variantsOfCaliber[0].Remove(0, 5);

                SetupCategories();
                SetupCaliberList(currentCategory);
                SetupVariantList(currentCaliber);
                SetVariant(currentVariant);
            }
        }

        public void ClearMagazine()
        {
            var loadable = GetHeld<IAmmunitionLoadable>();
            if (loadable != null)
                loadable.ClearRounds();
        }

        public void FillMagazine()
        {
            var loadable = GetHeld<IAmmunitionLoadable>();
            if (loadable == null || !Util.AllowLoadCartridge(currentCaliber, loadable, true))
                return;
            
            loadable.ClearRounds();
            SpawnAndInsertCar(loadable, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
        }

        public void TopOffMagazine()
        {
            var loadable = GetHeld<IAmmunitionLoadable>();
            if (loadable == null || !Util.AllowLoadCartridge(currentCaliber, loadable))
                return;

            SpawnAndInsertCar(loadable, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
        }

        private void SpawnAndInsertCar(IAmmunitionLoadable loadable, string carId)
        {
            if (loadable == null || loadable.GetLoadedCartridges().Count(x => x) >= loadable.GetCapacity() || string.IsNullOrWhiteSpace(carId))
                return;
            
            Util.SpawnItem(carId, "Ammo Spawner", cartridge => 
            {
                loadable.LoadRound(cartridge.GetComponent<Cartridge>());
                SpawnAndInsertCar(loadable, carId);
            }, transform.position);
        }

        public void SpawnRound()
        {
            if (!string.IsNullOrWhiteSpace(currentItemId))
            {
                Util.SpawnItem(currentItemId, "Ammo Spawner", _ => {}, spawnPosition.position, spawnPosition.rotation);
            }
        }
        
        #endregion Actions

        #region Setups
        public void SetupCategories()
        {
            if (categories != null)
            {
                foreach (var button in categories)
                {
                    Destroy(button.gameObject);
                }
                categories.Clear();
            }
            if (calibers != null)
            {
                foreach (var button in calibers)
                {
                    Destroy(button.gameObject);
                }
                calibers.Clear();
            }
            if (variants != null)
            {
                foreach (var button in variants)
                {
                    Destroy(button.gameObject);
                }
                variants.Clear();
            }

            var list = AmmoModule.AllCategories().OrderBy(c => SortingData?.SortedCategories.IndexOf(c)).ToList();
            foreach (var s in list)
            {
                AddCategory(s);
            }
        }

        public void SetupCaliberList(string category)
        {
            if (calibers != null)
            {
                foreach (var button in calibers)
                {
                    Destroy(button.gameObject);
                }
                calibers.Clear();
            }
            if (variants != null)
            {
                foreach (var button in variants)
                {
                    Destroy(button.gameObject);
                }
                variants.Clear();
            }

            var list = AmmoModule.AllCalibersOfCategory(category).OrderBy(c => SortingData?.SortedCalibers.IndexOf(c)).ToList();
            foreach (var s in list)
            {
                AddCaliber(s);
            }
        }

        public void SetupVariantList(string caliber)
        {
            if (variants != null)
            {
                foreach (var button in variants)
                {
                    Destroy(button.gameObject);
                }
                variants.Clear();
            }

            var list = AmmoModule.AllVariantsOfCaliber(caliber);
            list.Sort(new FirstFourNumbersCompare());
            foreach (var s in list)
            {
                AddVariant(s.Remove(0, 5));
            }
        }

        public void AddCategory(string category)
        {
            if (categories == null) categories = new List<Transform>();
            if (!ContainsName(category, categories))
            {
                var obj = Instantiate(categoryPref, categoriesContent, true);
                obj.gameObject.name = category;
                obj.transform.localScale = Vector3.one;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(delegate { SetCategory(obj.name); });
                obj.GetComponentInChildren<TextMeshProUGUI>().text = category;
                if (category.Equals(currentCategory)) obj.transform.GetChild(0).gameObject.SetActive(true);
                categories.Add(obj.transform);
            }
        }

        public void AddCaliber(string caliber)
        {
            if (calibers == null) calibers = new List<Transform>();
            if (!ContainsName(caliber, calibers))
            {
                var obj = Instantiate(caliberPref, calibersContent, true);
                obj.gameObject.name = caliber;
                obj.transform.localScale = Vector3.one;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(delegate { SetCaliber(obj.name); });
                if (caliber.Equals(currentCaliber)) obj.transform.GetChild(0).gameObject.SetActive(true);
                calibers.Add(obj.transform);
                obj.GetComponentInChildren<TextMeshProUGUI>().text = caliber;
            }
        }

        public void AddVariant(string variant)
        {
            if (variants == null) variants = new List<Transform>();
            if (!ContainsName(variant, variants))
            {
                var obj = Instantiate(variantPref, variantContent, true);
                obj.gameObject.name = variant;
                obj.transform.localScale = Vector3.one;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(delegate { SetVariant(obj.name); });
                if (variant.Equals(currentVariant)) obj.transform.GetChild(0).gameObject.SetActive(true);
                variants.Add(obj.transform);
                obj.GetComponentInChildren<TextMeshProUGUI>().text = variant;
            }
        }

        public bool ContainsName(string requiredName, List<Transform> transforms)
        {
            foreach (var t in transforms)
            {
                if (t.name.Equals(requiredName)) return true;
            }
            return false;
        }
        #endregion Setups

        #region Updates
        public void SetCategory(string category)
        {
            currentCategory = category;
            currentCaliber = "";
            currentVariant = "";
            description.text = "";
            //SetupCategories();
            SetupCaliberList(category);

            if (categories != null)
            {
                foreach (var button in categories)
                {
                    if (button.gameObject.name.Equals(currentCategory)) button.GetChild(0).gameObject.SetActive(true);
                    else button.GetChild(0).gameObject.SetActive(false);
                }
            }
            DestroyAllOfList(variants);
        }

        public void SetCaliber(string caliber)
        {
            currentCaliber = caliber;
            currentVariant = "";
            description.text = "";
            //SetupCaliberList(currentCategory);
            SetupVariantList(caliber);

            if (calibers != null)
            {
                foreach (var button in calibers)
                {
                    if (button.gameObject.name.Equals(currentCaliber)) button.GetChild(0).gameObject.SetActive(true);
                    else button.GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        public static void DestroyAllOfList(List<Transform> list)
        {
            if (list == null) return;
            foreach (var t in list.ToArray())
            {
                Destroy(t.gameObject);
            }
            list.Clear();
        }

        public void SetVariant(string variant)
        {
            currentVariant = variant;
            currentItemId = AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant);
            //SetupVariantList(currentCaliber);

            if (variants != null)
            {
                foreach (var button in variants)
                {
                    if (button.gameObject.name.Equals(currentVariant)) button.GetChild(0).gameObject.SetActive(true);
                    else button.GetChild(0).gameObject.SetActive(false);
                }
            }

            description.text = Catalog.GetData<ItemData>(currentItemId)?.description;
        }
        #endregion Updates

        public class FirstFourNumbersCompare : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var intX = int.Parse(x!.Substring(0, 4));
                var intY = int.Parse(y!.Substring(0, 4));

                int result;
                if (intX == intY)
                {
                    Debug.Log("Duplicate position found! " + x + ", " + y);
                    result = 0;
                }
                else if (intX < intY) result = -1;
                else result = 1;

                return result;
            }
        }
    }
}