using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Firearm components/Reciprocating barrel")]
public class ReciprocatingBarrel : MonoBehaviour
{
    public BoltBase bolt;
    public bool lockBoltBack;
    public Transform pivot;
    public Transform front;
    public Transform rear;
    public float pauseTime;
    private States _state;
    private float _moveStartTime;

    private enum States
    {
        Front,
        GoingBack,
        Back,
        GoingFront
    }

    private void Start()
    {
        bolt.OnFireEvent += Bolt_OnFireEvent;
        _state = States.Front;
    }

    private void Bolt_OnFireEvent()
    {
        _moveStartTime = Time.time;
        _state = States.GoingBack;
    }

    public bool AllowBoltReturn()
    {
        if (lockBoltBack)
        {
            return _state == States.Front;
        }

        return true;
    }

    private float Lerp(float startTime)
    {
        var timeThatPassed = Time.time - startTime;
        var timeForOneRound = 60f / bolt.firearm.roundsPerMinute;
        return timeThatPassed / (timeForOneRound / 2f);
    }

    private float _pauseElapsed;

    private void FixedUpdate()
    {
        if (_state == States.Back)
        {
            bool proceed;
            if (pauseTime != 0)
            {
                _pauseElapsed += Time.fixedDeltaTime;
                proceed = _pauseElapsed >= pauseTime;
            }
            else
            {
                proceed = true;
            }
            if (proceed)
            {
                _moveStartTime = Time.time;
                _state = States.GoingFront;
                if (lockBoltBack)
                {
                    bolt.EjectRound();
                    bolt.TryLoadRound();
                }
            }
        }

        if (_state == States.GoingBack)
        {
            _pauseElapsed = 0;
            pivot.localPosition = Vector3.Lerp(front.localPosition, rear.localPosition, Lerp(_moveStartTime));
        }
        else if (_state == States.GoingFront)
        {
            pivot.localPosition = Vector3.Lerp(rear.localPosition, front.localPosition, Lerp(_moveStartTime));
        }

        if (Util.AbsDist(pivot.localPosition, rear.localPosition) < 0.0001f && _state == States.GoingBack)
        {
            _state = States.Back;
            pivot.localPosition = rear.localPosition;
        }
        else if (Util.AbsDist(pivot.localPosition, front.localPosition) < 0.0001f && _state == States.GoingFront)
        {
            _state = States.Front;
            pivot.localPosition = front.localPosition;
            if (lockBoltBack)
            {
                bolt.TryRelease(true);
            }
        }
    }
}