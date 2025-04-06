using UnityEngine;

namespace GhettosFirearmSDKv2;

public class StockTogglerAdditional : MonoBehaviour
{
    public StockToggler parent;
    public AudioSource toggleSound;
    public Transform pivot;
    public Transform[] positions;
    public bool useAsSeparateObjects;

    private void Start()
    {
        parent.OnToggleEvent += ApplyPosition;
    }

    public void ApplyPosition(int index, bool playSound = true)
    {
        if (toggleSound && playSound)
        {
            toggleSound.Play();
        }
        if (!useAsSeparateObjects)
        {
            pivot.localPosition = positions[index].localPosition;
            pivot.localEulerAngles = positions[index].localEulerAngles;
        }
        else
        {
            for (var i = 0; i < positions.Length; i++)
            {
                positions[i].gameObject.SetActive(i == index);
            }
        }
    }
}