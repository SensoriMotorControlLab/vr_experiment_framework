using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UXF;

public class LocalizationTask : BaseTask
{
    private GameObject[] targets = new GameObject[3];
    // dock distance from Home
    protected float dock_dist = 0.025f;
    protected float req_targ_accuracy = 0.005f;
    private Trial trial;


    private GameObject localizationCam;
    private GameObject localizationSurface;
    private GameObject localizationPrefab;
    private GameObject arcError;
    protected AudioSource sound;

    protected List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    float locZ;

    // Angle of the localizer along the arc, in non-vr mode
    public float locX = 0;
    private float localizerSpeed2D = 0.0005f;
    private Vector3 localizerPos = new Vector3(0, 0, 0);

    ExperimentController ctrler;

    private GameObject locAim; // Cursor that indicates where the user's head is gazing
    private GameObject locAngle;

    public override void Setup()
    {

        maxSteps = 4;
        ctrler = ExperimentController.Instance();

        ctrler.CursorController.SetHandVisibility(false);
        Cursor.visible = false;
        ctrler.CursorController.SetCursorVisibility(true);

        localizationPrefab = Instantiate(ctrler.GetPrefab("LocalizationPrefab"));
        
        localizationPrefab.transform.position = Vector3.zero;
        localizationPrefab.transform.SetParent(ctrler.transform);
        ctrler.TargetContainer.transform.position = Vector3.zero;

        localizationCam = GameObject.Find("LocalizationCamera");
        localizationSurface = GameObject.Find("Surface");
        arcError = GameObject.Find("ArcError");


        // Set up the dock position
        targets[0] = GameObject.Find("Dock");
        targets[0].transform.localPosition = ctrler.TargetContainer.transform.position - ctrler.transform.forward * dock_dist;
        //targets[0].transform.position = new Vector3(ctrler.TargetContainer.transform.position.x, -0.250f, ctrler.TargetContainer.transform.position.z);

        // Set up the home position
        targets[1] = GameObject.Find("Home");
        targets[1].transform.localPosition = ctrler.TargetContainer.transform.position;
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
        locAim = GameObject.Find("Localizer");
        locAngle = GameObject.Find("locAngle");
        
        locAim.GetComponent<SphereCollider>().enabled = false;
        locAim.GetComponent<BaseTarget>().enabled = false;
        locAim.SetActive(false);

        locAim.transform.position =locAim.transform.position + Vector3.forward * 12.3f/100f;
        
        locAim.transform.SetParent(ctrler.TargetContainer.transform);

        Target.SetActive(false);

        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetObjectList("optional_params").Contains("vr"))
        {
            localizationSurface.SetActive(true);
            localizationCam.SetActive(false);
            ctrler.CursorController.UseVR = true;
        }
        else
        {
            ctrler.CursorController.SetVRCamera(false);
        }
        arcError.SetActive(false);
    }

    public override void Update()
    {
        
        base.Update();

        // Debug.Log(ctrler.CursorController.transform.localPosition.z);

        switch (currentStep)
        {
            case 0:
                targets[0].SetActive(true);
                if (!ctrler.Session.settings.GetObjectList("optional_params").Contains("return_visible"))
                {
                    // make the ball invisible
                    ctrler.CursorController.Model.GetComponent<Renderer>().enabled = false;
                }
                if (Mathf.Abs(targets[0].transform.position.magnitude - ctrler.CursorController.transform.position.magnitude) < req_targ_accuracy
                                && ctrler.CursorController.stillTime > 0.1f)
                {
                    ctrler.CursorController.Model.GetComponent<Renderer>().enabled = true;
                    IncrementStep();
                }
                break;
            case 1:
                if (Mathf.Abs(targets[1].transform.position.magnitude - ctrler.CursorController.transform.position.magnitude) < req_targ_accuracy
                              && ctrler.CursorController.stillTime > 0.3f)
                {
                    IncrementStep();
                }
                break;
            // When the user holds their hand and they are outside the home, begin the next phase of localization
            case 2:
                if(ctrler.CursorController.stillTime > 0.5f &&
                        ctrler.CursorController.DistanceFromHome > 0.1f && ctrler.CursorController.transform.position.z > 0 
                        && ctrler.CursorController.DistanceFromHome < 0.125f){
                            IncrementStep();
                        }
                if(ctrler.CursorController.DistanceFromHome > 0.125f && ctrler.CursorController.transform.position.z > 0){
                    arcError.SetActive(true);
                    arcError.GetComponent<ArcScript>().TargetDistance = ctrler.CursorController.DistanceFromHome * 100;
                    arcError.GetComponent<ArcScript>().GenerateArc();
                    
                }
                else{
                    arcError.SetActive(false);
                }

                
                
                break;
            case 3:
                // VR: User uses their head to localize their hand
                // Non-VR: User uses horizontal axis to localize their mouse

                if (ctrler.Session.settings.GetObjectList("optional_params").Contains("vr")) // if in vr
                {
                    // raycasts from camera to set localizer position
                    Plane plane = new Plane(Vector3.down, ctrler.transform.position.y);
                    Ray r = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                    if (ctrler.Session.settings.GetObjectList("optional_params").Contains("localize_via_gaze"))
                    {
                        if (plane.Raycast(r, out float hit))
                            locAim.transform.position = r.GetPoint(hit);
                    }
                    else
                    {
                        if (ctrler.CursorController.Get2DAxis().magnitude > 0)
                            locX = ctrler.CursorController.Get2DAxis().x * 20;
                            RotateLocalizer(locX);
                        // locZ += ctrler.CursorController.Get2DAxis().y * 0.002f;
                        // localizer.transform.position = new Vector3(locX, 0, locZ) + targets[2].transform.position;
                    }
                }
                else
                {
                    // A/D keys, left/right arrow keys, or gamepad joystick as input
                    locX = Input.GetAxisRaw("Horizontal") * 20;
                    RotateLocalizer(locX);
                    
                }

                // switch from click to enter or something? issue with clicking to refocus window

                // If the user presses the trigger associated with the hand, we end the trial
                if (ctrler.CursorController.IsTriggerDown("l") || Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Space))
                    IncrementStep();

                break;
        }

        if (Finished)
            ctrler.EndAndPrepare();

        if (Input.GetKeyDown(KeyCode.C))
        {
            Centre();
        }
    }

    private void RotateLocalizer(float locX)
    {
        //ctrler.TargetContainer.transform.position =new Vector3 (targets[1].transform.position.x, ctrler.TargetContainer.transform.position.y, targets[1].transform.position.z);
        if (ctrler.TargetContainer.transform.rotation.eulerAngles.y < 90){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
        else if(ctrler.TargetContainer.transform.rotation.eulerAngles.y < 95 && locX < 0){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
        else if (ctrler.TargetContainer.transform.rotation.eulerAngles.y > 270){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
        else if (ctrler.TargetContainer.transform.rotation.eulerAngles.y > 265 && locX > 0){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
    }

    /// <summary>
    /// Centres the experiment a little in front of the hand position
    /// Distance forward is determined by the dock distance
    /// </summary>
    protected void Centre()
    {
        //ctrler.TargetContainer.transform.localPosition = Vector3.zero;
        Vector3 pos = targets[0].transform.position;
        Vector3 centre = pos - ctrler.transform.forward * 0.1f;
        ctrler.CentreExperiment(centre);
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
                locAim.SetActive(true);
                //Target.GetComponent<ArcScript>().Expand();
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
        ExperimentController.Instance().LogObjectPosition("loc", locAim.transform.localPosition);
    }

    public override void Disable()
    {
        // TODO: reenable camera for VR

        foreach (GameObject g in targets)
            g.SetActive(false);

        locAim.SetActive(false);
    }

    protected override void OnDestroy()
    {
        foreach (GameObject g in targets)
            Destroy(g);

        Destroy(locAim);

        Destroy(localizationPrefab);

        //base.OnDestroy();
    }
}
