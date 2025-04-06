using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class FirearmClicker : MonoBehaviour
{
    public static List<FirearmClicker> all = new();

    public Transform[] axis;
    public Transform[] idlePos;
    public Transform[] pressedPos;

    public AudioSource[] clickSound;
    public AudioSource[] releaseSound;

    public bool triggered;
    private Item _item;

    private void Awake()
    {
        all.Add(this);
        _item = GetComponent<Item>();
    }

    private void FixedUpdate()
    {
        triggered = _item.handlers.Any() && _item.handlers.Any(h => h.creature.isPlayer && h.playerHand.controlHand.usePressed);
    }

    public static bool Trigger()
    {
        return all.Any(t => t.triggered);
    }
}