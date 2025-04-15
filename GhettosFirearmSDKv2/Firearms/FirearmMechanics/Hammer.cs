using System.Collections.Generic;
using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class Hammer : MonoBehaviour
{
    public GameObject firearm;
    private FirearmBase _firearm;
    public Transform hammer;
    public Transform idlePosition;
    public Transform cockedPosition;
    public List<AudioSource> hitSounds;
    public List<AudioSource> cockSounds;
    public bool cocked;
    public bool hasDecocker;
    public bool allowManualCock;
    public bool allowCockUncockWhenSafetyIsOn = true;
    private SaveNodeValueBool _hammerState;

    private void Start()
    {
        Util.GetParent(firearm, null).GetInitialization(Init);
    }

    public void Init(IAttachmentManager manager, IComponentParent parent)
    {
        if (manager is not FirearmBase f)
        {
            return;
        }
        _firearm = f;
        _firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;
        _firearm.OnCockActionEvent += Firearm_OnCockActionEvent;
        _hammerState = parent.SaveNode.GetOrAddValue("HammerState", new SaveNodeValueBool());
        if (_hammerState.Value)
        {
            Cock(true, true);
        }
        else
        {
            Fire(true, true);
        }
    }

    private void Firearm_OnFiremodeChangedEvent()
    {
        if (hasDecocker && _firearm.fireMode == FirearmBase.FireModes.Safe)
        {
            Fire();
        }
    }

    private void Firearm_OnCockActionEvent()
    {
        if (!allowManualCock || (!allowCockUncockWhenSafetyIsOn && _firearm.fireMode == FirearmBase.FireModes.Safe))
        {
            return;
        }
        if (cocked)
        {
            Fire(true);
        }
        else
        {
            Cock();
        }
    }

    public void Cock(bool silent = false, bool forced = false)
    {
        if (cocked && !forced)
        {
            return;
        }
        _hammerState.Value = true;
        cocked = true;
        if (hammer)
        {
            hammer.localPosition = cockedPosition.localPosition;
            hammer.localEulerAngles = cockedPosition.localEulerAngles;
        }
        if (!silent)
        {
            Util.PlayRandomAudioSource(cockSounds);
        }
    }

    public void Fire(bool silent = false, bool forced = false)
    {
        if (!cocked && !forced)
        {
            return;
        }
        _hammerState.Value = false;
        cocked = false;
        if (hammer)
        {
            hammer.localPosition = idlePosition.localPosition;
            hammer.localEulerAngles = idlePosition.localEulerAngles;
        }
        if (!silent)
        {
            Util.PlayRandomAudioSource(hitSounds);
        }
    }
}