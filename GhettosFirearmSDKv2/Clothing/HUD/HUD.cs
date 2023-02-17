using UnityEngine;
using ThunderRoad;
using System.Collections;
using Chabuk.ManikinMono;
using System.Linq;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Clothing/HUD/HUD")]
    public class HUD : MonoBehaviour
    {
        public GameObject hudObject;

        private void Awake()
        {
            StartCoroutine(Delayed());
            //hudObject.transform.SetParent(Player.local.head.cam.transform);
            //hudObject.transform.localPosition = Vector3.zero;
            //hudObject.transform.localEulerAngles = Vector3.zero;
            //Debug.Log("Creature on HUD: " + GetComponentInParent<Creature>().creatureId);
        }

        private IEnumerator Delayed()
        {
            yield return new WaitForSeconds(0.5f);
            Creature cr = GetComponentInParent<Creature>();
            if (cr != Player.currentCreature)
            {
                hudObject.SetActive(false);
            }
        }

        public void Update()
        {
            hudObject.transform.position = Player.local.head.cam.transform.position;
            hudObject.transform.rotation = Player.local.head.cam.transform.rotation;
        }
    }
}
