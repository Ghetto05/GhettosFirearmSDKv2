using System.Collections;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    [AddComponentMenu("Firearm SDK v2/Firearm components/Reciprocating barrel")]
    public class ReciprocatingBarrel : MonoBehaviour
    {
        public BoltBase bolt;
        public bool lockBoltBack;
        public Transform pivot;
        public Transform front;
        public Transform rear;
        public float pauseTime;
        private States state;
        float moveStartTime;

        private enum States
        {
            Front,
            GoingBack,
            Back,
            GoingFront
        }

        private void Awake()
        {
            bolt.OnFireEvent += Bolt_OnFireEvent;
            state = States.Front;
        }

        private void Bolt_OnFireEvent()
        {
            moveStartTime = Time.time;
            state = States.GoingBack;
            //StartCoroutine(CycleIE());
        }

        private IEnumerator CycleIE()
        {
            //atFront = false;
            //while (Util.AbsDist(pivot.position, rear.position) > 0.04)
            //{
            //    pivot.localPosition = Vector3.Lerp(rear.localPosition, front.localPosition, Lerp(moveStartTime));
            //    yield return null;
            //}
            yield return new WaitForSeconds(pauseTime);
            //if (lockBoltBack)
            //{
            //    bolt.EjectRound();
            //    bolt.TryLoadRound();
            //}
            //moveStartTime = Time.time;
            //while (Util.AbsDist(pivot.position, front.position) > 0.04)
            //{
            //    pivot.localPosition = Vector3.Lerp(front.localPosition, rear.localPosition, Lerp(moveStartTime));
            //    yield return null;
            //}
            //atFront = true;

            //if (lockBoltBack)
            //{
            //    bolt.TryRelease();
            //}
        }

        public bool AllowBoltReturn()
        {
            if (lockBoltBack) return state == States.Front;
            else return true;
        }

        private float Lerp(float startTime)
        {
            float timeThatPassed = Time.time - startTime;
            float timeForOneRound = 60f / bolt.firearm.roundsPerMinute;
            return timeThatPassed / (timeForOneRound / 2f);
        }

        private float pauseElapsed = 0;
        private void FixedUpdate()
        {
            if (state == States.Back)
            {
                bool proceed;
                if (pauseTime != 0)
                {
                    pauseElapsed += Time.fixedDeltaTime;
                    proceed = pauseElapsed >= pauseTime;
                }
                else proceed = true;
                if (proceed)
                {
                    moveStartTime = Time.time;
                    state = States.GoingFront;
                    bolt.EjectRound();
                    bolt.TryLoadRound();
                }
            }

            if (state == States.GoingBack)
            {
                pauseElapsed = 0;
                pivot.localPosition = Vector3.Lerp(front.localPosition, rear.localPosition, Lerp(moveStartTime));
            }
            else if (state == States.GoingFront)
            {
                pivot.localPosition = Vector3.Lerp(rear.localPosition, front.localPosition, Lerp(moveStartTime));
            }

            if (Util.AbsDist(pivot.localPosition, rear.localPosition) < 0.0001f && state == States.GoingBack)
            {
                state = States.Back;
                pivot.localPosition = rear.localPosition;
            }
            else if (Util.AbsDist(pivot.localPosition, front.localPosition) < 0.0001f && state == States.GoingFront)
            {
                state = States.Front;
                pivot.localPosition = front.localPosition;
                if (lockBoltBack)
                {
                    bolt.TryRelease(true);
                }
            }
        }
    }
}
