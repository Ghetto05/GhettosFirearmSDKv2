using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class FireModeReplacer : MonoBehaviour
{
    public Attachment attachment;
    public FirearmBase.FireModes oldFireMode;
    public FirearmBase.FireModes newFireMode;
    private Dictionary<FiremodeSelector, List<int>> _replacedIndexes = new();

    private void Start()
    {
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    private void InvokedStart()
    {
        Apply();
        attachment.OnDetachEvent += AttachmentOnOnDetachEvent;
    }

    private void AttachmentOnOnDetachEvent(bool despawndetach)
    {
        if (!despawndetach)
            Revert();
    }

    private void Apply()
    {
        var firearm = attachment.attachmentPoint.parentFirearm;
        foreach (var selector in firearm.GetComponentsInChildren<FiremodeSelector>().Where(x => x.firearm == firearm))
        {
            Debug.Log($"Applying to {selector.name}");
            _replacedIndexes.Add(selector, new List<int>());

            for (var i = 0; i < selector.firemodes.Length; i++)
            {
                if (selector.firemodes[i] == oldFireMode)
                {
                    _replacedIndexes[selector].Add(i);
                }
            }
            
            foreach (var i in _replacedIndexes[selector])
            {
                selector.firemodes[i] = newFireMode;
            }

            if (_replacedIndexes[selector].Contains(selector.currentIndex))
                firearm.fireMode = newFireMode;
        }
    }

    private void Revert()
    {
        var firearm = attachment.attachmentPoint.parentFirearm;
        foreach (var pair in _replacedIndexes)
        {
            foreach (var i in pair.Value)
            {
                pair.Key.firemodes[i] = oldFireMode;
            }

            if (pair.Value.Contains(pair.Key.currentIndex))
                firearm.fireMode = oldFireMode;
        }
    }
}