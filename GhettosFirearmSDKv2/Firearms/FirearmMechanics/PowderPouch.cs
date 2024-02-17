using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class PowderPouch : MonoBehaviour
    {
        private readonly float Delay = 0.02f;

        public Item item;
        public bool opened;
        public Transform source;
        public GameObject grain;
        public float maxAngle;

        public Transform lid;
        public Transform lidClosedPosition;
        public Transform lidOpenedPosition;

        private float _lastEject;

        private void Start()
        {
            item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
        }

        private void ItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                Tap();
            }

            if (action == Interactable.Action.AlternateUseStart)
            {
                if (opened)
                    Close();
                else
                    Open();
            }
        }

        [EasyButtons.Button]
        public void Open()
        {
            if (opened)
                return;
            opened = true;
            if (lid != null)
                lid.SetPositionAndRotation(lidOpenedPosition.position, lidOpenedPosition.rotation);
        }

        [EasyButtons.Button]
        public void Close()
        {
            if (!opened)
                return;
            opened = false;
            if (lid != null)
                lid.SetPositionAndRotation(lidClosedPosition.position, lidClosedPosition.rotation);
        }

        [EasyButtons.Button]
        public void Tap()
        {
            if (opened)
                Spawn();
        }

        private void FixedUpdate()
        {
            if (opened && Vector3.Angle(source.forward, Vector3.down) <= maxAngle)
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            if (Time.time - _lastEject > Delay)
            {
                GameObject grainIn = Instantiate(grain, source.position, Quaternion.Euler(Util.RandomRotation()));
                _lastEject = Time.time;
                grainIn.SetActive(true);
                Destroy(grainIn, 5f);
            }
        }
    }
}
