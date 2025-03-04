using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Bolt assemblies/Minigun")]
public class Minigun : BoltBase
{
    public bool revOnTrigger;
    public bool loopingMuzzleFlash;
        
    public float[] barrelAngles;
    public Transform roundMount;
    public Cartridge loadedCartridge;
    public Transform roundEjectPoint;
    public Transform roundEjectDir;
    public float roundEjectForce;
    public Transform barrel;

    public AudioSource revUpSound;
    public AudioSource revDownSound;
    public AudioSource rotatingLoop;
    public AudioSource rotatingLoopPlusFiring;
    private bool _revving;
    private float _degreesPerSecond;

    private float _lastShotTime;
    private float _currentSpeed;
    private float _revUpBeginTime;
    private float _beginTime = -100f;
    private bool _revvingUp;
    private bool _revvingDown;


    private void Start()
    {
        if (!revOnTrigger)
            firearm.item.OnHeldActionEvent += Item_OnHeldActionEvent;
        else
            firearm.OnTriggerChangeEvent += FirearmOnOnTriggerChangeEvent;
    }

    private void FirearmOnOnTriggerChangeEvent(bool isPulled)
    {
        if (isPulled)
            StartRevving();
        else
            StopRevving();
    }

    private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (handle == firearm.item.mainHandleRight)
        {
            if (action == Interactable.Action.AlternateUseStart)
                StartRevving();
            else if (action == Interactable.Action.AlternateUseStop)
                StopRevving();
        }
    }

    private void StartRevving()
    {
        if (_revving)
            return;
        _revvingUp = true;
        _revvingDown = false;
        _beginTime = Time.time;
        _revUpBeginTime = Time.time;
        revUpSound.Play();
        var timeForOneRound = 60f / firearm.roundsPerMinute;
        var timeForOneRotation = timeForOneRound * barrelAngles.Length;
        var rotationsPerSecond = 1 / timeForOneRotation;
        _degreesPerSecond = rotationsPerSecond * 360;
    }

    private void StopRevving()
    {
        if (!_revving)
            return;
        _revving = false;
        _revvingUp = false;
        _revvingDown = true;
        _beginTime = Time.time;
        rotatingLoop.Stop();
        rotatingLoopPlusFiring.Stop();
        revUpSound.Stop();
        revDownSound.Play();
            
        if (loopingMuzzleFlash && firearm.defaultMuzzleFlash != null && firearm.defaultMuzzleFlash.isPlaying)
            firearm.defaultMuzzleFlash.Stop();
    }

    private void FixedUpdate()
    {
        _revving = _revvingUp && (Time.time - _revUpBeginTime >= revUpSound.clip.length);
            
        if (_revvingUp || _revvingDown)
        {
            var timeSinceStart = Time.time - _beginTime;
            var speed = timeSinceStart / revUpSound.clip.length;
            if (speed > 1)
                speed = 1;
            if (_revvingDown)
                speed = 1f - speed;
            _currentSpeed = speed;
        }

        if (barrel != null)
            barrel.Rotate(new Vector3(0, 0, _degreesPerSecond * Time.deltaTime * _currentSpeed));

        if (fireOnTriggerPress && firearm.triggerState && _revving && Time.time - _lastShotTime >= 60f / firearm.roundsPerMinute)
        {
            TryFire();
        }

        if (!rotatingLoopPlusFiring.isPlaying && _revving && firearm.triggerState && !firearm.magazineWell.IsEmpty())
        {
            rotatingLoopPlusFiring.Play();
            rotatingLoop.Stop();
        }
        if (!rotatingLoop.isPlaying && _revving && (!firearm.triggerState || firearm.magazineWell.IsEmpty()))
        {
            rotatingLoop.Play();
            rotatingLoopPlusFiring.Stop();
        }
    }

    private void Update()
    {
        BaseUpdate();
    }

    public override void TryFire()
    {
        TryLoadRound();
        if (loadedCartridge == null || loadedCartridge.Fired)
        {
            InvokeFireLogicFinishedEvent();
            return;
        }
        _lastShotTime = Time.time;
        foreach (var hand in firearm.item.handlers)
        {
            if (hand.playerHand != null && hand.playerHand.controlHand != null) 
                hand.playerHand.controlHand.HapticShort(50f);
        }
        if (!loopingMuzzleFlash && loadedCartridge.data.playFirearmDefaultMuzzleFlash)
            firearm.PlayMuzzleFlash(loadedCartridge);
        else if (loopingMuzzleFlash && firearm.defaultMuzzleFlash != null && !firearm.defaultMuzzleFlash.isPlaying)
            firearm.defaultMuzzleFlash.Play();
        IncrementBreachSmokeTime();
        firearm.PlayFireSound(loadedCartridge);
        FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
        Util.PlayRandomAudioSource(firearm.fireSounds);
        FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
        loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, true);
        EjectRound();
        InvokeFireEvent();
        InvokeFireLogicFinishedEvent();
    }

    public override void EjectRound()
    {
        if (loadedCartridge == null)
            return;
        var c = loadedCartridge;
        loadedCartridge = null;
        if (roundEjectPoint != null)
        {
            c.transform.position = roundEjectPoint.position;
            c.transform.rotation = roundEjectPoint.rotation;
        }
        Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
        c.ToggleCollision(true);
        Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
        var rb = c.GetComponent<Rigidbody>();
        c.item.DisallowDespawn = false;
        c.transform.parent = null;
        rb.isKinematic = false;
        c.loaded = false;
        rb.WakeUp();
        if (roundEjectDir != null)
        {
            AddTorqueToCartridge(c);
            AddForceToCartridge(c, roundEjectDir, roundEjectForce);
        }
        c.ToggleHandles(true);
        if (firearm.magazineWell != null && firearm.magazineWell.IsEmptyAndHasMagazine() && firearm.magazineWell.currentMagazine.ejectOnLastRoundFired)
            firearm.magazineWell.Eject();
        InvokeEjectRound(c);
    }

    public override void TryLoadRound()
    {
        if (loadedCartridge == null && firearm.magazineWell.ConsumeRound() is { } c)
        {
            loadedCartridge = c;
            c.GetComponent<Rigidbody>().isKinematic = true;
            c.transform.parent = roundMount;
            c.transform.localPosition = Vector3.zero;
            c.transform.localEulerAngles = Util.RandomCartridgeRotation();
        }
    }
}