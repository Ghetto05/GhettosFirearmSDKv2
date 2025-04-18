using System.Collections;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2;

public class AmmoBelt : MonoBehaviour
{
    public Magazine magazine;
    public int hideCount;
    public GameObject[] beltLinks;
    public string linkItem;
    public float beltLinkEjectForce;

    private bool _inserted;
    private Transform[] _allPositions;
    private Transform[] _cappedPositions;

    private void Start()
    {
        var original = magazine.cartridgePositions.ToList();
        _allPositions = original.ToArray();
        original.RemoveRange(0, hideCount);
        _cappedPositions = original.ToArray();
        magazine.cartridgePositions = _cappedPositions;
        magazine.OnConsumeEvent += MagazineOnOnConsumeEvent;
        magazine.OnInsertEvent += _ => Insert();
        magazine.OnEjectEvent += _ => Remove();
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    private void InvokedStart()
    {
        Remove(true);
    }

    private void MagazineOnOnConsumeEvent(Cartridge c)
    {
        if (c && magazine.currentWell.beltLinkEjectDir)
        {
            if (Catalog.GetData<ItemData>(linkItem, false) is { } data)
            {
                data.SpawnAsync(item =>
                {
                    Util.IgnoreCollision(item.gameObject, magazine.currentWell.firearm.item.gameObject, true);
                    StartCoroutine(LinkEject(item));
                    item.Despawn(5f);
                }, magazine.currentWell.beltLinkEjectDir.position, magazine.currentWell.beltLinkEjectDir.rotation);
            }
        }
    }

    private IEnumerator LinkEject(Item item)
    {
        yield return new WaitForSeconds(0.01f);

        var f = Settings.cartridgeEjectionForceRandomizationDivision;
        item.physicBody.AddForce(magazine.currentWell.beltLinkEjectDir.forward * (beltLinkEjectForce + Random.Range(-(beltLinkEjectForce / f), beltLinkEjectForce / f)), ForceMode.Impulse);
        f = Settings.cartridgeEjectionTorque;
        var torque = new Vector3
                     {
                         x = Random.Range(-f, f),
                         y = Random.Range(-f, f),
                         z = Random.Range(-f, f)
                     };
        item.physicBody.AddTorque(torque, ForceMode.Impulse);
    }

    public void Insert()
    {
        if (_inserted)
        {
            return;
        }
        _inserted = true;

        magazine.cartridgePositions = _allPositions;
        magazine.UpdateCartridgePositions();
        for (var i = 0; i < hideCount; i++)
        {
            beltLinks[i].SetActive(true);
        }
    }

    public void Remove(bool initial = false)
    {
        if (!_inserted && !initial)
        {
            return;
        }
        _inserted = false;

        magazine.cartridgePositions = _cappedPositions;
        magazine.UpdateCartridgePositions();
        for (var i = 0; i < hideCount; i++)
        {
            beltLinks[i].SetActive(false);
        }
    }

    private void Update()
    {
        var linkCount = magazine.cartridges.Count;
        if (!_inserted)
        {
            linkCount += hideCount;
        }
        for (var i = 0; i < beltLinks.Length; i++)
        {
            beltLinks[i].SetActive(linkCount > i && (_inserted || i >= hideCount));
        }
    }
}