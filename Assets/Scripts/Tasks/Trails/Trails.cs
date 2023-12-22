using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UXF;
using MovementType = CursorController.MovementType;

public class Trails : BaseTask
{
    private ExperimentController ctrler;
    private GameObject trailSpace;
    protected Trial trial;

    private GameObject trailGate1, trailGate2;

    private BoxCollider startCollider; // Trigger attached to trailgate1 (start gate)
    private BoxCollider midwayCollider; // Standalone trigger used to determine if user is going correct direction
    [SerializeField]

    private GameObject railing1, railing2; 

    private GatePlacement gatePlacement; 

    private GameObject roadSegments;
    private GameObject track; 
    private GameObject obst;

    private float startPoint, endPoint, midPoint; // Percentages between 0-1 where the start, mid, and end gates will be placed along the track (clockwise)

    private GameObject car;

    [SerializeField]
    private bool carPastMidpoint = false;

    [SerializeField]
    private bool isOnTrack = true;

    // num times car went off track
    [SerializeField]
    private int numImpacts = 0;

    private List<Transform> raycastOrigins = new List<Transform>();

    // Number of triggers spread evenly between start & end point.
    // The user has to contact at least one of these for a lap to count.
    private int midTriggersCounter = 2;

    private List<BaseTarget> midwayTriggers = new List<BaseTarget>();

    // Whether to use raycasts or use the inner track to dermine whether offtrack
    private bool useRayCasts = true;

    private float inTrackTime, outTrackTime, pitStopTime;
    private float startOcclusionTime, endOcclusionTime, occludedTime;
    private List<bool> onTrackFrameStatus = new List<bool>();
    private List<Vector2> carPath = new List<Vector2>();
    private List<Vector2> cursorPath = new List<Vector2>();
    private List<Vector3> outTrackPath = new List<Vector3>();
    private List<Vector3> inTrackPath = new List<Vector3>();
    private List<Vector3> RF_InPath = new List<Vector3>();
    private List<Vector3> RF_OutPath = new List<Vector3>();
    private List<Vector3> LF_InPath = new List<Vector3>();
    private List<Vector3> LF_OutPath = new List<Vector3>();
    private List<Vector3> RR_InPath = new List<Vector3>();
    private List<Vector3> RR_OutPath = new List<Vector3>();
    private List<Vector3> LR_InPath = new List<Vector3>();
    private List<Vector3> LR_OutPath = new List<Vector3>();

    [SerializeField]
    private int score;
    private Scoreboard scoreboard;

    private string lapDiff = "-.--";
    private string bestLap = "-.--";
    private string lastLap = "-.--";
    private Dictionary<string, string> scoreboardInfo = new Dictionary<string, string>();
    private bool hasSoundPlayed = false;
    private bool isRunValid = true;
    private Vector3 carVelocity;
    private Vector3 carPrevPos;
    Quaternion targetRotation;

    /*
     * Step 0: 
     * 
     * spawn at startpoint gate
     * mouse move to car point
     * 
     * Step 1:
     * let car move
     * 
     * Step 2:
     * hit wall or hit finish line gate
     * log parameters
     * end trial
     * 
     * Step 3:
     * finished
     * 
     */

    public override void Setup()
    {
        maxSteps = 3;
        ctrler = ExperimentController.Instance();

        trailSpace = Instantiate(ctrler.GetPrefab("TrailPrefab"));

        trailGate1 = GameObject.Find("TrailGate1");
        trailGate2 = GameObject.Find("TrailGate2");

        startCollider = trailGate1.transform.GetChild(3).GetComponent<BoxCollider>();
        midwayCollider = GameObject.Find("MidwayCollider").GetComponent<BoxCollider>();

        gatePlacement = GameObject.Find("gatePlacement").GetComponent<GatePlacement>();

        roadSegments = GameObject.Find("generated_by_SplineExtrusion");

        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        track = GameObject.Find("Track");
        obst = GameObject.Find("Occlusion");
        car = GameObject.Find("Car");

        Home = trailGate1;

        switch(ctrler.Session.CurrentBlock.settings.GetString("per_block_type")){
            case "aligned":
                ctrler.CursorController.SetMovementType(MovementType.aligned);
                break;

            case "rotated":
                ctrler.CursorController.SetMovementType(MovementType.rotated);
                break;


        }
        for (int i = 0; i < roadSegments.transform.childCount; i++)
        { // add road segments to gatePlacement list of meshes
            gatePlacement.mesh.Add(roadSegments.transform.GetChild(i).GetComponent<MeshFilter>().mesh);
        }
        gatePlacement.Setup();

        /* TrailGate children:
        * 0: pole1
        * 1: pole2 
        * 2: checkered line (line renderer component)
        * 3: trigger (collider component)
        */

        startPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_startPoint");
        gatePlacement.SetGatePosition(trailGate1, trailGate1.transform.GetChild(0).gameObject, trailGate1.transform.GetChild(1).gameObject,
            trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(3).GetComponent<BoxCollider>(), startPoint, roadSegments);

        endPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_endPoint");
        gatePlacement.SetGatePosition(trailGate2, trailGate2.transform.GetChild(0).gameObject, trailGate2.transform.GetChild(1).gameObject,
            trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(3).GetComponent<BoxCollider>(), endPoint, roadSegments);

        if(ctrler.GetBestLapTime() != 0){
            lapDiff = ctrler.GetLapDiff();
            bestLap = ctrler.GetBestLapTime().ToString("0.000");
            lastLap = ctrler.GetLastLapTime().ToString("0.000");
        }
        scoreboardInfo.Add("Lap: ", ctrler.Session.currentTrialNum.ToString() + "/" + ExperimentController.Instance().Session.Trials.Count().ToString());
        scoreboardInfo.Add("Demerit Points", score.ToString());
        // scoreboardInfo.Add("Lap Time", lastLap);
        // scoreboardInfo.Add("Best Lap", bestLap);
        // scoreboardInfo.Add("Lap Diff", lapDiff);
        // scoreboardInfo.Add("% on track", score.ToString());

        scoreboard.SetElements(scoreboardInfo);

        // set up how many midway triggers to place according to the distance between start and end point
        if (endPoint < startPoint)
        midTriggersCounter = Mathf.RoundToInt((endPoint - startPoint + 1) * 10);
        else
        midTriggersCounter = Mathf.RoundToInt((endPoint - startPoint) *10 );

        // Place midway triggers throughout the track
        for (int i = 0; i < midTriggersCounter; i++)
        {
            midwayTriggers.Add(Instantiate(midwayCollider.gameObject).GetComponent<BaseTarget>());

            // Start    Mid1     Mid2     End
            // |--------|--------|--------|
            if (endPoint < startPoint)
            {
                // If the end point comes before the start point

                float distance = endPoint - startPoint + 1;

                midPoint = ((distance) / (midTriggersCounter + 1)) * (i + 1) + startPoint;

                if (midPoint > 1)
                    midPoint -= 1;
            }
            else
            {
                float distance = endPoint - startPoint;

                midPoint = ((distance) / (midTriggersCounter + 1)) * (i + 1) + startPoint;
            }

            gatePlacement.SetColliderPosition(midwayTriggers[i].GetComponent<BoxCollider>(), midPoint, roadSegments);
            midwayTriggers[i].transform.parent = track.transform;
        }
        midwayCollider.gameObject.SetActive(false);

        railing1 = GameObject.Find("generated_by_SplineMeshTiling");
        foreach (MeshCollider railing in railing1.transform.GetComponentsInChildren<MeshCollider>())
        {
            railing.tag = "TrailRailing";
            railing.convex = true;
            railing.isTrigger = true;
            railing.gameObject.SetActive(false);
        }

        railing2 = GameObject.Find("generated_by_SplineMeshTiling_1");
        foreach (MeshCollider railing in railing1.transform.GetComponentsInChildren<MeshCollider>())
        {
            railing.tag = "TrailRailing";
            railing.convex = true;
            railing.isTrigger = true;
            railing.gameObject.SetActive(false);
        }

        for(int i = 0; i < railing1.transform.GetChild(0).transform.childCount; i++)
        {
            railing1.transform.GetChild(i).gameObject.AddComponent<BaseTarget>();
        }

        for (int i = 0; i < railing2.transform.childCount; i++)
        {
            railing2.transform.GetChild(i).gameObject.AddComponent<BaseTarget>();
        }

        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetString("experiment_mode") == "trail")
        {
            ctrler.CursorController.SetVRCamera(false);
        }
        
        //check is obstruction is TRUE on the JSON and places it on the track
        if(ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_occlusion")){
            obst.SetActive(true);
            gatePlacement.ObstructionPlacement(obst, ctrler.Session.CurrentBlock.settings.GetFloat("per_block_occlusion_location"), GameObject.Find("Spline"));
        }
        else{
            obst.SetActive(false);
        }
        
        //check if mirror on JSON is TRUE and mirror the track on the z-axis and changes the position and rotation of the gates so the track still runs clockwise
        if(ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_mirror")){
            track.transform.localScale = new Vector3(-1,1,1);
        }
        if(ctrler.Session.CurrentBlock.settings.GetFloat("per_block_track_rotation") != 0){
            track.transform.Rotate(0,ctrler.Session.CurrentBlock.settings.GetFloat("per_block_track_rotation"),0);
        }
        
        car.transform.position = trailGate1.transform.position;
        raycastOrigins.AddRange(car.GetComponentsInChildren<Transform>());

        gatePlacement.SetCheckeredFlags(trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(0).gameObject,
        trailGate1.transform.GetChild(1).gameObject);

        gatePlacement.SetCheckeredFlags(trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(0).gameObject,
        trailGate2.transform.GetChild(1).gameObject);

        gatePlacement.CarPlacement(car, startPoint, roadSegments);

    }

    public override float GetRotation()
    {
        return ctrler.Session.CurrentBlock.settings.GetFloat("per_block_rotation");
    }

    public void SetOcclusionInfo(bool isOccluded)
    {
        if(isOccluded){
            startOcclusionTime = Time.time;
        }
        else{
            endOcclusionTime = Time.time;
            occludedTime += endOcclusionTime - startOcclusionTime;
        }
    }

    void FixedUpdate()
    {
        carVelocity = (car.transform.position - carPrevPos);
        carPrevPos = car.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 fwd;
        float turnRatio;
        switch (currentStep)
        {
            case 0:
                // mouse is inside gate 1 collider
                if (trailGate1.transform.GetChild(3).GetComponent<BoxCollider>().ClosestPoint
                    (ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main)) ==
                    ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main))
                {
                    IncrementStep();
                }
                break;

            case 1:
                Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main);
                car.transform.position = ctrler.CursorController.ConvertPosition(mousePoint);

                isOnTrack = true;
                
                turnRatio = carVelocity.magnitude * 150 * Time.deltaTime;
                targetRotation = Quaternion.LookRotation(carVelocity.normalized) * Quaternion.Euler(0, -90, 0);
                car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, turnRatio);

                // Use raycasts to determine if car is on track
                foreach (Transform t in raycastOrigins)
                {
                    // if any rays don't hit a collider, then the car is at least partially off the track 
                    if (!Physics.Raycast(t.position, t.TransformDirection(Vector3.down))){
                        isOnTrack = false;

                        switch(t.name){
                            case "RF":
                                RF_OutPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                            case "LF":
                                LF_OutPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                            case "RR":
                                RR_OutPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                            case "LR":
                                LR_OutPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                        }
                        break;
                    }
                    else{
                        switch(t.name){
                            case "RF":
                                RF_InPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                            case "LF":
                                LF_InPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                            case "RR":
                                RR_InPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                            case "LR":
                                LR_InPath.Add(new Vector3(t.position.x, t.position.z, Time.time));
                                break;
                        }
                    }
                                
                }

                if (!isOnTrack )
                {
                    if(!hasSoundPlayed){
                        trailSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];
                        trailSpace.GetComponent<AudioSource>().Play();
                        car.GetComponent<MeshRenderer>().materials[4].color = Color.red;
                        score++;
                        scoreboard.SetElement("Demerit Points", score.ToString());
                        hasSoundPlayed = true;
                    }
                    outTrackPath.Add(new Vector3(car.transform.position.x, car.transform.position.z, Time.time));
                }
                else
                {
                    car.GetComponent<MeshRenderer>().materials[4].color = Color.white;
                    inTrackPath.Add(new Vector3(car.transform.position.x, car.transform.position.z, Time.time));
                    hasSoundPlayed = false;
                } 

                carPath.Add(new Vector2(car.transform.position.x, car.transform.position.z));
                cursorPath.Add(new Vector2(mousePoint.x, mousePoint.z));

                foreach (BaseTarget t in midwayTriggers)
                {
                    // if the car hits the midway trigger, it is going the correct way
                    if (t.Collided)
                        carPastMidpoint = true;
                }    
                

                // if the car hits the start gate trigger, it is not going the right way 
                if (startCollider.GetComponent<BaseTarget>().Collided)
                    carPastMidpoint = false;

                if (isOnTrack)
                    inTrackTime += Time.deltaTime;
                else
                    outTrackTime += Time.deltaTime;

                // checks if car has gone through finish line
                if(carPastMidpoint && trailGate2.transform.GetChild(3).GetComponent<BaseTarget>().Collided){
                    trailGate1.transform.GetChild(3).GetComponent<BaseTarget>().Collided = false;
                    trailGate2.GetComponentInChildren<ParticleSystem>().transform.position = trailGate2.transform.position;
                    trailGate2.GetComponentInChildren<ParticleSystem>().Play();
                    trailSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];
                    trailSpace.GetComponent<AudioSource>().Play();
                    IncrementStep();
                    car.GetComponent<MeshRenderer>().materials[4].color = Color.yellow;
                }
                onTrackFrameStatus.Add(isOnTrack);
                // scoreboard.SetElement("% on track", (inTrackTime / (inTrackTime + outTrackTime)*100).ToString("0.00"));
                // scoreboard.SetElement("Lap Time", (outTrackTime + inTrackTime).ToString("0.000"));
                break;
            case 2:
                pitStopTime += Time.deltaTime;
                mousePoint = ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main);
                car.transform.position = ctrler.CursorController.ConvertPosition(mousePoint);

                fwd = carVelocity.normalized + car.transform.position;
                turnRatio = carVelocity.magnitude * 150 * Time.deltaTime;
                targetRotation = Quaternion.LookRotation(fwd - car.transform.position) * Quaternion.Euler(0, -90, 0);
                car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, turnRatio);

                if(ctrler.GetBestLapTime() == 0 || ctrler.GetBestLapTime() > outTrackTime + inTrackTime || ctrler.Session.currentTrialNumInBlock == 1){
                    ctrler.SetLapDiff(ctrler.GetBestLapTime(), outTrackTime + inTrackTime);
                    ctrler.SetBestLapTime(outTrackTime + inTrackTime);
                    ctrler.SetLastLapTime(outTrackTime + inTrackTime);
                }
                else if (ctrler.GetBestLapTime() < outTrackTime + inTrackTime){
                    ctrler.SetLapDiff(ctrler.GetBestLapTime(), outTrackTime + inTrackTime);
                    ctrler.SetLastLapTime(outTrackTime + inTrackTime);
                }
                // scoreboard.SetElement("Lap Diff", ctrler.GetLapDiff());
                // scoreboard.SetElement("Best Lap", ctrler.GetBestLapTime().ToString("0.000"));
                if (trailGate1.transform.GetChild(3).GetComponent<BaseTarget>().Collided)
                {
                    IncrementStep();
                }
                break;
        }
        if (Finished) ctrler.EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        switch (currentStep)
        {
            case 0:
                // make the start trigger smaller after the car is picked up
                startCollider.size = new Vector3(startCollider.size.z, startCollider.size.y, 0.1f);

                ctrler.StartTimer();

                break;
            case 1:
                if (!carPastMidpoint){
                    trailSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];
                    trailSpace.GetComponent<AudioSource>().Play();
                    isRunValid = false;
                    return false;
                }
                break;
        }


        return base.IncrementStep();
    }

    public void Impact()
    {
        Debug.Log("Hit!");
        //IncrementStep();
    }

    public override void LogParameters()
    {
        float distanceOut = 0;
        float distanceIn = 0;
        for(int i = 0; i<outTrackPath.Count; i ++){
            if(i+1 < outTrackPath.Count)
                distanceOut += Vector3.Distance(outTrackPath[i], outTrackPath[i+1]);
        }
        for(int i = 0; i<inTrackPath.Count; i ++){
            if(i+1 < inTrackPath.Count)
                distanceIn += Vector3.Distance(inTrackPath[i], inTrackPath[i+1]);
        }
        ctrler.Session.CurrentTrial.result["per_block_type"] = ctrler.Session.CurrentBlock.settings.GetString("per_block_type");
        ctrler.Session.CurrentTrial.result["is_run_valid"] = isRunValid;
        ctrler.LogVector2List("cursor_path", cursorPath);
        ctrler.LogVector2List("car_path", carPath);
        ctrler.LogPositionTime("out_track_path", outTrackPath);
        ctrler.Session.CurrentTrial.result["distance_out_track"] = distanceOut;
        ctrler.LogPositionTime("in_track_path", inTrackPath);
        ctrler.Session.CurrentTrial.result["distance_in_track"] = distanceIn;
        ctrler.LogPositionTime("RF_in_track_path", RF_InPath);
        ctrler.LogPositionTime("RF_out_track_path", RF_OutPath);
        ctrler.LogPositionTime("LF_in_track_path", LF_InPath);
        ctrler.LogPositionTime("LF_out_track_path", LF_OutPath);
        ctrler.LogPositionTime("RR_in_track_path", RR_InPath);
        ctrler.LogPositionTime("RR_out_track_path", RR_OutPath);
        ctrler.LogPositionTime("LR_in_track_path", LR_InPath);
        ctrler.LogPositionTime("LR_out_track_path", LR_OutPath);
        ctrler.Session.CurrentTrial.result["time_on_track"] = inTrackTime;
        ctrler.Session.CurrentTrial.result["time_out_track"] = outTrackTime;
        ctrler.Session.CurrentTrial.result["percent_on_track"] = inTrackTime / (inTrackTime + outTrackTime)*100;
        ctrler.Session.CurrentTrial.result["lap_time"] = outTrackTime + inTrackTime;
        // ctrler.LogBoolList("on_track_per_frame_status", onTrackFrameStatus);

        ctrler.Session.CurrentTrial.result["demerit_points"] = score;

        ctrler.Session.CurrentTrial.result["start_gate_placement"] = startPoint;
        ctrler.Session.CurrentTrial.result["end_gate_placement"] = endPoint;
        ctrler.Session.CurrentTrial.result["occlusion_placement"] = ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_occlusion");
        ctrler.Session.CurrentTrial.result["occluded_time"] = occludedTime;
        ctrler.Session.CurrentTrial.result["pit_stop_time"] = pitStopTime;

    }

    public override void Disable()
    {
        // Realign XR Rig to non-tilted position
        if (ctrler.Session.settings.GetString("experiment_mode") == "trail_vr")
        {
           /* XRRig.transform.RotateAround(trailSpace.transform.position, trailSpace.transform.forward,
                ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt") * -1);*/
        }

        trailSpace.SetActive(false);
    }

    protected override void OnDestroy()
    {
        // foreach (BaseTarget t in midwayTriggers)
        // {
        //     Destroy(t.gameObject);
        // }
        Destroy(trailSpace);
    }
}
