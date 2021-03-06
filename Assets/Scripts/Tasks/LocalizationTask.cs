using System;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class LocalizationTask : BaseTask
{
    private GameObject[] targets = new GameObject[3];
    private GameObject localizer; // Cursor that indicates where the user's head is gazing

    private Trial trial;

    private GameObject localizationCam;
    private GameObject localizationSurface;
    private GameObject localizationPrefab;
    protected AudioSource sound;

    protected List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    float locZ;

    // Angle of the localizer along the arc, in non-vr mode
    public float locX = 0;
    private float localizerSpeed2D = 0.0005f;
    private Vector3 localizerPos = new Vector3(0, 0, 0);

    ExperimentController ctrler;

    public void LateUpdate()
    {
        ExperimentController ctrler = ExperimentController.Instance();


        switch (currentStep)
        {
            // When the user holds their hand and they are outside the home, begin the next phase of localization
            case 2 when ctrler.CursorController.PauseTime > 0.5f && 
                        ctrler.CursorController.DistanceFromHome > 0.01f:
                IncrementStep();
                break;
            case 3: 
                // VR: User uses their head to localize their hand
                // Non-VR: User uses horizontal axis to localize their mouse

                if (ctrler.Session.settings.GetObjectList("optional_params").Contains("vr")) // if in vr
                {
                    // raycasts from camera to set localizer position
                    Plane plane = new Plane(Vector3.down, ctrler.transform.position.y);
                    Ray r = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                    if (ctrler.Session.settings.GetObjectList("optional_params").Contains("localize_via_gaze")){
                        if (plane.Raycast(r, out float hit))
                            localizer.transform.position = r.GetPoint(hit);
                    }
                    else{
                        if(ctrler.CursorController.Get2DAxis().magnitude > 0)
                            locX += ctrler.CursorController.Get2DAxis().x * 0.002f;
                            locZ += ctrler.CursorController.Get2DAxis().y * 0.002f;
                            localizer.transform.position = new Vector3(locX, 0, locZ) + targets[2].transform.position;
                    }              
                }
                else
                {
                    
                    // A/D keys, left/right arrow keys, or gamepad joystick as input
                    locX += Input.GetAxisRaw("Horizontal") * localizerSpeed2D;
                    locZ += Input.GetAxisRaw("Vertical") * localizerSpeed2D;
                    locX = Mathf.Clamp(locX, -90f, 90f); 

                    float angle = locX * Mathf.Deg2Rad;

                    // centre == centre of Arc == centre of Home
                    Vector3 centre = Target.transform.position;

                    // distance from home: copied from ArcTarget script, multiplied by the size of the arc
                    float distance = (Target.GetComponent<ArcScript>().TargetDistance + centre.z) * Target.transform.localScale.x;

                    // find position along arc
                    Vector3 newPos = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distance;
                    newPos += centre;

                    localizer.transform.position = new Vector3(locX, 0, locZ) + newPos;
                }

                // switch from click to enter or something? issue with clicking to refocus window

                // If the user presses the trigger associated with the hand, we end the trial
                if (ctrler.CursorController.IsTriggerDown(ctrler.CursorController.CurrentTaskHand) || Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Space))
                    IncrementStep();

                break;
        }

        if (Finished)
            ctrler.EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        ExperimentController ctrler = ExperimentController.Instance();
        UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);

        switch (currentStep)
        {
            case 0: // Enter dock
                targets[0].SetActive(false);
                Home.SetActive(true);
                break;
            case 1: // Enter home
                Home.SetActive(false);

                ctrler.StartTimer();

                // Create tracker objects
                ctrler.AddTrackedPosition("hand_path",
                    ctrler.Session.CurrentTrial.settings.GetString("per_block_hand") == "l"
                        ? ctrler.CursorController.LeftHand
                        : ctrler.CursorController.RightHand);

                ctrler.AddTrackedPosition("cursor_path", ctrler.CursorController.gameObject);

                Target.SetActive(true);

                ExperimentController.Instance().CursorController.SetCursorVisibility(false);

                break;
            case 2: // Pause in arc
                localizer.SetActive(true);
                Target.GetComponent<ArcScript>().Expand();
                sound.Play();
                if (ctrler.Session.settings.GetObjectList("optional_params").Contains("vr"))
                    VibrateController(0, 0.34f, 0.15f, devices);

                break;
            case 3: // Select the spot they think their real hand is
                Target.SetActive(false);

                // We use the target variable to store the cursor position
                Target.transform.position =
                    ExperimentController.Instance().CursorController.GetHandPosition();

                break;
        }

        base.IncrementStep();
        return finished;
    }

    public override void LogParameters()
    {
        // Store where they think their hand is
        ExperimentController.Instance().LogObjectPosition("loc", localizer.transform.localPosition);
    }

    public override void Setup()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        maxSteps = 4;

        ctrler.CursorController.SetHandVisibility(false);
        Cursor.visible = false;
        ctrler.CursorController.SetCursorVisibility(true);

        localizationPrefab = Instantiate(ctrler.GetPrefab("LocalizationPrefab"));
        localizationPrefab.transform.SetParent(ctrler.transform);
        localizationPrefab.transform.localPosition = Vector3.zero;

        localizationCam = GameObject.Find("LocalizationCamera");
        localizationSurface = GameObject.Find("Surface");
        

        // Set up the dock position
        targets[0] = GameObject.Find("Dock");
        targets[0].transform.position = ctrler.TargetContainer.transform.position;
        //targets[0].transform.position = new Vector3(ctrler.TargetContainer.transform.position.x, -0.250f, ctrler.TargetContainer.transform.position.z);

        // Set up the home position
        targets[1] = GameObject.Find("Home");
        targets[1].transform.position = ctrler.TargetContainer.transform.position;
        //targets[1].transform.position = new Vector3(ctrler.TargetContainer.transform.position.x, -0.250f, ctrler.TargetContainer.transform.position.z) + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        Home = targets[1];

        // Grab an angle from the list and then remove it
        float targetAngle = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_targetListToUse"));

        // Set up the arc object
        targets[2] = GameObject.Find("ArcTarget");
        targets[2].transform.rotation = Quaternion.Euler(
            0f,
            -targetAngle + 90f,
            0f);

        targets[2].transform.position = targets[1].transform.position;

        targets[2].GetComponent<ArcScript>().TargetDistance = ctrler.Session.CurrentTrial.settings.GetFloat("per_block_distance");
        targets[2].GetComponent<ArcScript>().Angle = targets[2].transform.rotation.eulerAngles.y;
        //targets[2].transform.localScale = Vector3.one;
        Target = targets[2];
        sound = targets[2].GetComponent<AudioSource>();

        // Set up the GameObject that tracks the user's gaze
        localizer = GameObject.Find("Localizer");
        localizer.GetComponent<SphereCollider>().enabled = false;
        localizer.GetComponent<BaseTarget>().enabled = false;
        localizer.SetActive(false);
        

        localizer.transform.SetParent(ctrler.TargetContainer.transform);
        localizer.name = "Localizer";

        Target.SetActive(false);


        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetObjectList("optional_params").Contains("vr"))
        {
            localizationSurface.SetActive(false);
            localizationCam.SetActive(false);
            ctrler.CursorController.UseVR = true;
        }
        else
        {
            ctrler.CursorController.SetVRCamera(false);
        }
    }

    public override void Disable()
    {
        // TODO: reenable camera for VR

        foreach (GameObject g in targets)
            g.SetActive(false);

        localizer.SetActive(false);
    }

    protected override void OnDestroy()
    {
        foreach (GameObject g in targets)
             Destroy(g);

        Destroy(localizer);

        Destroy(localizationPrefab);

        //base.OnDestroy();
    }
}
