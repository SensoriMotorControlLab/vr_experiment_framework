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
    protected List<GameObject> targets = new List<GameObject>();
    protected ExperimentController ctrler;
    protected Trial trial;

    protected GameObject reachPrefab;
    protected GameObject reachCam;
    protected GameObject reachSurface;
    protected GameObject waterBowl;
    protected GameObject water;
    protected TimerIndicator timerIndicator;
    protected Scoreboard scoreboard;
    protected GameObject tint;

    protected float speed = 1;
    protected int id;
    protected LTDescr d;
    protected float targetAngle;

    protected bool trackScore;
    protected bool hasTimer;

    public void Update()
    {
        if (!trackScore) scoreboard.ManualScoreText = "Practice Round";

        if (Input.GetKeyDown(KeyCode.N))
            IncrementStep();

        if (currentStep == 2 &&
            ctrler.CursorController.PauseTime > 0.5f &&
            ctrler.CursorController.DistanceFromHome > 0.05f &&
            trial.settings.GetString("per_block_type") == "nocursor")
            IncrementStep();

        if (Finished)
            ctrler.EndAndPrepare();

        // checks if there is a water animation in the scene
        if (d != null)
        {           
            // after animation has completed and the current step is the home step it sets the home ball to active
            if (!LeanTween.isTweening(id) && currentStep == 1)
            {
                targets[1].SetActive(true);
            }
        }
        else if(currentStep == 1){
            targets[1].SetActive(true);
        }
    }

    public override bool IncrementStep()
    {
        if(currentStep < 3){
            targets[currentStep].SetActive(false);
        }
        

        switch (currentStep)
        {
            // If the user enters the home, start tracking time
            case 1:
                ctrler.StartTimer();
                ctrler.CursorController.SetMovementType(reachType[2]);

                // Start green timer bar
                if(hasTimer){
                    timerIndicator.BeginTimer();
                }
                

                if (trial.settings.GetString("per_block_type") == "nocursor")
                    ctrler.CursorController.SetCursorVisibility(false);

                // Add trackers: current hand position, cursor position
                ctrler.AddTrackedPosition("hand_path",
                    ctrler.Session.CurrentTrial.settings.GetString("per_block_hand") == "l"
                        ? ctrler.CursorController.LeftHand
                        : ctrler.CursorController.RightHand);

                ctrler.AddTrackedPosition("cursor_path", ctrler.CursorController.gameObject);

                break;
            case 2:
                if (trackScore && ctrler.Session.settings.GetString("experiment_mode") != "targetTrack")
                {
                    ctrler.Score++;
                }
                break;
        }

        base.IncrementStep();

        if (!finished)
            // if current step is home step and there is a water animation in the scene, it sets the home ball to innactive
            if(currentStep == 1)
            {
                targets[1].SetActive(false);
            }
            else if(currentStep < 3)
            {
                targets[currentStep].SetActive(true);
            }
            

        return finished;
    }

    public void SetSetup(){
        targetAngle = Convert.ToSingle(ctrler.PseudoRandom("per_block_targetListToUse"));
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_tintPresent") == "tc1"){

             tint.SetActive(true);

            tint.transform.parent = Camera.main.transform;
            tint.transform.localPosition = new Vector3(0,0, 0.1f);
            tint.transform.localRotation = Quaternion.Euler(-90,0,0);

            float curTint = Convert.ToSingle(ctrler.PseudoRandom("per_block_tintPresent"));

            switch(curTint) 
            {
                case(0):
                tint.gameObject.GetComponent<Renderer>().material.color = (new Color(1,0,0,0.20f));
                break;

                case(1):
                tint.gameObject.GetComponent<Renderer>().material.color = (new Color(0,0,1,0.20f));
                break;

                case(3):
                tint.gameObject.GetComponent<Renderer>().material.color = (new Color(0,1,0,0.20f));
                break;

                case(4):
                tint.gameObject.GetComponent<Renderer>().material.color = (new Color(1,1,0,0.20f));
                break;

                case(5):
                tint.gameObject.GetComponent<Renderer>().material.color = (new Color(1,0,1,0.20f));
                break;

                case(6):
                tint.SetActive(false);
                break;
            }

         }
         else{
             tint.SetActive(false);
         }

         if(hasTimer){
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

        // Set up the dock position
        targets.Add(GameObject.Find("Dock"));
        targets[0].transform.localPosition = ctrler.TargetContainer.transform.localPosition;

        // Set up the home position
        targets.Add(GameObject.Find("Home"));
        targets[1].transform.localPosition = ctrler.TargetContainer.transform.localPosition + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        Home = targets[1];

        // Set up the target

        

        // Takes a target angle from the list and removes it
        
        
        targets.Add(GameObject.Find("Target"));
        targets[2].transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        targets[2].transform.localPosition = targets[1].transform.localPosition +
                                        targets[2].transform.forward.normalized *
                                        (trial.settings.GetFloat("per_block_distance") / 100f);

        

        // Disable collision detection for nocursor task
        if (trial.settings.GetString("per_block_type") == "nocursor")
            targets[2].GetComponent<BaseTarget>().enabled = false;

        targets[2].SetActive(false);
        Target = targets[2];

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
    public override void Setup()
    {
        maxSteps = 3;
        ctrler = ExperimentController.Instance();

        trial = ctrler.Session.CurrentTrial;

        Cursor.visible = false;

        reachPrefab = Instantiate(ctrler.GetPrefab("ReachPrefab"));
        reachPrefab.transform.SetParent(ctrler.transform);
        reachPrefab.transform.localPosition = new Vector3(0,-0.8f,0);

        reachCam = GameObject.Find("ReachCam");
        reachSurface = GameObject.Find("Surface");
        water = GameObject.Find("Water");
        timerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        tint = GameObject.Find("Tint");
      
        SetSetup();

        

        // sets up the water in the level

        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_waterPresent") == "wp1")
        {
            float waterLevel = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_waterPresent"));
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

        
    }

    public override void LogParameters()
    {
        Session session = ctrler.Session;

        session.CurrentTrial.result["home_x"] = Home.transform.localPosition.x;
        session.CurrentTrial.result["home_y"] = Home.transform.localPosition.y;
        session.CurrentTrial.result["home_z"] = Home.transform.localPosition.z;

        session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        session.CurrentTrial.result["target_y"] = Target.transform.localPosition.y;
        session.CurrentTrial.result["target_z"] = Target.transform.localPosition.z;
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
