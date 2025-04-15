using GhettosFirearmSDKv2.Attachments;
using GhettosFirearmSDKv2.Common;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class FiremodeSelector : MonoBehaviour
{
    private IComponentParent _parent;
    public FirearmBase actualFirearm;
    public GameObject firearm;
    public Attachment attachment;
    public Transform safetySwitch;
    public Transform safePosition;
    public Transform semiPosition;
    public Transform burstPosition;
    public Transform autoPosition;
    public Transform attachmentFirearmPosition;
    public AudioSource switchSound;
    public FirearmBase.FireModes[] firemodes;
    public float[] fireRates;
    public Transform[] irregularPositions;
    public int currentIndex;
    private SaveNodeValueInt _fireModeIndex;
    public Hammer hammer;
    public bool allowSwitchingModeIfHammerIsUncocked = true;
    public bool onlyAllowSwitchingIfBoltHasState;
    public BoltBase.BoltState switchAllowedState;

    private FirearmBase.FireModes _preAttachFireMode;

    private void OnDestroy()
    {
        actualFirearm.OnAltActionEvent -= OnAltAction;
        if (_parent is Attachment a)
        {
            a.OnDetachEvent -= AttachmentOnOnDetachEvent;
        }
    }

    private void Start()
    {
        Util.GetParent(firearm, attachment).GetInitialization(Init);
    }

    public void Init(IAttachmentManager manager, IComponentParent parent)
    {
        _parent = parent;
        if (manager is Firearm f)
        {
            actualFirearm = f;
        }
        
        if (parent is Attachment a)
        {
            a.OnDetachEvent += AttachmentOnOnDetachEvent;
            _preAttachFireMode = actualFirearm.fireMode;
            if (a.GetComponent<AttachmentFirearm>() is { } af)
            {
                actualFirearm = af;
            }
        }

        if (!actualFirearm)
        {
            return;
        }

        actualFirearm.OnAltActionEvent += OnAltAction;
        actualFirearm.fireMode = firemodes[currentIndex];
        UpdatePosition();

        _fireModeIndex = parent.SaveNode.GetOrAddValue("Firemode", new SaveNodeValueInt());
        actualFirearm.SetFiremode(firemodes[_fireModeIndex.Value]);
        currentIndex = _fireModeIndex.Value;
        UpdatePosition();
        OnFiremodeChanged?.Invoke(actualFirearm.fireMode);
    }

    private void AttachmentOnOnDetachEvent(bool despawnDetach)
    {
        actualFirearm.fireMode = _preAttachFireMode;
    }

    private void OnAltAction(bool longPress)
    {
        if (longPress && (allowSwitchingModeIfHammerIsUncocked || (hammer && hammer.cocked && (!onlyAllowSwitchingIfBoltHasState || !actualFirearm.bolt || actualFirearm.bolt.state == switchAllowedState))))
        {
            CycleFiremode();
        }
    }

    public void CycleFiremode()
    {
        if (currentIndex + 1 < firemodes.Length)
        {
            currentIndex++;
        }
        else
        {
            currentIndex = 0;
        }
        actualFirearm.SetFiremode(firemodes[currentIndex]);
        if (fireRates is not null && fireRates.Length > currentIndex)
        {
            actualFirearm.roundsPerMinute = fireRates[currentIndex];
        }
        if (switchSound)
        {
            switchSound.Play();
        }
        if (irregularPositions is not null && irregularPositions.Length > currentIndex)
        {
            safetySwitch.SetPositionAndRotation(irregularPositions[currentIndex].position, irregularPositions[currentIndex].rotation);
        }
        else
        {
            UpdatePosition();
        }
        _fireModeIndex.Value = currentIndex;
        OnFiremodeChanged?.Invoke(actualFirearm.fireMode);
    }

    private void UpdatePosition()
    {
        if (!safetySwitch)
        {
            return;
        }
        var mode = actualFirearm.fireMode;
        if (mode == FirearmBase.FireModes.Safe && safePosition)
        {
            safetySwitch.position = safePosition.position;
            safetySwitch.rotation = safePosition.rotation;
        }
        else if (mode == FirearmBase.FireModes.Semi && semiPosition)
        {
            safetySwitch.position = semiPosition.position;
            safetySwitch.rotation = semiPosition.rotation;
        }
        else if (mode == FirearmBase.FireModes.Burst && burstPosition)
        {
            safetySwitch.position = burstPosition.position;
            safetySwitch.rotation = burstPosition.rotation;
        }
        else if (mode == FirearmBase.FireModes.Auto && autoPosition)
        {
            safetySwitch.position = autoPosition.position;
            safetySwitch.rotation = autoPosition.rotation;
        }
        else if (mode == FirearmBase.FireModes.AttachmentFirearm && attachmentFirearmPosition)
        {
            safetySwitch.position = attachmentFirearmPosition.position;
            safetySwitch.rotation = attachmentFirearmPosition.rotation;
        }
    }

    public delegate void OnModeChangedDelegate(FirearmBase.FireModes newMode);

    public event OnModeChangedDelegate OnFiremodeChanged;
}