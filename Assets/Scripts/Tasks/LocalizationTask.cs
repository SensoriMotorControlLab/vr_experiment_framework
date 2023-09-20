
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
    private GameObject localizationCam;
    private GameObject localizationSurface;
    private GameObject localizationPrefab;
    private GameObject arcError;
    private GameObject arcRotation;
    protected AudioSource sound;
    protected GameObject pen;
    float localizerLoc = 0;
    int arcSpan = 0;
    float ArcRot = 0;
    protected List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    // Angle of the localizer along the arc, in non-vr mode
    public float locX = 0;
    ExperimentController ctrler;
    private GameObject locAim; // Cursor that indicates where the user's head is gazing
    private GameObject locAngle;
    Vector3 homePos = new Vector3(0, 0, 0);
    List<Vector4> handPos = new List<Vector4>();
    List<Vector4> cursorPos = new List<Vector4>();
    List<Vector4> indicatorPos = new List<Vector4>();
    List<Vector4> penPos = new List<Vector4>();
    Vector2 pos_3cm_out = new Vector2(0, 0);
    Vector2 cursor_3cm_out = new Vector2(0, 0);
    Vector2 pen_3cm_out = new Vector2(0, 0);
    Vector2 pos_2cm_out = new Vector2(0, 0);
    Vector2 cursor_2cm_out = new Vector2(0, 0);
    Vector2 pen_2cm_out = new Vector2(0, 0);
    Vector2 arcAquired = new Vector2(0, 0);
    Vector2 localizingEvent = new Vector2(0, 0);
    bool outEvent_3cm = true;
    bool outEvent_2cm = true;
    float targetAngle = 0;
    GameObject baseObject;
    float pressedTime = 0;
    GameObject activeCursor;
    Vector2 handPos2D = new Vector2(0, 0);
    float finalReachAngle;
    float finalCursorAngle;
    float finalPenAngle;
    Vector3 locPos;
    GameObject office;
    GameObject lab;
    float penHeight;
    GameObject dummyDock;
    GameObject audioCue;
    
    public override void Setup()
    {

        maxSteps = 4;
        ctrler = ExperimentController.Instance();

        ctrler.CursorController.SetHandVisibility(false);
        Cursor.visible = false;

        localizationPrefab = Instantiate(ctrler.GetPrefab("LocalizationPrefab"));
        
        localizationPrefab.transform.position = new Vector3(0, 0, 0);
        localizationPrefab.transform.SetParent(ctrler.transform);
        ctrler.TargetContainer.transform.position = new Vector3(0, 0.06f, 0);

        localizationCam = GameObject.Find("LocalizationCamera");
        localizationSurface = GameObject.Find("Surface");
        arcError = GameObject.Find("ArcError");
        baseObject = GameObject.Find("BaseObject");
        pen = GameObject.Find("Pen");
        arcRotation = GameObject.Find("ArcRotation");
        targets[2] = GameObject.Find("ArcTarget");
        locAim = GameObject.Find("Localizer");
        locAngle = GameObject.Find("locAngle");
        dummyDock = GameObject.Find("DummyDock");

        if (ctrler.Session.settings.GetObjectList("optional_params").Contains("cursor") && ctrler.Session.CurrentTrial.settings.GetBool("per_block_cursor_visible")){
            ctrler.CursorController.SetCursorVisibility(true);
        }
        else{
            ctrler.CursorController.SetCursorVisibility(false);
        }

        Vector3 tempPos;
        tempPos =new Vector3 (ctrler.TargetContainer.transform.position.x, ctrler.TargetContainer.transform.position.y, ctrler.TargetContainer.transform.position.z);
        if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
            pen.SetActive(true);
            baseObject.GetComponent<Renderer>().enabled = false;
            activeCursor = pen;
            pen.transform.localEulerAngles = new Vector3(0, -165, -15);
            office = Instantiate(ctrler.GetPrefab("office"), new Vector3(-3.4f, -0.71f, 8.1f), Quaternion.Euler(0, 180, 0));
            office.SetActive(true);
            office.transform.parent = localizationPrefab.transform; 
            penHeight = Mathf.Abs(pen.transform.position.y - pen.transform.GetChild(0).transform.position.y);
            localizationSurface.GetComponent<Renderer>().material = ctrler.Materials["wood"];
            localizationSurface.transform.localScale = new Vector3(1.22f, 0.1f, 1.9f);
            audioCue = GameObject.Find("pen_to_target");
            // audioCue.GetComponent<AudioSource>().Play();
        }
        else{
            pen.SetActive(false);
            baseObject.GetComponent<Renderer>().enabled = true;
            activeCursor = baseObject;
            lab = Instantiate(ctrler.GetPrefab("room"), new Vector3(-0.13f, 0.16f, 0.218f), Quaternion.identity);
            lab.SetActive(true);
            lab.transform.parent = localizationPrefab.transform;
            localizationSurface.transform.localScale = new Vector3(4f, 0.1f, 4f);
            audioCue = GameObject.Find("thumb_to_target");
            // audioCue.GetComponent<AudioSource>().Play();
        }

        // Set up the home position
        targets[1] = GameObject.Find("Home");
        targets[1].transform.localPosition = tempPos + ctrler.transform.forward * 0.07f;
        //targets[1].transform.position = new Vector3(ctrler.TargetContainer.transform.position.x, -0.250f, ctrler.TargetContainer.transform.position.z) + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        Home = targets[1];
        homePos = targets[1].transform.position;

        // Set up the dock position
        targets[0] = GameObject.Find("Dock");
        targets[0].transform.position = new Vector3(0, localizationPrefab.transform.position.y, 0) - ctrler.transform.forward * dock_dist;
        targets[0].GetComponent<MeshRenderer>().enabled = false;
        dummyDock.transform.position = new Vector3 (targets[0].transform.position.x, ctrler.TargetContainer.transform.position.y, targets[0].transform.position.z);
        // Grab an angle from the list and then remove it
        targetAngle = Convert.ToSingle(ctrler.PseudoRandom("per_block_targetListToUse"));
        baseObject.GetComponent<Renderer>().enabled = false;
        // Set up the arc object
        
        
        Debug.Log("Target Angle: " + targetAngle);
        arcRotation.transform.position = targets[1].transform.position;
        targets[2].GetComponent<ArcScript>().TargetDistance = ctrler.Session.CurrentTrial.settings.GetFloat("per_block_distance");
        Target = targets[2];
        sound = targets[2].GetComponent<AudioSource>();

        arcError.transform.position = targets[1].transform.position;

        // Set up the GameObject that tracks the user's gaze
       
        
        locAim.GetComponent<SphereCollider>().enabled = false;
        locAim.GetComponent<BaseTarget>().enabled = false;
        locAim.SetActive(false);

        locAim.transform.position =new Vector3(locAim.transform.position.x, ctrler.TargetContainer.transform.position.y, locAim.transform.position.z) + Vector3.forward * ctrler.Session.CurrentTrial.settings.GetFloat("per_block_distance")/100f;
        ctrler.TargetContainer.transform.rotation = Quaternion.Euler(0, 0, 0);       
        locAim.transform.SetParent(ctrler.TargetContainer.transform);
        ctrler.TargetContainer.transform.position = ctrler.TargetContainer.transform.position +  ctrler.TargetContainer.transform.forward * 7f/100f;

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
            localizationPrefab.transform.localPosition = Vector3.zero;
            // ctrler.TargetContainer.transform.localPosition = Vector3.zero;

        }
        arcError.SetActive(false);
    }

    protected virtual Vector3 GetMousePoint(Transform ball)
    {
        //ToFix: can the below two be one function called point to planepoint?
        Vector3 ctrl = new Vector3(ctrler.CursorController.GetHandPosition().x, 0, ctrler.CursorController.GetHandPosition().z);
        localizationSurface = GameObject.Find("Surface");

            return ctrler.CursorController.ControllerToPlanePoint(
                        localizationSurface.transform.up * ball.position.y,
                        ball.position,
                        ctrl);
    }

    void PenFollowMouse()
    {
        pen.transform.position = new Vector3(baseObject.transform.position.x, ctrler.TargetContainer.transform.position.y + penHeight, baseObject.transform.position.z);
        pen.transform.localEulerAngles = new Vector3(0, -165, -15);
        locPos = pen.transform.GetChild(0).transform.position;
        Vector3 tempPenPos = pen.transform.GetChild(0).transform.position;
        if(currentStep != 0 && currentStep != 1){
            penPos.Add(new Vector4(tempPenPos.x, tempPenPos.y, tempPenPos.z, Time.time));
        }
        
    }

    public override void Update()
    {
        
        Vector3 tempHandPos;
        Vector3 tempCursorPos;
        baseObject = GameObject.Find("BaseObject");
        pen = GameObject.Find("Pen");
        localizationSurface = GameObject.Find("Surface");
        handPos2D = new Vector2(ctrler.CursorController.transform.position.x, ctrler.CursorController.transform.position.z);
        
        if(ctrler.CursorController.IsTriggerDown("l") || Input.GetKey(KeyCode.S)){
            pressedTime += Time.deltaTime;
        }
        else{
            pressedTime = 0;
        }

        if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
            baseObject.GetComponent<Renderer>().enabled = false;
            activeCursor = pen;
            PenFollowMouse();
            localizationSurface.GetComponent<Renderer>().material = ctrler.Materials["wood"];
            localizationSurface.transform.localScale = new Vector3(1.22f, 0.1f, 1.9f);
        }
        else{    
            locPos = baseObject.transform.position;
            activeCursor = baseObject;
            localizationSurface.transform.localScale = new Vector3(4f, 0.1f, 4f);
        }
        Vector3 temp = ctrler.CursorController.GetHandPosition();
        switch (currentStep)
        {
            case 0:
                Vector3 mousePoint = GetMousePoint(baseObject.transform);
                Vector3 mousePlane = new Vector3(ctrler.CursorController.Model.transform.position.x, mousePoint.y, ctrler.CursorController.Model.transform.position.z);
                baseObject.transform.position = ctrler.CursorController.ConvertPosition(mousePlane);
                
                targets[0].SetActive(true);
                if (!ctrler.Session.settings.GetObjectList("optional_params").Contains("return_visible"))
                {
                    // make the ball invisible
                    activeCursor.GetComponent<Renderer>().enabled = false;
                }
                else
                {
                    activeCursor.GetComponent<Renderer>().enabled = true;
                }
                Vector2 tempDockPos = new Vector2(targets[0].transform.position.x, targets[0].transform.position.z);
                Vector2 tempHandPos2D = new Vector2(baseObject.transform.position.x, baseObject.transform.position.z);
                if ((tempDockPos - tempHandPos2D).magnitude < 0.009f
                                && ctrler.CursorController.stillTime > 0.3f)
                {
                    activeCursor.GetComponent<Renderer>().enabled = true;
                    IncrementStep();
                }
                break;
            case 1:
                baseObject.transform.position = ctrler.CursorController.ConvertPosition(new Vector3 (temp.x, ctrler.TargetContainer.transform.position.y, temp.z));
                Vector2 tempHome = new Vector2(targets[1].transform.position.x, targets[1].transform.position.z);
                Vector2 tempLocPos = new Vector2(locPos.x, locPos.z);
                if ((tempHome - tempLocPos).magnitude < 0.005f && ctrler.CursorController.stillTime > 0.3f)
                {
                    IncrementStep();
                    localizerLoc = Convert.ToSingle(ctrler.PseudoRandom("per_block_localizer_location"));
                    ctrler.TargetContainer.transform.rotation = Quaternion.Euler(0, localizerLoc, 0);
                    arcRotation = GameObject.Find("ArcRotation");
                    arcRotation.transform.position = targets[1].transform.position;
                    arcRotation.transform.rotation = Quaternion.Euler(Vector3.up * (90 - targetAngle));
                    arcSpan = Convert.ToInt32(ctrler.PseudoRandom("per_block_arc_span"));
                    targets[2].GetComponent<ArcScript>().arcSpan = arcSpan;
                    targets[2].GetComponent<ArcScript>().GenerateArc();

                }
                break;
            // When the user holds their hand and they are outside the home, begin the next phase of localization
            case 2:
                
                baseObject.transform.position = ctrler.CursorController.ConvertPosition(new Vector3 (temp.x, ctrler.TargetContainer.transform.position.y, temp.z));
                activeCursor.GetComponent<Renderer>().enabled = false;
                if(outEvent_3cm){
                    if((locPos - targets[1].transform.position).magnitude > 0.03){
                        if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                            pen_3cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, pen.transform.GetChild(0).transform.position), Time.time);
                            pos_3cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.transform.localPosition.normalized), Time.time);
                            cursor_3cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.Model.transform.position), Time.time);
                            outEvent_3cm = false;
                        }
                        else 
                        pos_3cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.transform.localPosition.normalized), Time.time);
                        cursor_3cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.Model.transform.position), Time.time); 
                        outEvent_3cm = false;
                    }
                }
                if(outEvent_2cm){
                    if((locPos - targets[1].transform.position).magnitude > 0.02){
                        if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                            pen_2cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, pen.transform.GetChild(0).transform.position), Time.time);
                            pos_2cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.transform.localPosition.normalized), Time.time);
                            cursor_2cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.Model.transform.position), Time.time);
                            outEvent_2cm = false;
                        }
                        else 
                        pos_2cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.transform.localPosition.normalized), Time.time);
                        cursor_2cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.Model.transform.position), Time.time); 
                        outEvent_2cm = false;
                    }
                }
                tempHandPos = ctrler.CursorController.transform.position;
                tempCursorPos = ctrler.CursorController.Model.transform.position;
                if(currentStep != 0 && currentStep != 1){
                    handPos.Add(new Vector4(tempHandPos.x, tempHandPos.y, tempHandPos.z, Time.time));
                    cursorPos.Add(new Vector4(tempCursorPos.x, tempCursorPos.y, tempCursorPos.z, Time.time));
                }

                if(ctrler.CursorController.stillTime > 0.5f &&
                        (targets[1].transform.position - locPos).magnitude > 0.1f && locPos.z > targets[1].transform.position.z
                        && (targets[1].transform.position - locPos).magnitude < 0.135f)
                        {
                            IncrementStep();
                            if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                                arcAquired = new Vector2(Vector3.Angle(targets[1].transform.right, pen.transform.GetChild(0).transform.position), Time.time);
                            }
                            else {
                                arcAquired = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.transform.localPosition.normalized), Time.time);
                            } 
                        }

                if((((targets[1].transform.position - locPos).magnitude > 0.005f && (targets[1].transform.position - locPos).magnitude < 0.13f) || ((targets[1].transform.position - locPos).magnitude > 0.135f)) 
                    && locPos.z > targets[1].transform.position.z){
                    
                    arcError.SetActive(true);
                    arcError.GetComponent<ArcScript>().TargetDistance = (targets[1].transform.position - locPos).magnitude * 100;
                    arcError.GetComponent<ArcScript>().GenerateArc();
                    
                    
                }
                else{
                    arcError.SetActive(false);
                }

                
                
                break;
            case 3:
                baseObject.transform.position = ctrler.CursorController.ConvertPosition(new Vector3 (temp.x, ctrler.TargetContainer.transform.position.y, temp.z));
                activeCursor.GetComponent<Renderer>().enabled = false;
                // VR: User uses their head to localize their hand
                // Non-VR: User uses horizontal axis to localize their mouse
                arcError.SetActive(false);
                targets[2].GetComponent<ArcScript>().arcSpan = 180;
                targets[2].GetComponent<ArcScript>().GenerateArc();
                arcRotation.transform.rotation = Quaternion.Euler(Vector3.up * (90 - 90));
                //logging params for cvs file
                tempHandPos = ctrler.CursorController.transform.position;
                tempCursorPos = ctrler.CursorController.Model.transform.position;
                handPos.Add(new Vector4(tempHandPos.x, tempHandPos.y, tempHandPos.z, Time.time));
                cursorPos.Add(new Vector4(tempCursorPos.x, tempCursorPos.y, tempCursorPos.z, Time.time));
                Vector3 tempIndicatorPos = locAim.transform.position;
                indicatorPos.Add(new Vector4(tempIndicatorPos.x, tempIndicatorPos.y, tempIndicatorPos.z, Time.time));

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
                        if (ctrler.CursorController.Get2DAxis().magnitude > 0.1f){
                            locX = ctrler.CursorController.Get2DAxis().x *100;
                            RotateLocalizer(locX);
                        }
                        else{
                            RotateLocalizer(0);
                        }   

                    }
                }
                else
                {
                    // A/D keys, left/right arrow keys, or gamepad joystick as input
                    locX = Input.GetAxisRaw("Horizontal") * 20;
                    RotateLocalizer(locX);
                    
                }

                // NON-VR** If the user presses the spacebar for 200ms and localizer is within arcspan, we end the trial
                Debug.Log(ctrler.TargetContainer.transform.rotation.y);
                if(Input.GetKey(KeyCode.S) && ((-0.7f < ctrler.TargetContainer.transform.rotation.y) && (ctrler.TargetContainer.transform.rotation.y < 0.7f))){
                    if(pressedTime > 0.2f){
                        IncrementStep();
                        localizingEvent = new Vector2(Vector3.Angle(targets[1].transform.right, (locAim.transform.position - targets[1].transform.position).normalized), Time.time);
                        baseObject.GetComponent<Renderer>().enabled = false;
                    }
                    
                }
                
                // VR** If the user presses the trigger associated with the hand for 200ms and localizer is within arcspan, we end the trial
                if (ctrler.CursorController.IsTriggerDown("l") && ((-0.7f < ctrler.TargetContainer.transform.rotation.y) && (ctrler.TargetContainer.transform.rotation.y < 0.7f))){
                    if(pressedTime > 0.2f){
                        IncrementStep();
                        localizingEvent = new Vector2(Vector3.Angle(targets[1].transform.right, (locAim.transform.position - targets[1].transform.position).normalized), Time.time);
                        baseObject.GetComponent<Renderer>().enabled = false;
                    }
                }
                    

                break;
        }

        if (Finished){
            finalReachAngle = Vector3.Angle(targets[1].transform.right, ctrler.CursorController.transform.localPosition.normalized);
            finalCursorAngle = Vector3.Angle(targets[1].transform.right, ctrler.CursorController.Model.transform.position);
            if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                finalPenAngle = Vector3.Angle(targets[1].transform.right, pen.transform.GetChild(0).transform.position);
            }
            ctrler.EndAndPrepare();
        }
            

        if (Input.GetKeyDown(KeyCode.C))
        {
            Centre();
        }
    }

    private void RotateLocalizer(float locX)
    {
        if (ctrler.TargetContainer.transform.rotation.eulerAngles.y < 100){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
        else if(ctrler.TargetContainer.transform.rotation.eulerAngles.y < 105 && locX < 0){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
        else if (ctrler.TargetContainer.transform.rotation.eulerAngles.y > 260){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
        else if (ctrler.TargetContainer.transform.rotation.eulerAngles.y > 255 && locX > 0){
            ctrler.TargetContainer.transform.Rotate(Vector3.up * locX * Time.deltaTime);
        }
    }

    public float getTime(){
        return Time.realtimeSinceStartup;
    }
    /// <summary>
    /// Centres the experiment a little in front of the hand position
    /// Distance forward is determined by the dock distance
    /// </summary>
    protected void Centre()
    {
        ctrler.CentreExperiment(targets[0].transform.position);
    }

    public override bool IncrementStep()
    {
        ExperimentController ctrler = ExperimentController.Instance();
        UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);

        switch (currentStep)
        {
            case 0: // Enter dock
                targets[0].SetActive(false);
                dummyDock.SetActive(false);
                Home.SetActive(true);
                VibrateController(0, 0.34f, 0.15f, devices);
                break;
            case 1: // Enter home
                Home.SetActive(false);
                VibrateController(0, 0.34f, 0.15f, devices);
                ctrler.StartTimer();

                Target.SetActive(true);

                

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
                baseObject.GetComponent<Renderer>().enabled = false;
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
        Session session = ctrler.Session;
        
        session.CurrentTrial.result["type"] = session.CurrentTrial.settings.GetString("per_block_type");
        session.CurrentTrial.result["pen_present"] = session.CurrentTrial.settings.GetBool("per_block_penPresent");
        session.CurrentTrial.result["hand"] = session.CurrentTrial.settings.GetString("per_block_hand");
        session.CurrentTrial.result["home_x"] = targets[1].transform.position.x;
        session.CurrentTrial.result["home_y"] = targets[1].transform.position.y;
        session.CurrentTrial.result["home_z"] = targets[1].transform.position.z;
        session.CurrentTrial.result["target_size_m"] = "";
        session.CurrentTrial.result["rotation_size"] = "";
        session.CurrentTrial.result["target_angle"] = targetAngle;
        session.CurrentTrial.result["cursor_size_m"] = ctrler.CursorController.Model.transform.localScale.x;
        session.CurrentTrial.result["arc_radius_or_target_distance_m"] = targets[2].GetComponent<ArcScript>().TargetDistance / 100;
        ctrler.LogVector4List("hand_pos", handPos);
        ctrler.LogVector4List("cursor_pos", cursorPos);
        ctrler.LogVector4List("pen_pos", penPos);
        session.CurrentTrial.result["hand_3cm_out_angle"] = pos_3cm_out.x;
        session.CurrentTrial.result["hand_3cm_out_time"] = pos_3cm_out.y;
        session.CurrentTrial.result["cursor_3cm_out_angle"] = cursor_3cm_out.x;
        session.CurrentTrial.result["cursor_3cm_out_time"] = cursor_3cm_out.y;
        session.CurrentTrial.result["pen_3cm_out_angle"] = pen_3cm_out.x;
        session.CurrentTrial.result["pen_3cm_out_time"] = pen_3cm_out.y;
        session.CurrentTrial.result["hand_2cm_out_angle"] = pos_2cm_out.x;
        session.CurrentTrial.result["hand_2cm_out_time"] = pos_2cm_out.y;
        session.CurrentTrial.result["cursor_2cm_out_angle"] = cursor_2cm_out.x;
        session.CurrentTrial.result["cursor_2cm_out_time"] = cursor_2cm_out.y;
        session.CurrentTrial.result["pen_2cm_out_angle"] = pen_2cm_out.x;
        session.CurrentTrial.result["pen_2cm_out_time"] = pen_2cm_out.y;
        session.CurrentTrial.result["hand_final_angle"] = finalReachAngle;
        session.CurrentTrial.result["cursor_final_angle"] = finalCursorAngle;
        session.CurrentTrial.result["pen_final_angle"] = finalPenAngle;
        session.CurrentTrial.result["arc_aquired_angle"] = arcAquired.x;
        session.CurrentTrial.result["arc_aquired_time"] = arcAquired.y;
        ctrler.LogVector4List("Indicator_position", indicatorPos);
        session.CurrentTrial.result["localizing_angle"] = localizingEvent.x;
        session.CurrentTrial.result["localizing_time"] = localizingEvent.y;
        
        

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

        base.OnDestroy();
    }
}
