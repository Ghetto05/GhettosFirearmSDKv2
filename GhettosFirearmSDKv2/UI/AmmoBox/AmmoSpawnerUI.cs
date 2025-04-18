using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2;

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

    public List<Transform> categories = [];
    public List<Transform> calibers = [];
    public List<Transform> variants = [];

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
            {
                item.DisallowDespawn = true;
            }
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
            {
                item.DisallowDespawn = locked;
            }
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

    public List<T> GetHeld<T>() where T : class, ICaliberGettable
    {
        var results = GetHeldFromHandle<T>(Player.local.handLeft.ragdollHand.grabbedHandle);
        results.AddRange(GetHeldFromHandle<T>(Player.local.handRight.ragdollHand.grabbedHandle));
        return results;
    }

    private List<T> GetHeldFromHandle<T>(Handle handle) where T : class, ICaliberGettable
    {
        if (!handle)
        {
            return [];
        }

        if (handle.item && handle.item.GetComponent<T>() is { } held)
        {
            return [held];
        }

        var attachmentFirearm = handle.GetComponentInParent<AttachmentFirearm>();
        var firearm = handle.item.GetComponent<Firearm>();
        FirearmBase f = !attachmentFirearm ? attachmentFirearm : firearm;
        if (f)
        {
            var fRes = new List<T>();
            if (f.bolt is T bolt)
            {
                fRes.Add(bolt);
            }
            if (f.magazineWell.currentMagazine is T magazine)
            {
                fRes.Add(magazine);
            }
            fRes.AddRange(f.GetComponentsInParent<T>().Where(x => x.GetTransform().GetComponentInParent<FirearmBase>() == f && !fRes.Contains(x)));
            return fRes;
        }

        return [];
    }

    public void GetCaliberFromGunOrMag()
    {
        var gettables = GetHeld<ICaliberGettable>();
        if (!gettables.Any())
        {
            return;
        }

        currentCaliber = gettables.First().GetCaliber();
        currentCategory = AmmoModule.GetCaliberCategory(currentCaliber);
        var variantsOfCaliber = AmmoModule.AllVariantsOfCaliber(currentCaliber);
        variantsOfCaliber.Sort(new FirstFourNumbersCompare());
        currentVariant = variantsOfCaliber[0].Remove(0, 5);

        SetupCategories();
        SetupCaliberList(currentCategory);
        SetupVariantList(currentCaliber);
        SetVariant(currentVariant);
    }

    public void ClearMagazine()
    {
        var loadable = GetHeld<IAmmunitionLoadable>();
        if (loadable is not null)
        {
            loadable.ForEach(x => x.ClearRounds());
        }
    }

    public void FillMagazine()
    {
        var loadables = GetHeld<IAmmunitionLoadable>();
        foreach (var loadable in loadables)
        {
            if (loadable is null || !Util.AllowLoadCartridge(currentCaliber, loadable, true))
            {
                continue;
            }

            loadable.ClearRounds();
            SpawnAndInsertCarRecursive(loadable, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
        }
    }

    public void TopOffMagazine()
    {
        var loadables = GetHeld<IAmmunitionLoadable>();
        foreach (var loadable in loadables)
        {
            if (loadable is null || !Util.AllowLoadCartridge(currentCaliber, loadable))
            {
                continue;
            }

            SpawnAndInsertCarRecursive(loadable, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
        }
    }

    private void SpawnAndInsertCarRecursive(IAmmunitionLoadable loadable, string carId)
    {
        if (loadable is null || loadable.GetLoadedCartridges().Count(x => x) >= loadable.GetCapacity() || string.IsNullOrWhiteSpace(carId))
        {
            return;
        }

        Util.SpawnItem(carId, "Ammo Spawner", cartridge =>
        {
            loadable.LoadRound(cartridge.GetComponent<Cartridge>());
            SpawnAndInsertCarRecursive(loadable, carId);
        }, transform.position);
    }

    public void SpawnRound()
    {
        if (!string.IsNullOrWhiteSpace(currentItemId))
        {
            Util.SpawnItem(currentItemId, "Ammo Spawner", _ => { }, spawnPosition.position, spawnPosition.rotation);
        }
    }

    #endregion

    #region Setups

    public void SetupCategories()
    {
        if (categories is not null)
        {
            foreach (var button in categories)
            {
                Destroy(button.gameObject);
            }
            categories.Clear();
        }
        if (calibers is not null)
        {
            foreach (var button in calibers)
            {
                Destroy(button.gameObject);
            }
            calibers.Clear();
        }
        if (variants is not null)
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
        if (calibers is not null)
        {
            foreach (var button in calibers)
            {
                Destroy(button.gameObject);
            }
            calibers.Clear();
        }
        if (variants is not null)
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
        if (variants is not null)
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
            if (category.Equals(currentCategory))
            {
                obj.transform.GetChild(0).gameObject.SetActive(true);
            }
            categories.Add(obj.transform);
        }
    }

    public void AddCaliber(string caliber)
    {
        if (!ContainsName(caliber, calibers))
        {
            var obj = Instantiate(caliberPref, calibersContent, true);
            obj.gameObject.name = caliber;
            obj.transform.localScale = Vector3.one;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localPosition = Vector3.zero;
            obj.SetActive(true);
            obj.GetComponent<Button>().onClick.AddListener(delegate { SetCaliber(obj.name); });
            if (caliber.Equals(currentCaliber))
            {
                obj.transform.GetChild(0).gameObject.SetActive(true);
            }
            calibers.Add(obj.transform);
            obj.GetComponentInChildren<TextMeshProUGUI>().text = caliber;
        }
    }

    public void AddVariant(string variant)
    {
        if (!ContainsName(variant, variants))
        {
            var obj = Instantiate(variantPref, variantContent, true);
            obj.gameObject.name = variant;
            obj.transform.localScale = Vector3.one;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localPosition = Vector3.zero;
            obj.SetActive(true);
            obj.GetComponent<Button>().onClick.AddListener(delegate { SetVariant(obj.name); });
            if (variant.Equals(currentVariant))
            {
                obj.transform.GetChild(0).gameObject.SetActive(true);
            }
            variants.Add(obj.transform);
            obj.GetComponentInChildren<TextMeshProUGUI>().text = variant;
        }
    }

    public bool ContainsName(string requiredName, List<Transform> transforms)
    {
        foreach (var t in transforms)
        {
            if (t.name.Equals(requiredName))
            {
                return true;
            }
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

        if (categories is not null)
        {
            foreach (var button in categories)
            {
                if (button.gameObject.name.Equals(currentCategory))
                {
                    button.GetChild(0).gameObject.SetActive(true);
                }
                else
                {
                    button.GetChild(0).gameObject.SetActive(false);
                }
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

        if (calibers is not null)
        {
            foreach (var button in calibers)
            {
                if (button.gameObject.name.Equals(currentCaliber))
                {
                    button.GetChild(0).gameObject.SetActive(true);
                }
                else
                {
                    button.GetChild(0).gameObject.SetActive(false);
                }
            }
        }
    }

    public static void DestroyAllOfList(List<Transform> list)
    {
        if (list is null)
        {
            return;
        }
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

        if (variants is not null)
        {
            foreach (var button in variants)
            {
                if (button.gameObject.name.Equals(currentVariant))
                {
                    button.GetChild(0).gameObject.SetActive(true);
                }
                else
                {
                    button.GetChild(0).gameObject.SetActive(false);
                }
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
            else if (intX < intY)
            {
                result = -1;
            }
            else
            {
                result = 1;
            }

            return result;
        }
    }
}