using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhettosFirearmSDKv2.Drone
{
    [AddComponentMenu("Firearm SDK v2/Drones/Drone 40")]
    public class Drone40 : MonoBehaviour
    {
        public static List<Drone40> all;

        public static Drone40 GetByID(string id)
        {
            if (all == null) return null;
            foreach (Drone40 drone in all)
            {
                if (drone.droneId.Equals(id)) return drone;
            }
            return null;
        }

        public enum DroneType
        {
            Grenade,
            Camera
        }

        public DroneType type;
        public string droneId;
        public float activationHeight;
        public float speed;
        public float ySpeed;
        public Rigidbody rb;
        public Animation extendAnimation;
        public float turnTime;
        public AudioSource loop;
        public ImpactDetonator detonator;
        public Camera cam;
        public Light spotlight;
        public Transform camRoot;
        public Vector3 camLeftAxis;
        public Vector3 camRightAxis;
        public float camHorizontalSpeed;
        public Transform camPivot;
        public Vector3 horizontalStart;
        public Vector3 horizontalEnd;
        public float camVerticalSpeed;
        Quaternion targetRotation = Quaternion.Euler(90, 0, 0);
        Quaternion startRot;
        float activationTime;
        public bool active;
        float startHeight;
        bool beforeSpin = true;
        [Space]
        public float driveShaftSpeed;
        [Space]
        public Transform driveShaft1;
        public Vector3 driveShaft1Axis;
        [Space]
        public Transform driveShaft2;
        public Vector3 driveShaft2Axis;
        [Space]
        public Transform driveShaft3;
        public Vector3 driveShaft3Axis;
        [Space]
        public Transform driveShaft4;
        public Vector3 driveShaft4Axis;
        float currentVertical = 0f;
        bool gottaAddConstrains = true;

        public enum cameraDirections
        {
            Up,
            Down,
            Left,
            Right
        }

        private void Awake()
        {
            startHeight = transform.position.y;
            droneId = $"Drone_{type}_{ Random.Range(0, 10)}{Random.Range(0, 10)}{Random.Range(0, 10)}{Random.Range(0, 10)}";
        }

        public void Move(float leftHandY, float rightHandX, float leftHandX, float rightHandY)
        {
            transform.Translate(new Vector3(leftHandY * speed, rightHandY * ySpeed, -rightHandX * speed));
            transform.Rotate(new Vector3(0, 0, -leftHandX * speed));
        }

        void FixedUpdate()
        {
            if (!active && beforeSpin && transform.position.y >= startHeight + activationHeight)
            {
                Activate();
            }
            else if (active)
            {
                if ((Time.time - activationTime) / turnTime > 1f && gottaAddConstrains)
                {
                    gottaAddConstrains = false;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
                }
                else if (gottaAddConstrains) transform.rotation = Quaternion.Slerp(startRot, targetRotation, (Time.time - activationTime) / turnTime);

                camPivot.localEulerAngles = Vector3.Lerp(horizontalStart, horizontalEnd, currentVertical);
            }
        }

        void Update()
        {
            if (!extendAnimation.isPlaying && active)
            {
                Rotate(driveShaft1, driveShaft1Axis, driveShaftSpeed * Time.deltaTime);
                Rotate(driveShaft2, driveShaft2Axis, driveShaftSpeed * Time.deltaTime);
                Rotate(driveShaft3, driveShaft3Axis, driveShaftSpeed * Time.deltaTime);
                Rotate(driveShaft4, driveShaft4Axis, driveShaftSpeed * Time.deltaTime);
            }
        }

        private void Rotate(Transform t, Vector3 axis, float angle)
        {
            t.localEulerAngles += axis * angle;
        }

        public void Activate()
        {
            activationTime = Time.time;
            extendAnimation.Play();
            active = true;
            beforeSpin = false;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            startRot = transform.rotation;
            loop.Play();
            if (all == null) all = new List<Drone40>();
            all.Add(this);
            Drone40ControlPanel.CallUpdateList();
        }

        public void Drop()
        {
            active = false;
            rb.useGravity = true;
            loop.Stop();
            all.Remove(this);
        }

        public void MoveCamera(cameraDirections direction)
        {
            if (direction == cameraDirections.Up)
            {
                currentVertical += camVerticalSpeed;
            }
            else if (direction == cameraDirections.Down)
            {
                currentVertical -= camVerticalSpeed;
            }
            else if (direction == cameraDirections.Left)
            {
                camRoot.localEulerAngles += camLeftAxis * camHorizontalSpeed;
            }
            else if (direction == cameraDirections.Right)
            {
                camRoot.localEulerAngles += camRightAxis * camHorizontalSpeed;
            }
        }

        public void ToggleLight(bool active)
        {
            spotlight.enabled = active;
        }
    }
}
