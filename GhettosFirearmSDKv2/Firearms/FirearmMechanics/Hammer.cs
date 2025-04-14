using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class Hammer : MonoBehaviour
{
    public FirearmBase firearm;
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
        Invoke(nameof(InvokedStart), Settings.invokeTime);
    }

    public void InvokedStart()
    {
        if (!firearm)
        {
            firearm = GetComponentInParent<Firearm>();
        }
        firearm.OnFiremodeChangedEvent += Firearm_OnFiremodeChangedEvent;
        firearm.OnCockActionEvent += Firearm_OnCockActionEvent;
        _hammerState = firearm.SaveNode.GetOrAddValue("HammerState", new SaveNodeValueBool());
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
        if (hasDecocker && firearm.fireMode == FirearmBase.FireModes.Safe)
        {
            Fire();
        }
    }

    private void Firearm_OnCockActionEvent()
    {
        if (!allowManualCock || (!allowCockUncockWhenSafetyIsOn && firearm.fireMode == FirearmBase.FireModes.Safe))
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