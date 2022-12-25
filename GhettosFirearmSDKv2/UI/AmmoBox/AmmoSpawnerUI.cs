using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2
{
    public class AmmoSpawnerUI : MonoBehaviour
    {
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
        public Text description;

        public bool locked;

        public List<Transform> categories;
        public List<Transform> calibers;
        public List<Transform> variants;

        private void Awake()
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
                Lock();
            }
            SetupCategories();
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart) Lock();
        }

        #region Actions
        public Magazine GetHeld()
        {
            if (Player.local.handRight.ragdollHand.grabbedHandle is Handle han && han.item is Item heldItem && heldItem.GetComponent<Firearm>() is Firearm firearm && firearm.magazineWell is MagazineWell magwell && magwell.currentMagazine is Magazine ma)
            {
                return ma;
            }
            else if (Player.local.handLeft.ragdollHand.grabbedHandle is Handle han1 && han1.item is Item heldItem2 && heldItem2.GetComponent<Firearm>() is Firearm firearm2 && firearm2.magazineWell is MagazineWell magwell2 && magwell2.currentMagazine is Magazine ma2)
            {
                return ma2;
            }
            else if (Player.local.handRight.ragdollHand.grabbedHandle is Handle han2 && han2.item is Item heldItem3 && heldItem3.GetComponent<Magazine>() is Magazine ma3)
            {
                return ma3;
            }
            else if (Player.local.handLeft.ragdollHand.grabbedHandle is Handle han3 && han3.item is Item heldItem4 && heldItem4.GetComponent<Magazine>() is Magazine ma4)
            {
                return ma4;
            }
            else return null;
        }

        public void GetCaliberFromGunOrMag()
        {
            Magazine mag = GetHeld();

            if (mag != null)
            {
                currentCaliber = mag.caliber;
                currentCategory = AmmoModule.GetCaliberCategory(currentCaliber).Remove(0, 5);
                List<string> varis = AmmoModule.AllVariantsOfCaliber(currentCaliber);
                varis.Sort();
                currentVariant = varis[0].Remove(0, 5);

                SetupCategories();
                SetupCaliberList(currentCategory);
                SetupVariantList(currentCaliber);
                SetVariant(currentVariant);
            }
            else
            {
                Debug.Log("No magazine found!");
            }
        }

        public void ClearMagazine(Magazine mag = null)
        {
            if (mag == null)
            {
                mag = GetHeld();
            }
            if (mag == null) return;

            foreach (Cartridge car in mag.cartridges)
            {
                car.item.Despawn(0.05f);
            }
            mag.cartridges.Clear();
        }

        public void FillMagazine()
        {
            Magazine mag = GetHeld();
            if (mag == null) return;
            if (!Util.AllowLoadCatridge(currentCaliber, mag)) return;

            ClearMagazine(mag);
            SpawnAndInsertCar(mag, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
        }

        private void SpawnAndInsertCar(Magazine mag, string carId)
        {
            if (mag != null && mag.cartridges.Count < mag.maximumCapacity && !string.IsNullOrWhiteSpace(carId))
            {
                Catalog.GetData<ItemData>(carId).SpawnAsync(cartr => 
                {
                    mag.InsertRound(cartr.GetComponent<Cartridge>(), true);
                    SpawnAndInsertCar(mag, carId);
                }, mag.transform.position);
            }
        }

        public void TopOffMagazine()
        {
            Magazine mag = GetHeld();
            if (mag == null) return;
            if (!Util.AllowLoadCatridge(currentCaliber, mag)) return;

            SpawnAndInsertCar(mag, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
        }

        public void SpawnRound()
        {
            if (!string.IsNullOrWhiteSpace(currentItemId))
            {
                Catalog.GetData<ItemData>(currentItemId).SpawnAsync(car => {}, spawnPosition.position, spawnPosition.rotation);
            }
        }

        public void Lock()
        {
            locked = !locked;
            if (item != null)
            {
                item.rb.isKinematic = locked;
                item.disallowDespawn = locked;
            }
            canvasCollider.enabled = locked;
            canvas.enabled = locked;
        }
        #endregion Actions

        #region Setups
        public void SetupCategories()
        {
            #region cleanup
            if (categories != null)
            {
                foreach (Transform button in categories)
                {
                    Destroy(button.gameObject);
                }
                categories.Clear();
            }
            #endregion cleanup
            
            List<string> list = AmmoModule.AllCategories();
            list.Sort();
            foreach (string s in list)
            {
                AddCategory(s.Remove(0, 5));
            }
        }

        public void SetupCaliberList(string category)
        {
            #region cleanup
            if (calibers != null)
            {
                foreach (Transform button in calibers)
                {
                    Destroy(button.gameObject);
                }
                calibers.Clear();
            }
            #endregion cleanup
            
            List<string> list = AmmoModule.AllCalibersOfCategory(category);
            list.Sort();
            foreach (string s in list)
            {
                AddCaliber(s.Remove(0, 5));
            }
        }

        public void SetupVariantList(string caliber)
        {
            #region cleanup
            if (variants != null)
            {
                foreach (Transform button in variants)
                {
                    Destroy(button.gameObject);
                }
                variants.Clear();
            }
            #endregion cleanup

            List<string> list = AmmoModule.AllVariantsOfCaliber(caliber);
            list.Sort();
            foreach (string s in list)
            {
                AddVariant(s.Remove(0, 5));
            }
        }

        public void AddCategory(string category)
        {
            if (categories == null) categories = new List<Transform>();
            if (!ContainsName(category, categories))
            {
                GameObject obj = Instantiate(categoryPref);
                obj.name = category;
                obj.transform.SetParent(categoriesContent);
                obj.transform.localScale = Vector3.one;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(delegate { SetCategory(obj.name); });
                obj.GetComponentInChildren<Text>().text = category;
                if (category.Equals(currentCategory)) obj.transform.GetChild(0).gameObject.SetActive(true);
                categories.Add(obj.transform);
            }
        }

        public void AddCaliber(string caliber)
        {
            if (calibers == null) calibers = new List<Transform>();
            if (!ContainsName(caliber, calibers))
            {
                GameObject obj = Instantiate(caliberPref);
                obj.name = caliber;
                obj.transform.SetParent(calibersContent);
                obj.transform.localScale = Vector3.one;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(delegate { SetCaliber(obj.name); });
                if (caliber.Equals(currentCaliber)) obj.transform.GetChild(0).gameObject.SetActive(true);
                calibers.Add(obj.transform);
                obj.GetComponentInChildren<Text>().text = caliber;
            }
        }

        public void AddVariant(string variant)
        {
            if (variants == null) variants = new List<Transform>();
            if (!ContainsName(variant, variants))
            {
                GameObject obj = Instantiate(variantPref);
                obj.name = variant;
                obj.transform.SetParent(variantContent);
                obj.transform.localScale = Vector3.one;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localPosition = Vector3.zero;
                obj.SetActive(true);
                obj.GetComponent<Button>().onClick.AddListener(delegate { SetVariant(obj.name); });
                if (variant.Equals(currentVariant)) obj.transform.GetChild(0).gameObject.SetActive(true);
                variants.Add(obj.transform);
                obj.GetComponentInChildren<Text>().text = variant;
            }
        }

        public bool ContainsName(string requiredName, List<Transform> transforms)
        {
            foreach (Transform t in transforms)
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
            SetupCaliberList(category);
            SetupCategories();
        }

        public void SetCaliber(string caliber)
        {
            currentCaliber = caliber;
            SetupVariantList(caliber);
            SetupCaliberList(currentCategory);
        }

        public void SetVariant(string variant)
        {
            currentVariant = variant;
            SetupVariantList(currentCaliber);
            currentItemId = AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant);
            description.text = AmmoModule.GetDescription(currentItemId);
        }
        #endregion Updates
    }
}
