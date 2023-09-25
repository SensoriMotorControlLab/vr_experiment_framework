using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UXF;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;
using UnityEngine.InputSystem.XR;

public class CursorController : MonoBehaviour
{
    // The visible representation of the cursor. A blue sphere
    public GameObject Model;
    public bool triggerUp;
    // References to the left and right hand positions
    public GameObject LeftHand, RightHand;
    private GameObject leftHandModel, rightHandModel;
    private GameObject leftHandCollider, rightHandCollider;
    private GameObject vrCamera;
    private Vector3 previousPosition;
    private bool prevTrigger;
    public bool UseVR;
    public bool useHand = true;

    /// <summary>
    /// Whether or not the cursor is visible
    /// </summary>
    public bool CursorVisible { get; private set; }
    /// <summary>
    /// The current task's hand. Either "l" or "r"
    /// </summary>
    public string CurrentTaskHand { get; private set; }
    /// <summary>
    /// Get the left hand device
    /// </summary>
    public InputDevice LeftHandDevice { get; private set; }
    /// <summary>
    /// Get the right hand device
    /// </summary>
    public InputDevice RightHandDevice { get; private set; }
    /// <summary>
    /// How long the cursor has been still for
    /// </summary>
    public float stillTime { get; private set; }
    /// <summary>
    /// How far the cursor is from the home position
    /// </summary>
    public float DistanceFromHome { get; private set; }

    public enum MovementType
    {
        aligned,
        rotated,
        clamped
    }

    public MovementType MoveType { get; private set; }

    void Start()
    {
        // For oculus
        leftHandModel = LeftHand.transform.Find("left_touch_controller_model_skel").gameObject;
        rightHandModel = RightHand.transform.Find("right_touch_controller_model_skel").gameObject;

        leftHandCollider = LeftHand.transform.Find("LeftHandCollider").gameObject;
        rightHandCollider = RightHand.transform.Find("RightHandCollider").gameObject;

        vrCamera = GameObject.Find("Main Camera");
    }

    void Update()
    {
        if (!RightHandDevice.isValid)
        {

            List<InputDevice> devices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right, devices);
            if (devices.Count > 0)
            {
                RightHandDevice = devices[0];
            }
            else
            {
                Debug.LogWarning("Right device not detected.");
            }

        }

        if (!LeftHandDevice.isValid)
        {

            List<InputDevice> devices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left, devices);
            if (devices.Count > 0)
            {
                LeftHandDevice = devices[0];
                Debug.Log("Found Left Device: " + LeftHandDevice);
            }
            else
            {
                Debug.LogWarning("Left device not detected.");
            }
        }
    }
    /// <summary>
    /// Returns true if the trigger is down on the current task's hand
    /// </summary>
    /// <returns></returns>
    public bool IsTriggerDown()
    {
        return IsTriggerDown(CurrentTaskHand);
    }

    /// <summary>
    /// Returns true if the trigger is down on the specified hand
    /// </summary>
    /// <param name="hand"></param>
    /// <returns></returns>
    public bool IsTriggerDown(String hand)
    {
        return hand == "l"
            ? LeftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftVal) && leftVal
            : RightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightVal) && rightVal;
    }

    /// <summary>
    /// Returns the 2D axis of the joystick on the current task's hand
    /// </summary>
    /// <returns></returns>
    public Vector2 Get2DAxis()
    {
        return CurrentTaskHand == "l"
            ? RightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 left2DAxis) ? left2DAxis : Vector2.zero
            : LeftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 right2DAxis) ? right2DAxis : Vector2.zero;
    }

    /// <summary>
    /// Returns true if the trigger is up on the current task's hand
    /// </summary>
    /// <returns></returns>
    public bool IsTriggerReleased()
    {
        return IsTriggerReleased(CurrentTaskHand);
    }

    /// <summary>
    /// Returns true if the trigger is up on the specified hand
    /// </summary>
    /// <param name="hand"></param>
    /// <returns></returns>
    public bool IsTriggerReleased(String hand)
    {
        return hand == "l"
            ? LeftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftVal) && leftVal && prevTrigger
            : RightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightVal) && rightVal && prevTrigger;
    }

    /// <summary>
    /// Returns the velocity of the current task's hand
    /// </summary>
    /// <returns></returns>
    public Vector3 GetVelocity()
    {
        return GetVelocity(CurrentTaskHand);
    }

    /// <summary>
    /// Returns the velocity of the specified hand
    /// </summary>
    /// <param name="hand"></param>
    /// <returns></returns>
    public Vector3 GetVelocity(String hand)
    {
        return hand == "l"
            ? LeftHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 leftVel) ? leftVel : Vector3.zero
            : RightHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 rightVel) ? rightVel : Vector3.zero;
    }

    /// <summary>
    /// Skip the the menu is joystick on left hand is clicked
    /// </summary>
    /// <returns></returns>
    public bool MenuSkip()
    {
        return CurrentTaskHand == "l"
            ? RightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool rightVal) && rightVal
            : LeftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool leftVal) && leftVal;
    }

    /// <summary>
    /// Returns the GameObject that represents the hand involved in the current trial
    /// </summary>
    public GameObject GetCurrentHand()
    {
        return CurrentTaskHand == "l" ? LeftHand : RightHand;
    }

    /// <summary>
    /// Returns the GameObject that represents the current task's hand collider
    /// </summary>
    /// <returns></returns>
    public GameObject GetCurrentCollider()
    {
        return CurrentTaskHand == "l" ? leftHandCollider : rightHandCollider;
    }

    /// <summary>
    /// Sets up all properties pertaining to the cursor and hand.
    /// Run by the OnTrialBegin event by UXF
    /// </summary>
    public void SetupHand(Trial trial)
    {
        switch (trial.settings.GetString("per_block_hand"))
        {
            case "r":
                CurrentTaskHand = "r";
                break;
            case "l":
                CurrentTaskHand = "l";
                break;
            default:
                Debug.LogWarning("\"per_block_hand\" is not 'l' or 'r'. Check the JSON.");
                break;
        }
    }
    /// <summary>
    /// Sets the movement type of the cursor
    /// </summary>
    /// <param name="moveType"></param>
    public void SetMovementType(MovementType moveType)
    {
        MoveType = moveType;
    }

    /// <summary>
    /// Sets the visibility of the cursor
    /// </summary>
    public void SetCursorVisibility(bool visible)
    {
        Model.GetComponent<MeshRenderer>().enabled = CursorVisible = visible;
    }

    /// <summary>
    /// Sets the visibility of the real hands
    /// </summary>
    public void SetHandVisibility(bool visible)
    {
        leftHandModel.SetActive(visible);
        rightHandModel.SetActive(visible);
    }

    // LateUpdate is called last once per frame
    void LateUpdate()
    {
        if (ExperimentController.Instance().CurrentTask == null) return;

        Vector3 realHandPosition = GetHandPosition();
        transform.position = ConvertPosition(realHandPosition);

        if ((previousPosition - realHandPosition).magnitude > 0.0005f)
            stillTime = 0f;
        else
            stillTime += Time.deltaTime;

        previousPosition = realHandPosition;

        if (ExperimentController.Instance().CurrentTask.Home != null)
        {
            DistanceFromHome =
                (transform.position - ExperimentController.Instance().CurrentTask.Home.transform.position).magnitude;
        }
        else
        {
            DistanceFromHome = -1f;
        }

        prevTrigger = IsTriggerDown(CurrentTaskHand);
    }

    /// <summary>
    /// Returns the position of the current task's hand collider gameObject or mouse cursor
    /// </summary>
    /// <returns></returns>
    public Vector3 GetHandPosition()
    {
        if (UseVR)
        {
            return CurrentTaskHand == "l"
            ? leftHandCollider.transform.position
            : rightHandCollider.transform.position;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning("make sure your camera is tagged as the Main camera");
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z));
        return new Vector3(mousePos.x, ExperimentController.Instance().transform.position.y, mousePos.z);
    }
    /// <summary>
    /// Returns the position of the current task's hand based on the VR controller
    /// </summary>
    /// <returns></returns>
    public Vector3 GetRawHandPosition()
    {
        return CurrentTaskHand == "l"
            ? LeftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPos) ? leftPos : Vector3.zero
            : RightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos) ? rightPos : Vector3.zero;
    }

    /// <summary>
    /// Converts the user's hand location into the transformed cursor location
    /// </summary>
    /// <returns></returns>
    public Vector3 ConvertPosition(Vector3 position)
    {
        ExperimentController ctrler = ExperimentController.Instance();

        // Get home position. Returns Vector3.zero when task doesn't use a home position
        Vector3 home = ctrler.CurrentTask.Home != null ?
            ctrler.CurrentTask.Home.transform.position : Vector3.zero;

        switch (MoveType)
        {
            case MovementType.aligned:
                return position;
            case MovementType.rotated:

                if (ctrler.Session.CurrentBlock.settings.GetString("per_block_rotation") is string)
                {
                    float angle = ctrler.CurrentTask.GetRotation();
                    return Quaternion.Euler(0, -angle, 0) * (position - home) + home;
                }
                else
                {
                    float angle = ctrler.Session.CurrentTrial.settings.GetFloat("per_block_rotation");

                    return Quaternion.Euler(0, -angle, 0) * (position - home) + home;
                }

            case MovementType.clamped:
                // Get vector between home position and target
                Vector3 target = ctrler.CurrentTask.Target.transform.position;
                Vector3 normal = target - home;

                // Rotate vector by 90 degrees to get plane parallel to the vector
                normal = Quaternion.Euler(0f, -90f, 0f) * normal;

                //  o   < target
                //  |
                // -|   < normal
                //  |
                //  x   < home / center of experiment

                // Project position using this new vector as the plane normal
                return Vector3.ProjectOnPlane(position - home, normal.normalized) + home;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Maps the mouse cursor position to the plane's Y coordinate. Used for non-VR tasks
    /// <param name="planeNormal"></param>
    /// <param name="planePos"></param>
    /// <param name="camera"></param>
    /// <returns></returns>
    public Vector3 MouseToPlanePoint(Vector3 planeNormal, Vector3 planePos, Camera camera)
    {
        // If the camera is orthographic, it is a top down view and thus x and z are just screen coordinates
        if (camera.orthographic)
        {
            Vector3 mouseCoords = camera.ScreenToWorldPoint(Input.mousePosition);
            return new Vector3(mouseCoords.x, planePos.y, mouseCoords.z);
        }

        Vector3 pos = camera.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x, Input.mousePosition.y, camera.nearClipPlane));

        Vector3 direction = (pos - camera.transform.position).normalized;

        Plane plane = new Plane(planeNormal.normalized, planePos);
        Ray r = new Ray(camera.transform.position, direction);

        return plane.Raycast(r, out float enter) ? r.GetPoint(enter) : Vector3.zero;
    }

    /// <summary>
    /// Maps the controller position to the plane's Y coordinate. Used for VR tasks
    /// </summary>
    /// <param name="planeNormal"></param>
    /// <param name="planePos"></param>
    /// <param name="ctrller"></param>
    /// <returns></returns>
    public Vector3 ControllerToPlanePoint(Vector3 planeNormal, Vector3 planePos, Vector3 ctrller)
    {
        Plane plane = new Plane(planeNormal.normalized, planePos);
        Ray ray;
        if (ctrller.y <= planePos.y)
        {
            ray = new Ray(ctrller, Vector3.up);
        }
        else
        {
            ray = new Ray(ctrller, Vector3.down);
        }

        return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
    }

    /// <summary>
    /// Enable or disable the VR Camera. Required for experiments with a specific camera
    /// </summary>
    /// <param name="state"></param>
    public void SetVRCamera(bool state)
    {
        if (state)
        {
            vrCamera.SetActive(true);
            vrCamera.GetComponent<TrackedPoseDriver>().enabled = true;
        }
        else
        {
            vrCamera.SetActive(false);
        }
    }

    /// <summary>
    /// Vibrates the controller if the device supports it.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="amplitude"></param>
    /// <param name="duration"></param>
    /// <param name="devices"></param>
    /// <returns></returns>
    public bool VibrateController(uint channel, float amplitude, float duration, List<InputDevice> devices)
    {
        foreach (var device in devices)
        {
            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    return device.SendHapticImpulse(channel, amplitude, duration);
                }
            }
        }

        return false;
    }
}
