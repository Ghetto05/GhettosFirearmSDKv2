using UnityEngine;

namespace GhettosFirearmSDKv2;

public class ChamberLoader : MonoBehaviour, ICaliberGettable
{
    public string caliber;
    public Collider loadCollider;
    public BoltBase boltToBeLoaded;
    public Lock lockingCondition;
    public FirearmBase firearm;
    public AudioSource[] insertSounds;

    private void Start()
    {
        firearm.OnCollisionEvent += OnCollisionEnter;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Util.CheckForCollisionWithThisCollider(collision, loadCollider) && collision.collider.GetComponentInParent<Cartridge>() is { } c )
        {
            TryLoad(c);
        }
    }

    public bool TryLoad(Cartridge cartridge)
    {
        if ((!lockingCondition || lockingCondition.IsUnlocked()) &&
            Util.AllowLoadCartridge(cartridge, firearm.magazineWell?.caliber ?? caliber) &&
            boltToBeLoaded.LoadChamber(cartridge, false))
        {
            Util.PlayRandomAudioSource(insertSounds);
            return true;
        }
        return false;
    }

    public string GetCaliber()
    {
        return firearm.magazineWell?.caliber ?? caliber;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}