using System.Collections.Generic;
using System.Linq;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class FireModeReplacer : MonoBehaviour
{
    private IAttachmentManager _attachmentManager;
    public Attachment attachment;
    public FirearmBase.FireModes oldFireMode;
    public FirearmBase.FireModes newFireMode;
    private readonly Dictionary<FiremodeSelector, List<int>> _replacedIndexes = new();

    private void Start()
    {
        Util.GetParent(null, attachment).GetInitialization(Init);
    }

    private void Init(IAttachmentManager manager, IComponentParent parent)
    {
        _attachmentManager = manager;
        Apply();
        attachment.OnDetachEvent += AttachmentOnOnDetachEvent;
    }

    private void AttachmentOnOnDetachEvent(bool despawnDetach)
    {
        if (!despawnDetach)
        {
            Revert();
        }
    }

    private void Apply()
    {
        if (_attachmentManager is not FirearmBase firearm)
        {
            return;
        }
        foreach (var selector in firearm.GetComponentsInChildren<FiremodeSelector>().Where(x => x.actualFirearm == firearm))
        {
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
            {
                firearm.fireMode = newFireMode;
            }
        }
    }

    private void Revert()
    {
        if (_attachmentManager is not FirearmBase firearm)
        {
            return;
        }
        foreach (var pair in _replacedIndexes)
        {
            foreach (var i in pair.Value)
            {
                pair.Key.firemodes[i] = oldFireMode;
            }

            if (pair.Value.Contains(pair.Key.currentIndex))
            {
                firearm.fireMode = oldFireMode;
            }
        }
    }
}