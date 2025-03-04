using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Clothing/HUD/HUD")]
public class HUD : MonoBehaviour
{
    public GameObject hudObject;
    public Transform scaleRoot;

    private void Start()
    {
        var cr = GetComponentInParent<Creature>();
        if (!cr.brain.instance.id.Equals("Player"))
        {
            Destroy(hudObject);
            enabled = false;
            Destroy(this);
        }
        else
        {
            hudObject.transform.SetParent(Player.local.head.cam.transform);
            hudObject.transform.localPosition = Vector3.zero;
            hudObject.transform.localEulerAngles = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        Destroy(hudObject);
    }
}