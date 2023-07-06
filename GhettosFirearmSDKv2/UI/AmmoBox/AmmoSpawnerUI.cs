using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;

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
        public TextMeshProUGUI description;

        public bool locked;

        public List<Transform> categories;
        public List<Transform> calibers;
        public List<Transform> variants;

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
                Lock();
            }
            SetupCategories();
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart) Lock();
        }

        #region Actions
        public Magazine GetHeldMagazine()
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

        public Speedloader GetHeldSpeedloader()
        {
            if (Player.local.handRight.ragdollHand.grabbedHandle is Handle han2 && han2.item is Item heldItem3 && heldItem3.GetComponent<Speedloader>() is Speedloader ma3)
            {
                return ma3;
            }
            else if (Player.local.handLeft.ragdollHand.grabbedHandle is Handle han3 && han3.item is Item heldItem4 && heldItem4.GetComponent<Speedloader>() is Speedloader ma4)
            {
                return ma4;
            }
            else return null;
        }

        public void GetCaliberFromGunOrMag()
        {
            Magazine mag = GetHeldMagazine();
            Speedloader sped = GetHeldSpeedloader();

            if (mag != null || sped != null)
            {
                if (mag != null) currentCaliber = mag.caliber;
                else currentCaliber = sped.calibers[0];
                currentCategory = AmmoModule.GetCaliberCategory(currentCaliber).Remove(0, 5);
                List<string> varis = AmmoModule.AllVariantsOfCaliber(currentCaliber);
                varis.Sort(new FirstFourNumbersCompare());
                currentVariant = varis[0].Remove(0, 5);

                SetupCategories();
                SetupCaliberList(currentCategory);
                SetupVariantList(currentCaliber);
                SetVariant(currentVariant);
            }
        }

        public void ClearMagazine()
        {
            if (GetHeldMagazine() != null) ClearMagazine(GetHeldMagazine());
            if (GetHeldSpeedloader() != null) ClearSpeedloader(GetHeldSpeedloader());
        }

        public void ClearMagazine(Magazine mag)
        {
            foreach (Cartridge car in mag.cartridges)
            {
                car.item.Despawn(0.05f);
            }
            mag.cartridges.Clear();
        }

        public void ClearSpeedloader(Speedloader speedloader)
        {
            for (int i = 0; i < speedloader.loadedCartridges.Length; i++)
            {
                if (speedloader.loadedCartridges[i] != null)
                {
                    speedloader.loadedCartridges[i].item.Despawn(0.05f);
                    speedloader.loadedCartridges[i] = null;
                }
            }
        }

        public void FillMagazine()
        {
            Magazine mag = GetHeldMagazine();
            Speedloader sped = GetHeldSpeedloader();
            if (mag != null && Util.AllowLoadCatridge(currentCaliber, mag))
            {
                ClearMagazine(mag);
                SpawnAndInsertCar(mag, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
            }
            if (sped != null && Util.AllowLoadCatridge(sped.calibers[0], currentCaliber))
            {
                ClearSpeedloader(sped);
                FillSpeedloader(sped, AmmoModule.GetCartridgeItemId(currentCategory, currentCaliber, currentVariant));
            }
        }

        private void FillSpeedloader(Speedloader sped, string carId, int index = 0)
        {
            if (sped != null && !string.IsNullOrWhiteSpace(carId) && index < sped.loadedCartridges.Length)
            {
                if (sped.loadedCartridges[index] == null)
                {
                    Catalog.GetData<ItemData>(carId).SpawnAsync(cartr =>
                    {
                        sped.LoadSlot(index, cartr.GetComponent<Cartridge>(), true);
                        FillSpeedloader(sped, carId, index + 1);
                    }, sped.transform.position + Vector3.up * 2);
                }
                else
                {
                    FillSpeedloader(sped, carId, index + 1);
                }
            }
        }

        private void SpawnAndInsertCar(Magazine mag, string carId)
        {
            if (mag != null && mag.cartridges.Count < mag.maximumCapacity && !string.IsNullOrWhiteSpace(carId))
            {
                Catalog.GetData<ItemData>(carId).SpawnAsync(cartr => 
                {
                    mag.InsertRound(cartr.GetComponent<Cartridge>(), true, false);
                    SpawnAndInsertCar(mag, carId);
                }, mag.transform.position);
            }
        }

        public void TopOffMagazine()
        {
            Magazine mag = GetHeldMagazine();
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
                item.physicBody.isKinematic = locked;
                item.disallowDespawn = locked;
            }
            canvasCollider.enabled = locked;
            canvas.enabled = locked;
        }
        #endregion Actions

        #region Setups
        public void SetupCategories()
        {
            if (categories != null)
            {
                foreach (Transform button in categories)
                {
                    Destroy(button.gameObject);
                }
                categories.Clear();
            }
            if (calibers != null)
            {
                foreach (Transform button in calibers)
                {
                    Destroy(button.gameObject);
                }
                calibers.Clear();
            }
            if (variants != null)
            {
                foreach (Transform button in variants)
                {
                    Destroy(button.gameObject);
                }
                variants.Clear();
            }

            List<string> list = AmmoModule.AllCategories();
            list.Sort(new FirstFourNumbersCompare());
            foreach (string s in list)
            {
                AddCategory(s.Remove(0, 5));
            }
        }

        public void SetupCaliberList(string category)
        {
            if (calibers != null)
            {
                foreach (Transform button in calibers)
                {
                    Destroy(button.gameObject);
                }
                calibers.Clear();
            }
            if (variants != null)
            {
                foreach (Transform button in variants)
                {
                    Destroy(button.gameObject);
                }
                variants.Clear();
            }

            List<string> list = AmmoModule.AllCalibersOfCategory(category);
            list.Sort(new FirstFourNumbersCompare());
            foreach (string s in list)
            {
                AddCaliber(s.Remove(0, 5));
            }
        }

        public void SetupVariantList(string caliber)
        {
            if (variants != null)
            {
                foreach (Transform button in variants)
                {
                    Destroy(button.gameObject);
                }
                variants.Clear();
            }

            List<string> list = AmmoModule.AllVariantsOfCaliber(caliber);
            list.Sort(new FirstFourNumbersCompare());
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
                obj.gameObject.name = category;
                obj.transform.SetParent(categoriesContent);
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
                GameObject obj = Instantiate(caliberPref);
                obj.gameObject.name = caliber;
                obj.transform.SetParent(calibersContent);
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
                GameObject obj = Instantiate(variantPref);
                obj.gameObject.name = variant;
                obj.transform.SetParent(variantContent);
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
            currentCaliber = "";
            currentVariant = "";
            description.text = "";
            //SetupCategories();
            SetupCaliberList(category);

            if (categories != null)
            {
                foreach (Transform button in categories)
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
                foreach (Transform button in calibers)
                {
                    if (button.gameObject.name.Equals(currentCaliber)) button.GetChild(0).gameObject.SetActive(true);
                    else button.GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        public static void DestroyAllOfList(List<Transform> list)
        {
            if (list == null) return;
            foreach (Transform t in list.ToArray())
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
                foreach (Transform button in variants)
                {
                    if (button.gameObject.name.Equals(currentVariant)) button.GetChild(0).gameObject.SetActive(true);
                    else button.GetChild(0).gameObject.SetActive(false);
                }
            }

            Addressables.LoadAssetAsync<GameObject>(Catalog.GetData<ItemData>(currentItemId).prefabLocation).Completed += (handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    if (handle.Result.GetComponent<ProjectileData>() is ProjectileData data)
                    {
                        string descriptionText = "";
                        if (!data.isHitscan)
                        {
                            //descriptionText += "Projectile: " + data.projectileItemId + "\n";
                            descriptionText += "Velocity: " + data.muzzleVelocity;
                            if (!string.IsNullOrWhiteSpace(data.additionalInformation))
                            {
                                descriptionText += "\n";
                                descriptionText += data.additionalInformation;
                            }
                        }
                        else
                        {
                            if (data.projectileCount > 0)
                            {
                                if (data.projectileCount == 1)
                                {
                                    descriptionText += "Damage: " + (data.damagePerProjectile / 50) * 100 + "%\n";
                                    descriptionText += "Force: " + data.forcePerProjectile + "\n";
                                }
                                else if (data.projectileCount > 1)
                                {
                                    descriptionText += "Projectile count: " + data.projectileCount + "\n";
                                    descriptionText += "Damage per projectile: " + (data.damagePerProjectile / 50) * 100 + "%\n";
                                    descriptionText += "Force per projectile: " + data.forcePerProjectile + "\n";
                                }
                                descriptionText += "Range: " + data.projectileRange + "\n";
                                descriptionText += "Penetration level: " + data.penetrationPower.ToString();
                                if (handle.Result.GetComponentInChildren<TracerModule>() != null) descriptionText += "\nHas tracer function";
                                if (data.forceDestabilize && !data.knocksOutTemporarily) descriptionText += "\nAlways destabilizes hit target";
                                if (data.forceIncapitate) descriptionText += $"\nIncapacitates hit target permanently";
                                else if (data.knocksOutTemporarily) descriptionText += $"\nincapacitates hit target for {data.temporaryKnockoutTime} seconds";
                                if (data.isElectrifying) descriptionText += $"\nElectrifies targets for {data.tasingDuration} with a force of {data.tasingForce}";
                                if (data.isExplosive) descriptionText += $"\nExplodes: {data.explosiveData.radius} meters radius, {data.explosiveData.force} force, {data.explosiveData.damage} damage";
                            }
                            if (!string.IsNullOrWhiteSpace(data.additionalInformation))
                            {
                                descriptionText += "\n" + data.additionalInformation;
                            }
                        }

                        description.text = descriptionText;
                    }
                    else Debug.LogWarning("No projectile data component found on root object of " + currentCaliber + ", " + currentVariant + "! Please make sure it is added to the root, not a child object!");
                }
                else
                {
                    Debug.LogError("Couldn't load prefab of " + currentCaliber + ", " + currentVariant + "!");
                }
            });
        }
        #endregion Updates

        public class FirstFourNumbersCompare : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                int intX = int.Parse(x.Substring(0, 4));
                int intY = int.Parse(y.Substring(0, 4));

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