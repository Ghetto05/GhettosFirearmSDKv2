using System.Collections;
using System.Collections.Generic;
using GhettosFirearmSDKv2.Explosives;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class GrenadeSpoon : MonoBehaviour
{
    public Explosive explosive;
    public float fuseTime;
    public Transform startPosition;
    public Transform endPosition;
    public Transform body;
    public Rigidbody rb;
    public float deployForce;
    public Transform forceDir;
    public List<Lock> locks;
    public Item grenadeItem;
    public float deployTime;
    public AudioSource[] deploySounds;
    private float _startTime;
    private bool _moving;
    private bool _triggered;

    private void Start()
    {
        grenadeItem.OnUngrabEvent += GrenadeItem_OnUngrabEvent;
        grenadeItem.OnHeldActionEvent += GrenadeItem_OnHeldActionEvent;
        foreach (var l in locks)
        {
            l.ChangedEvent += L_ChangedEvent;
        }
    }

    private void L_ChangedEvent()
    {
        StartCoroutine(DelayedCheck());
    }

    private void GrenadeItem_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (action == Interactable.Action.UseStop)
        {
            Release();
        }
    }

    private void Update()
    {
        if (_moving && body != null && !_triggered)
        {
            body.localRotation = Quaternion.Lerp(startPosition.localRotation, endPosition.localRotation, (Time.time - _startTime) / deployTime);
        }
        if (Quaternion.Angle(endPosition.localRotation, body.transform.localRotation) < 0.01f && !_triggered)
        {
            _triggered = true;
            rb = body.gameObject.AddComponent<Rigidbody>();
            explosive.Detonate(fuseTime);
            body.SetParent(null);
            body.rotation = endPosition.rotation;
            body.position = endPosition.position;
            rb.velocity = grenadeItem.physicBody.velocity;
            rb.useGravity = true;
            StartCoroutine(DelayedAddForce(rb));
        }
    }

    private IEnumerator DelayedAddForce(Rigidbody targetRb)
    {
        yield return new WaitForSeconds(0.01f);
        targetRb.AddForce(forceDir.forward * deployForce * 10);
    }

    private void GrenadeItem_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
    {
        if (handle == grenadeItem.mainHandleLeft) StartCoroutine(DelayedCheck());
    }

    private IEnumerator DelayedCheck()
    {
        yield return new WaitForSeconds(0.01f);
        if (grenadeItem.mainHandleRight.handlers.Count < 1)
        {
            Release();
        }
    }

    public void Release(bool forced = false)
    {
        if (_moving) return;
        if (forced || AllLocksReleased())
        {
            _startTime = Time.time;
            Util.PlayRandomAudioSource(deploySounds);
            _moving = true;
        }
    }

    private bool AllLocksReleased()
    {
        foreach (var l in locks)
        {
            if (!l.IsUnlocked()) return false;
        }
        return true;
    }
}