using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ReachToTargetTask : BaseTask
{
    // The current steps are as follows:
    // 1. User goes to DOCK position (the starting position)
    // 2. User moves FORWARD to HOME position (aligned)
    // 3. User moves to TARGET with reachType[1]

    MovementType[] reachType;  // Reach type for current step
    protected List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

    // Dock distance from Home
    protected float dock_dist = 0.025f;
    protected List<GameObject> targets = new List<GameObject>();
    protected ExperimentController ctrler;
    protected Trial trial;
    float hold_still_time = 0.5f;

    protected GameObject reachPrefab;
    protected GameObject reachCam;
    protected GameObject reachSurface;
    protected GameObject waterBowl;
    protected GameObject water;
    protected TimerIndicator timerIndicator;
    protected Scoreboard scoreboard;
    protected GameObject tint;
    protected GameObject pen;
    protected GameObject activeCursor;
    protected float speed = 1;
    protected int id;
    protected LTDescr d;
    protected float targetAngle;

    protected bool trackScore;
    protected bool hasTimer;

    protected string tintColur;
    public float rotation = 0;
    List<Vector4> handPos = new List<Vector4>();
    List<Vector4> penPos = new List<Vector4>();
    List<Vector4> cursorPos = new List<Vector4>();
    Vector2 pos_3cm_out = new Vector2(0, 0);
    Vector2 pen_3cm_out = new Vector2(0, 0);
    Vector2 cursor_3cm_out = new Vector2(0, 0);
    Vector2 pos_2cm_out = new Vector2(0, 0);
    Vector2 pen_2cm_out = new Vector2(0, 0);
    Vector2 cursor_2cm_out = new Vector2(0, 0);
    bool outEvent_3cm = true;
    bool outEvent_2cm = true;
    GameObject baseObject;
    GameObject arc;
    float finalReachAngle;
    float finalCursorAngle;
    float finalPenAngle;
    GameObject office;
    GameObject lab;
    Vector3 rotatePoint;
    float penHeight;
    GameObject dummyDock;
    GameObject handImg;
    GameObject penIndicator;
    GameObject audioCue;

    public override void Setup()
    {
        maxSteps = 3;
        ctrler = ExperimentController.Instance();

        trial = ctrler.Session.CurrentTrial;

        Cursor.visible = false;
        Debug.Log("ReachToTargetTask Setup");
        reachPrefab = Instantiate(ctrler.GetPrefab("ReachPrefab"));
        reachPrefab.transform.SetParent(ctrler.transform);
        reachPrefab.transform.position = new Vector3(0, 0, 0);
        ctrler.TargetContainer.transform.position = new Vector3(0, 0.05f, 0);


        reachCam = GameObject.Find("ReachCam");
        reachSurface = GameObject.Find("Surface");
        water = GameObject.Find("Water");
        // timerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        tint = GameObject.Find("Tint");
        waterBowl = GameObject.Find("waterBasin");
        waterBowl.SetActive(false);
        baseObject = GameObject.Find("BaseObject");
        arc = GameObject.Find("ArcError");
        pen = GameObject.Find("Pen");
        dummyDock = GameObject.Find("DummyDock");
        handImg = GameObject.Find("hand");
        penIndicator = GameObject.Find("PenIndicator");
        SetSetup();
        arc.SetActive(false);

        if (ctrler.Session.settings.GetObjectList("optional_params").Contains("cursor") && ctrler.Session.CurrentTrial.settings.GetBool("per_block_cursor_visible")){
            ctrler.CursorController.SetCursorVisibility(true);
        }
        else{
            ctrler.CursorController.SetCursorVisibility(false);
        }

        baseObject.GetComponent<Renderer>().enabled = false;

        if(trial.settings.GetBool("per_block_penPresent")){
            pen.SetActive(true);
            baseObject.GetComponent<Renderer>().enabled = false;
            office = Instantiate(ctrler.GetPrefab("office"), new Vector3(-3.4f, -0.71f, 8.1f), Quaternion.Euler(0, 180, 0));
            office.SetActive(true);
            office.transform.parent = reachPrefab.transform;
            pen.GetComponent<Renderer>().enabled = false;
            pen.transform.localEulerAngles = new Vector3(0, -165, -15);
            penHeight = Mathf.Abs(pen.transform.position.y - pen.transform.GetChild(0).transform.position.y);
            reachSurface.GetComponent<Renderer>().material = ctrler.Materials["wood"];
            reachSurface.transform.localScale = new Vector3(1.22f, 0.1f, 1.9f);
            handImg.GetComponent<SpriteRenderer>().enabled = false;
            if(trial.settings.GetString("per_block_type") == "nocursor"){
                penIndicator.GetComponent<Renderer>().enabled = true;
                penIndicator.transform.position = targets[0].transform.position;
            }
            else{
                penIndicator.GetComponent<Renderer>().enabled = false;
            }
        }
        else{
            pen.SetActive(false);
            lab = Instantiate(ctrler.GetPrefab("room"), new Vector3(-0.13f, 0.16f, 0.218f), Quaternion.identity);
            lab.SetActive(true);
            lab.transform.parent = reachPrefab.transform;
            reachSurface.transform.localScale = new Vector3(4f, 0.1f, 4f);
            handImg.GetComponent<SpriteRenderer>().enabled = true;
            handImg.transform.position = targets[0].transform.position;
            penIndicator.GetComponent<Renderer>().enabled = false;
        }

        if(trial.settings.GetString("per_block_type") == "nocursor"){
            isNoCursor = true;
            reachSurface.GetComponent<Renderer>().material.color = new Color(0.1f, 0f, 0f, 1f);
            if(trial.settings.GetBool("per_block_penPresent")){
                audioCue = GameObject.Find("pen_to_target");
                audioCue.GetComponent<AudioSource>().Play();
            }
            else{
                audioCue = GameObject.Find("thumb_to_target");
                audioCue.GetComponent<AudioSource>().Play();
            }
        }
        else if (trial.settings.GetString("per_block_type") == "rotated"){
            rotation = Convert.ToSingle(ctrler.PseudoRandom("per_block_rotation"));
            reachSurface.GetComponent<Renderer>().material.color = new Color(0f, 0f, 0.1f, 1f);
        }

        else if (trial.settings.GetString("per_block_type") == "aligned"){
            reachSurface.GetComponent<Renderer>().material.color = new Color(0f, 0.1f, 0f, 1f);
        }
        // sets up the water in the level

        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_waterPresent") == "wp1")
        {
            float waterLevel = Convert.ToSingle(ctrler.PseudoRandom("per_block_waterPresent"));
            //waterBowl.SetActive(true);
            water.SetActive(true);

            // If previous trial had a water level, animate water level rising/falling from that level
            try
            {
                if (ctrler.Session.PrevTrial.result.ContainsKey("per_block_waterPresent"))
                {
                    water.transform.localPosition =
                        new Vector3(0,
                        Convert.ToSingle(ctrler.Session.PrevTrial.result["per_block_waterPresent"]) / 10,
                        0);

                    id = LeanTween.moveLocalY(water, waterLevel / 10, speed).id;
                    d = LeanTween.descr(id);
                }
                else
                {
                    water.transform.localPosition = new Vector3(0, -0.03f, 0);
                    id = LeanTween.moveLocalY(water, waterLevel / 10, speed).id;
                    d = LeanTween.descr(id);
                }
            }
            catch (NoSuchTrialException e)
            {
                water.transform.localPosition = new Vector3(0, -0.03f, 0);
                id = LeanTween.moveLocalY(water, waterLevel / 10, speed).id;
                d = LeanTween.descr(id);
            }
        }
        else
        {
            //waterBowl.SetActive(false);
            water.SetActive(false);
        }

        ctrler.CursorController.useHand = true;
    }

        protected virtual Vector3 GetMousePoint(Transform ball)
    {
        //ToFix: can the below two be one function called point to planepoint?
        Vector3 ctrl = new Vector3(ctrler.CursorController.GetHandPosition().x, 0, ctrler.CursorController.GetHandPosition().z);
        reachSurface = GameObject.Find("Surface");

            return ctrler.CursorController.ControllerToPlanePoint(
                        reachSurface.transform.up * ball.position.y,
                        ball.position,
                        ctrl);
    }

    void PenFollowMouse()
    {
        pen.transform.localEulerAngles = new Vector3(0, -165, -15);
        penHeight = Mathf.Abs(pen.transform.position.y - pen.transform.GetChild(0).transform.position.y);
        pen.transform.position = new Vector3(baseObject.transform.position.x, ctrler.TargetContainer.transform.position.y + penHeight, baseObject.transform.position.z);
        Vector3 temp = ctrler.CursorController.GetHandPosition();
        switch(currentStep){
            case 0: 
                if(trial.settings.GetString("per_block_type") == "nocursor"){
                    penIndicator.GetComponent<Renderer>().enabled = true;
                    penIndicator.transform.position = targets[0].transform.position;
                }
                handImg.GetComponent<SpriteRenderer>().enabled = false;
                baseObject.GetComponent<BaseTarget>().enabled = true;
                Vector3 mousePoint = GetMousePoint(baseObject.transform);
                Vector3 mousePlane = new Vector3(ctrler.CursorController.Model.transform.position.x, 0, ctrler.CursorController.Model.transform.position.z);
                baseObject.transform.position = ctrler.CursorController.ConvertPosition(mousePlane);
                break;
            case 1:
            if (isNoCursor){
                penIndicator.GetComponent<Renderer>().enabled = false;
                if((pen.transform.GetChild(0).transform.position - targets[1].transform.position).magnitude < 0.06f
                && (pen.transform.GetChild(0).transform.position - targets[1].transform.position).magnitude > 0.005f){
                    VibrateController(0, (0.6f - ((pen.transform.GetChild(0).transform.position - targets[1].transform.position).magnitude * 10))/2, Time.deltaTime, devices);
                    float color = 1 - ((pen.transform.GetChild(0).transform.position - targets[1].transform.position).magnitude)/0.06f;
                    targets[1].gameObject.GetComponent<Renderer>().material.color = (new Color(1, color, color, 1f));
                }
                else if((pen.transform.GetChild(0).transform.position - targets[1].transform.position).magnitude < 0.005f){
                    targets[1].gameObject.GetComponent<Renderer>().material.color = (new Color(0, 1, 0, 1f));
                }
            }
                
                baseObject.GetComponent<BaseTarget>().enabled = false;
                baseObject.transform.position = ctrler.CursorController.ConvertPosition(new Vector3 (temp.x, ctrler.TargetContainer.transform.position.y, temp.z), rotatePoint);
                break;
            default:
                baseObject.GetComponent<BaseTarget>().enabled = false;
                baseObject.transform.position = ctrler.CursorController.ConvertPosition(new Vector3 (temp.x, ctrler.TargetContainer.transform.position.y, temp.z), rotatePoint);
                break;
        }
        Vector3 tempPenPos = pen.transform.GetChild(0).transform.position;
        if(currentStep !=0 && currentStep != 1){
            penPos.Add(new Vector4(tempPenPos.x, tempPenPos.y, tempPenPos.z, Time.time));
        }
        
    }

    public override void Update()
    {
        base.Update();
        baseObject = GameObject.Find("BaseObject");
        pen = GameObject.Find("Pen");
        reachSurface = GameObject.Find("Surface");
        handImg = GameObject.Find("hand");

        if(trial.settings.GetString("per_block_type") == "nocursor"){
            isNoCursor = true;
            reachSurface.GetComponent<Renderer>().material.color = new Color(0.1f, 0f, 0f, 1f);
        }
        else{
            isNoCursor = false;
        }

        if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
            pen.GetComponent<Renderer>().enabled = true;
            baseObject.GetComponent<Renderer>().enabled = false;
            activeCursor = pen;
            PenFollowMouse();
            reachSurface.GetComponent<Renderer>().material = ctrler.Materials["wood"];
            reachSurface.transform.localScale = new Vector3(1.22f, 0.1f, 1.9f);
        }
        else{
            
            activeCursor = baseObject;
            reachSurface.transform.localScale = new Vector3(4f, 0.1f, 4f);
            Vector3 temp = ctrler.CursorController.GetHandPosition();
            switch(currentStep){
                case 0: 
                    handImg.GetComponent<SpriteRenderer>().enabled = true;
                    handImg.transform.position = targets[0].transform.position;
                    Vector3 mousePoint = GetMousePoint(baseObject.transform);
                    Vector3 mousePlane = new Vector3(ctrler.CursorController.Model.transform.position.x, 0, ctrler.CursorController.Model.transform.position.z);
                    baseObject.transform.position = ctrler.CursorController.ConvertPosition(mousePlane);
                    break;
                case 1:
                    handImg.GetComponent<SpriteRenderer>().enabled = false;
                    if(isNoCursor && (baseObject.transform.position - targets[1].transform.position).magnitude < 0.06f
                    && (baseObject.transform.position - targets[1].transform.position).magnitude > 0.005f){
                        VibrateController(0, (0.6f - ((baseObject.transform.position - targets[1].transform.position).magnitude * 10))/2, Time.deltaTime, devices);
                        float color = 1 - ((baseObject.transform.position - targets[1].transform.position).magnitude)/0.06f;
                        targets[1].gameObject.GetComponent<Renderer>().material.color = (new Color(1, color, color, 1f));
                    }
                    else if(isNoCursor && ((baseObject.transform.position - targets[1].transform.position).magnitude < 0.005f)){
                        targets[1].gameObject.GetComponent<Renderer>().material.color = (new Color(0, 1, 0, 1f));
                    }
                    baseObject.transform.position = ctrler.CursorController.ConvertPosition(new Vector3 (temp.x, ctrler.TargetContainer.transform.position.y, temp.z));
                    break;
                default:
                    
                    baseObject.transform.position = ctrler.CursorController.ConvertPosition(new Vector3 (temp.x, ctrler.TargetContainer.transform.position.y, temp.z));
                    break;
            }
        }
        
        switch (currentStep)
        {
            case 0:
                targets[2].SetActive(false);
                if (!ctrler.Session.settings.GetObjectList("optional_params").Contains("return_visible") || isNoCursor)
                {
                    // make the ball invisible
                    activeCursor.GetComponent<Renderer>().enabled = false;
                }
                else
                {
                    activeCursor.GetComponent<Renderer>().enabled = true;
                }
                break;
            case 1:
                if(isNoCursor){
                    activeCursor.GetComponent<Renderer>().enabled = false;
                }
                else{
                    activeCursor.GetComponent<Renderer>().enabled = true;
                }
                break;
            case 2:
                if(isNoCursor){
                    activeCursor.GetComponent<Renderer>().enabled = false;
                }
                if(isNoCursor && activeCursor.transform.position.z > targets[1].transform.position.z){
                    arc.SetActive(true);
                    if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                        arc.GetComponent<ArcScript>().TargetDistance = (activeCursor.transform.GetChild(0).transform.position - arc.transform.position).magnitude * 100;
                        arc.GetComponent<ArcScript>().GenerateArc();
                    }
                    else{
                        arc.GetComponent<ArcScript>().TargetDistance = (activeCursor.transform.position - arc.transform.position).magnitude * 100;
                        arc.GetComponent<ArcScript>().GenerateArc();
                    }
                    
                }
                else {
                    arc.GetComponent<ArcScript>().TargetDistance = 0;
                    arc.GetComponent<ArcScript>().GenerateArc();
                    arc.SetActive(false);
                }
                break;
        }

        // switch(trial.settings.GetString("per_block_type")){
        //     case "rotated":
        //         reachSurface.GetComponent<Renderer>().material.color = new Color(0f, 0f, 0.1f, 1f);
        //         break;
        //     case "aligned":
        //         reachSurface.GetComponent<Renderer>().material.color = new Color(0f, 0.1f, 0f, 1f);
        //         break;
        //     case "nocursor":
        //         reachSurface.GetComponent<Renderer>().material.color = new Color(0.1f, 0f, 0f, 1f);
        //         break;
        // }

        if (!trackScore) scoreboard.ManualScoreText = "Practice Round";

        if (Input.GetKeyDown(KeyCode.N))
            IncrementStep();


        if (currentStep == 2 &&
            ctrler.CursorController.stillTime > 0.5f && (targets[1].transform.position - activeCursor.transform.GetChild(0).transform.position).magnitude > 0.03f &&
            trial.settings.GetString("per_block_type") == "nocursor" && (targets[0].transform.position - activeCursor.transform.GetChild(0).transform.position).magnitude > 0.03f
            && ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                IncrementStep();
            }
        else if(currentStep == 2 &&
            ctrler.CursorController.stillTime > 0.5f && (targets[1].transform.position - activeCursor.transform.position).magnitude > 0.03f &&
            trial.settings.GetString("per_block_type") == "nocursor" && (targets[0].transform.position - activeCursor.transform.position).magnitude > 0.03f 
            && !ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                IncrementStep();
            }
            

        if (Finished){
            if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                Vector3 penDiff = targets[1].transform.position - activeCursor.transform.GetChild(0).transform.position;
                float penRad = MathF.Atan2(penDiff.z, penDiff.x);
                finalPenAngle = penRad * Mathf.Rad2Deg + 180;
            }
            Vector3 diff = targets[1].transform.position - ctrler.CursorController.transform.localPosition.normalized;
            float rad = MathF.Atan2(diff.z, diff.x);
            finalReachAngle = rad * Mathf.Rad2Deg + 180;
            diff = targets[1].transform.position - baseObject.transform.position;
            rad = MathF.Atan2(diff.z, diff.x);
            finalCursorAngle = rad * Mathf.Rad2Deg + 180;
            ctrler.EndAndPrepare();
        }
            

        // checks if there is a water animation in the scene
        if (d != null)
        {
            // after animation has completed and the current step is the home step it sets the home ball to active
            if (!LeanTween.isTweening(id) && currentStep == 1)
            {
                targets[1].SetActive(true);
            }
        }
        else if (currentStep == 1)
        {
            targets[1].SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Centre();
            
        }
        switch(currentStep)
        {
            case 2:
                Vector3 diff;
                float rad;
                if(outEvent_3cm){
                    if((targets[1].transform.position - activeCursor.transform.position).magnitude > 0.03f && !ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                        diff = targets[1].transform.position - ctrler.CursorController.transform.localPosition.normalized;
                        rad = MathF.Atan2(diff.z, diff.x);
                        pos_3cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        diff = targets[1].transform.position - baseObject.transform.position;
                        rad = MathF.Atan2(diff.z, diff.x);
                        cursor_3cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        outEvent_3cm = false;
                    }
                    else if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent") && (targets[1].transform.position - activeCursor.transform.GetChild(0).transform.position).magnitude > 0.03f){
                        diff = targets[1].transform.position - activeCursor.transform.GetChild(0).transform.position;
                        rad = MathF.Atan2(diff.z, diff.x);
                        pen_3cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        diff = targets[1].transform.position - ctrler.CursorController.transform.localPosition.normalized;
                        rad = MathF.Atan2(diff.z, diff.x);
                        pos_3cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        diff = targets[1].transform.position - baseObject.transform.position;
                        rad = MathF.Atan2(diff.z, diff.x);
                        cursor_3cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        outEvent_3cm = false;
                    }
                    
                }
                if(outEvent_2cm){
                    if((targets[1].transform.position - activeCursor.transform.position).magnitude > 0.02f && !ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                        diff = targets[1].transform.position - ctrler.CursorController.transform.localPosition.normalized;
                        rad = MathF.Atan2(diff.z, diff.x);
                        pos_2cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        diff = targets[1].transform.position - baseObject.transform.position;
                        rad = MathF.Atan2(diff.z, diff.x);
                        cursor_2cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        outEvent_2cm = false;
                    }
                    else if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent") && (targets[1].transform.position - activeCursor.transform.GetChild(0).transform.position).magnitude > 0.02f){
                        diff = targets[1].transform.position - activeCursor.transform.GetChild(0).transform.position;
                        rad = MathF.Atan2(diff.z, diff.x);
                        pen_2cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        diff = targets[1].transform.position - ctrler.CursorController.transform.localPosition.normalized;
                        rad = MathF.Atan2(diff.z, diff.x);
                        pos_2cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        diff = targets[1].transform.position - baseObject.transform.position;
                        rad = MathF.Atan2(diff.z, diff.x);
                        cursor_2cm_out = new Vector2((rad * Mathf.Rad2Deg + 180), Time.time);
                        outEvent_2cm = false;
                    }
                    
                }
                Vector3 tempHandPos = ctrler.CursorController.transform.position;
                Vector3 tempCursorPos = baseObject.transform.position;
                if(currentStep != 0 && currentStep != 1){
                    handPos.Add(new Vector4(tempHandPos.x, tempHandPos.y, tempHandPos.z, Time.time));
                    cursorPos.Add(new Vector4(tempCursorPos.x, tempCursorPos.y, tempCursorPos.z, Time.time));
                }
                break;
        }
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
        if (currentStep < 3)
        {
            targets[currentStep].SetActive(false);
        }

        switch (currentStep)
        {
            // If the user enters the home, start tracking time
            case 0:
                VibrateController(0, 0.34f, 0.15f, devices);
                dummyDock.SetActive(false);
                targets[2].SetActive(false);
                targets[1].SetActive(true);
                break;
            case 1:
                rotatePoint = baseObject.transform.position;
                ctrler.StartTimer();
                ctrler.CursorController.SetMovementType(reachType[2]);
                VibrateController(0, 0.34f, 0.15f, devices);
                // Start green timer bar
                if (hasTimer)
                {
                    timerIndicator.BeginTimer();
                }

                if (trial.settings.GetString("per_block_type") == "nocursor" ){
                    activeCursor.GetComponent<Renderer>().enabled = false;
                    arc.SetActive(true);
                    if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent")){
                        activeCursor.transform.GetChild(0).GetComponent<BaseTarget>().enabled = false;
                    }
                    else{
                        activeCursor.GetComponent<BaseTarget>().enabled = false;
                    }
                    
                } 
                else if(trial.settings.GetBool("per_block_penPresent")){
                    baseObject.GetComponent<Renderer>().enabled = false;
                    baseObject.GetComponent<BaseTarget>().enabled = false;
                }                
                else{
                    baseObject.GetComponent<Renderer>().enabled = true;
                }
                targets[2].SetActive(true);
                break;
            case 2:
                arc.SetActive(false);
                targets[2].SetActive(false);
                VibrateController(0, 0.34f, 0.15f, devices);
                if (trackScore && ctrler.Session.settings.GetString("experiment_mode") != "targetTrack")
                {
                    ctrler.Score++;
                }
                break;
        }

        base.IncrementStep();

        if (!finished)
            // if current step is home step and there is a water animation in the scene, it sets the home ball to innactive
            if (currentStep == 1)
            {
                targets[1].SetActive(false);
            }


        return finished;
    }

    public override float GetRotation()
    {
        return rotation;
    }

    public void SetSetup()
    {
        targetAngle = Convert.ToSingle(ctrler.PseudoRandom("per_block_targetListToUse"));
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_tintPresent") == "tc1")
        {
            object obj = ctrler.PseudoRandom("per_block_tintPresent");
            float curTint = Convert.ToSingle(obj);
            rotation = Convert.ToSingle(ctrler.PseudoRandom("per_block_rotation"));
            
            tint.SetActive(true);

            tint.transform.parent = Camera.main.transform;
            tint.transform.localPosition = new Vector3(0, 0, 0.1f);
            tint.transform.localRotation = Quaternion.Euler(-90, 0, 0);



            switch (curTint)
            {
                case (0):
                    tint.gameObject.GetComponent<Renderer>().material.color = (new Color(1, 0, 0, 0.20f));
                    tintColur = "red";
                    break;

                case (1):
                    tint.gameObject.GetComponent<Renderer>().material.color = (new Color(0, 0, 1, 0.20f));
                    tintColur = "blue";
                    break;

                case (2):
                    tint.gameObject.GetComponent<Renderer>().material.color = (new Color(0, 1, 0, 0.20f));
                    tintColur = "green";
                    break;

                case (3):
                    tint.gameObject.GetComponent<Renderer>().material.color = (new Color(1, 1, 0, 0.20f));
                    tintColur = "yellow";
                    break;

                case (4):
                    tint.gameObject.GetComponent<Renderer>().material.color = (new Color(1, 0, 1, 0.20f));
                    tintColur = "purple";
                    break;

                case (5):
                    tint.SetActive(false);
                    break;
            }

        }
        else
        {
            tint.SetActive(false);
        }

        if (hasTimer)
        {
            timerIndicator.Timer = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_timerTime");
        }



        // Whether or not this is a practice trial 
        // replaces scoreboard with 'Practice Round', doesn't record score
        trackScore = (ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_score"));

        if (!trackScore)
        {
            // Scoreboard is now updated by the reach class
            scoreboard.AllowManualSet = true;
            scoreboard.ScorePrefix = false;
            scoreboard.ManualScoreText = "Practice Round";
        }

        Enum.TryParse(ctrler.Session.CurrentTrial.settings.GetString("per_block_type"),
            out MovementType rType);

        reachType = new MovementType[3];
        reachType[2] = rType;


        // Set up hand and cursor
        ctrler.CursorController.SetHandVisibility(false);
        ctrler.CursorController.SetCursorVisibility(true);

        Vector3 tempPos;
        tempPos =new Vector3 (ctrler.TargetContainer.transform.position.x, ctrler.TargetContainer.transform.position.y, ctrler.TargetContainer.transform.position.z); 

        // Set up the dock position
        targets.Add(GameObject.Find("Dock"));
        targets[0].transform.position = reachPrefab.transform.position - ctrler.transform.forward * dock_dist;
        dummyDock.transform.position = new Vector3(targets[0].transform.position.x, ctrler.TargetContainer.transform.position.y, targets[0].transform.position.z);
        targets[0].GetComponent<MeshRenderer>().enabled = false;
        // Set up the home position
        targets.Add(GameObject.Find("Home"));
        targets[1].transform.localPosition = tempPos + targets[1].transform.forward * 7f/100f;
        targets[1].SetActive(false);
        Home = targets[1];
        arc.transform.position = targets[1].transform.position;

        // Set up the target
        // Takes a target angle from the list and removes it
        targets.Add(GameObject.Find("Target"));
        targets[2].transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        targets[2].transform.localPosition = targets[1].transform.localPosition +
                                        targets[2].transform.forward.normalized *
                                        (trial.settings.GetFloat("per_block_distance") / 100f);

        targets[2].SetActive(false);
        Target = targets[2];

        // Disable collision detection for nocursor task
        // if (trial.settings.GetString("per_block_type") == "nocursor")
            // targets[2].GetComponent<BaseTarget>().enabled = false;

        

        // Use static camera for non-vr version
        if (ctrler.Session.settings.GetObjectList("optional_params").Contains("vr"))
        {
            reachSurface.SetActive(true);
            reachCam.SetActive(false);
            ctrler.CursorController.UseVR = true;
        }
        else
        {
            ctrler.CursorController.SetVRCamera(false);
        }

    }

    public override void LogParameters()
    {
        Session session = ctrler.Session;

        session.CurrentTrial.result["type"] = session.CurrentTrial.settings.GetString("per_block_type");
        session.CurrentTrial.result["pen_present"] = ctrler.Session.CurrentTrial.settings.GetBool("per_block_penPresent");
        session.CurrentTrial.result["hand"] = session.CurrentTrial.settings.GetString("per_block_hand");
        session.CurrentTrial.result["home_x"] = targets[1].transform.position.x;
        session.CurrentTrial.result["home_y"] = targets[1].transform.position.y;
        session.CurrentTrial.result["home_z"] = targets[1].transform.position.z;
        session.CurrentTrial.result["target_angle"] = targetAngle;
        session.CurrentTrial.result["target_size_m"] = targets[2].transform.localScale.x;
        session.CurrentTrial.result["rotation_size"] = rotation;
        session.CurrentTrial.result["cursor_size_m"] = ctrler.CursorController.Model.transform.localScale.x;
        session.CurrentTrial.result["arc_radius_or_target_distance_m"] = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_distance") / 100;
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
        session.CurrentTrial.result["arc_aquired_angle"] = "";
        session.CurrentTrial.result["arc_aquired_time"] = "";
        session.CurrentTrial.result["localizing_angle"] = "";
        session.CurrentTrial.result["localizing_time"] = "";
        
    }

    public override void Disable()
    {
        reachPrefab.SetActive(false);

        //ctrler.CursorController.SetVRCamera(true);
    }

    protected override void OnDestroy()
    {
        // When the trial ends, we need to delete all the objects this task spawned
        Destroy(reachPrefab);

        base.OnDestroy();
    }
}
