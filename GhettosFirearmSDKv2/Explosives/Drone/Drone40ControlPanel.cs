using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2.Drone
{
    public class Drone40ControlPanel : MonoBehaviour
    {
        public Drone40 currentDrone;
        public Item item;
        [Header("Selection")]
        public GameObject selectionCanvas;
        public List<GameObject> buttons;
        public RectTransform contentTransform;
        public GameObject buttonPrefab;
        [Header("Control")]
        public GameObject controlCanvas;
        public GameObject grenadeSubControlPanel;
        [Header("Camera control")]
        public GameObject cameraSubControlPanel;
        public RawImage camFeed;
        public RawImage lightBulb;
        public Color lightActiveColor;
        public Color lightDisabledColor;
        public bool lightActive;
        private bool _controlMode;

        private void FixedUpdate()
        {
            if (_controlMode && currentDrone != null && controlCanvas.activeInHierarchy)
            {
                currentDrone.Move(PlayerControl.handLeft.JoystickAxis.y, PlayerControl.handRight.JoystickAxis.x, PlayerControl.handLeft.JoystickAxis.x, PlayerControl.handRight.JoystickAxis.y);
            }
        }

        public void ToggleControlMode(bool active)
        {
            _controlMode = active && item.handlers.Count == 2;
            //PlayerControl.moveActive = !controlMode;
            //PlayerControl.turnActive = !controlMode;
            Player.local.locomotion.allowMove = !active;
            Player.local.locomotion.allowTurn = !active;
            Player.local.locomotion.allowCrouch = !active;
            Player.local.locomotion.allowJump = !active;
        }

        private void Awake()
        {
            OnUpdateList += UpdateList;
            GoToSelection();
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            ToggleControlMode(true);
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            ToggleControlMode(false);
        }

        public void UpdateList()
        {
            if (contentTransform.gameObject.activeInHierarchy)
            {
                GoToSelection();
            }
        }

        public void GoToSelection()
        {
            ToggleControlMode(false);
            currentDrone = null;
            controlCanvas.SetActive(false);
            selectionCanvas.SetActive(true);
            if (Drone40.all == null) return;
            if (buttons != null)
            {
                foreach (var obj in buttons)
                {
                    Destroy(obj);
                }
            }
            buttons = new List<GameObject>();

            foreach (var drone in Drone40.all)
            {
                var button = Instantiate(buttonPrefab, contentTransform);
                button.SetActive(true);
                buttons.Add(button);
                button.transform.localPosition = new Vector3(20, -20 - (buttons.IndexOf(button) * 70), 0);
                button.transform.localEulerAngles = Vector3.zero;
                button.transform.GetChild(0).gameObject.GetComponent<Text>().text = drone.droneId;
                button.GetComponent<Button>().onClick.AddListener(delegate { GoToDroneControl(button); });
            }
            contentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 20 + (buttons.Count * 70));
        }

        public void GoToDroneControl(GameObject button)
        {
            controlCanvas.SetActive(true);
            selectionCanvas.SetActive(false);
            currentDrone = Drone40.GetByID(button.transform.GetChild(0).gameObject.GetComponent<Text>().text);
            lightActive = false;
            cameraSubControlPanel.SetActive(currentDrone.type == Drone40.DroneType.Camera);
            grenadeSubControlPanel.SetActive(currentDrone.type == Drone40.DroneType.Grenade);
            if (currentDrone.type == Drone40.DroneType.Camera)
            {
                camFeed.texture = new RenderTexture(currentDrone.cam.targetTexture);
                currentDrone.cam.targetTexture = (RenderTexture)camFeed.texture;
            }
            ToggleControlMode(true);
        }

        public void MoveCameraUp()
        {
            if (currentDrone != null && currentDrone.active && currentDrone.type == Drone40.DroneType.Camera)
            {
                currentDrone.MoveCamera(Drone40.CameraDirections.Up);
            }
        }

        public void MoveCameraDown()
        {
            if (currentDrone != null && currentDrone.active && currentDrone.type == Drone40.DroneType.Camera)
            {
                currentDrone.MoveCamera(Drone40.CameraDirections.Down);
            }
        }

        public void MoveCameraLeft()
        {
            if (currentDrone != null && currentDrone.active && currentDrone.type == Drone40.DroneType.Camera)
            {
                currentDrone.MoveCamera(Drone40.CameraDirections.Left);
            }
        }

        public void MoveCameraRight()
        {
            if (currentDrone != null && currentDrone.active && currentDrone.type == Drone40.DroneType.Camera)
            {
                currentDrone.MoveCamera(Drone40.CameraDirections.Right);
            }
        }

        public static void CallUpdateList()
        {
            OnUpdateList?.Invoke();
        }

        public void ToggleLight()
        {
            lightActive = !lightActive;
            if (lightActive) lightBulb.color = lightActiveColor;
            else lightBulb.color = lightDisabledColor;
            currentDrone.ToggleLight(lightActive);
        }

        public void Detonate()
        {
            GoToSelection();
        }

        public void ArmAndDrop()
        {
            currentDrone.Drop();
            GoToSelection();
        }

        public delegate void UpdateListDelegate();
        public static event UpdateListDelegate OnUpdateList;
    }
}
