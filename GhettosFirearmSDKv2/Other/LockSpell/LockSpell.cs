using System;
using System.Linq;
using System.Reflection;
using ThunderRoad;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GhettosFirearmSDKv2.Other.LockSpell;

public class LockSpell : SpellCastCharge
{
    private const string SpellID = "Ghetto05.FirearmSDKv2.LockSpell";

    public static void ToggleEquip()
    {
        if (!Player.currentCreature ||
            !Player.characterData.mode.data.TryGetGameModeSaveData(out SandboxSaveData saveData) ||
            saveData.gameModeId != "Sandbox")
            return;

        var spellEquipped = Player.currentCreature.container.contents.HasContentWithID(SpellID);
        
        if (Settings.SpawnLockSpell && !spellEquipped)
            Player.currentCreature.container.AddSpellContent(SpellID);
        else if (spellEquipped)
            Player.currentCreature.container.RemoveContent(SpellID);
    }
    
    public override void Fire(bool active)
    {
        base.Fire(active);

        if (spellCaster.ragdollHand.otherHand.grabbedHandle?.item is not { } item || item.GetComponent<LockedItemModule>())
            return;

        if (Settings.stripLockedItems)
            Strip(item.transform);
        item.gameObject.AddComponent<LockedItemModule>();
    }

    private static void Strip(Transform t)
    {
        foreach (var c in t.GetComponents<MonoBehaviour>()
                     .Where(x => !_typeWhiteList.Contains(x.GetType()) && x.GetType().Assembly == Assembly.GetAssembly(typeof(LockSpell))))
        {
            Object.Destroy(c);
        }

        foreach (var ct in t.Cast<Transform>())
        {
            Strip(ct);
        }
    }

    private static Type[] _typeWhiteList = new[]
                                           {
                                               typeof(GhettoHandle)
                                           };
}