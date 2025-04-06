using TMPro;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AmmoCounter : MonoBehaviour
{
    public TextMeshProUGUI counter;
    public FirearmBase firearm;
    public Attachment attachment;
    public string counterTextFormat;
    public bool tryToDisplayCapacity;
    public bool countChamberAsCapacity;
    public string nullText;

    private BoltBase _bolt;
    private MagazineWell _magazineWell;

    public void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        if (attachment && attachment.attachmentPoint.ConnectedManager is FirearmBase f)
        {
            firearm = f;
        }

        if (firearm)
        {
            _bolt = firearm.bolt;
            _magazineWell = firearm.magazineWell;
        }
    }

    private void Update()
    {
        var count = -1;
        var capacity = -1;
        if (_bolt)
        {
            count = 0;
            capacity = 1;
            if (_bolt.GetChamber())
            {
                count++;
            }
        }

        if (_magazineWell)
        {
            if (count == -1)
            {
                count = 0;
            }
            if (_magazineWell.currentMagazine)
            {
                capacity += _magazineWell.currentMagazine.ActualCapacity;
                count += _magazineWell.currentMagazine.cartridges.Count;
            }
        }

        if (count != -1)
        {
            if (!tryToDisplayCapacity)
            {
                counter.text = string.Format(counterTextFormat.Replace("\\n", "\n"), count);
            }
            else
            {
                counter.text = string.Format(counterTextFormat.Replace("\\n", "\n"), count, capacity);
            }
        }
        else
        {
            counter.text = nullText;
        }
    }
}