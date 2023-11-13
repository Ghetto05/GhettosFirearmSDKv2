using System;
using UnityEngine;
using TMPro;

namespace GhettosFirearmSDKv2
{
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
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            if (attachment != null)
            {
                firearm = attachment.attachmentPoint.parentFirearm;
            }

            if (firearm != null)
            {
                _bolt = firearm.bolt;
                _magazineWell = firearm.magazineWell;
            }
        }

        private void Update()
        {
            int count = -1;
            int capacity = -1;
            if (_bolt != null)
            {
                count = 0;
                capacity = 1;
                if (_bolt.GetChamber() != null) count++;
            }

            if (_magazineWell != null)
            {
                if (count == -1)
                    count = 0;
                if (_magazineWell.currentMagazine != null)
                {
                    capacity += _magazineWell.currentMagazine.maximumCapacity;
                    count += _magazineWell.currentMagazine.cartridges.Count;
                }
            }

            if (count != -1)
            {
                if (!tryToDisplayCapacity)
                    counter.text = string.Format(counterTextFormat.Replace("\\n", "\n"), count);
                else
                    counter.text = string.Format(counterTextFormat.Replace("\\n", "\n"), count, capacity);
            }
            else
                counter.text = nullText;
        }
    }
}
