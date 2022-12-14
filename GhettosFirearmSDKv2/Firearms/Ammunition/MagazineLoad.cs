using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class MagazineLoad : MonoBehaviour
    {
        public int forCapacity;
        public string[] ids;

        public void Load(Magazine magazine)
        {
            Recurve(forCapacity - 1, magazine);
        }

        private void Recurve(int index, Magazine mag)
        {
            if (index < 0) return;
            Catalog.GetData<ItemData>(ids[index]).SpawnAsync(item =>
            {
                mag.InsertRound(item.GetComponent<Cartridge>(), true);
                Recurve(index - 1, mag);
            }, this.transform.position + Vector3.up * 3, this.transform.rotation);
        }
    }
}