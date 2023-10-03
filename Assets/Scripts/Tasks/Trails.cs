using System;
using System.Collections;
using System.Collections.Generic;
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
    private List<BaseTarget> innerTrackColliders = new List<BaseTarget>();

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
    private const int NUM_MID_TRIGGERS = 2;

    private List<BaseTarget> midwayTriggers = new List<BaseTarget>();

    // Whether to use raycasts or use the inner track to dermine whether offtrack
    private bool useRayCasts = false;

    private float inTrackTime, outTrackTime;
    private List<bool> onTrackFrameStatus = new List<bool>();
    private List<Vector2> carPath = new List<Vector2>();
    private List<Vector2> cursorPath = new List<Vector2>();

    [SerializeField]
    private int score;
    private Scoreboard scoreboard;

    private string lapDiff = "-.--";
    private string bestLap = "-.--";
    private string lastLap = "-.--";
    private Dictionary<string, string> scoreboardInfo = new Dictionary<string, string>();

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
                print("rotated");
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
            trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(3).GetComponent<BoxCollider>(), startPoint, GameObject.Find("Spline"));

        endPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_endPoint");
        gatePlacement.SetGatePosition(trailGate2, trailGate2.transform.GetChild(0).gameObject, trailGate2.transform.GetChild(1).gameObject,
            trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(3).GetComponent<BoxCollider>(), endPoint, GameObject.Find("Spline"));

        if(ctrler.GetBestLapTime() != 0){
            lapDiff = ctrler.GetLapDiff();
            bestLap = ctrler.GetBestLapTime().ToString("0.000");
            lastLap = ctrler.GetLastLapTime().ToString("0.000");
        }

        scoreboardInfo.Add("Demerit Points", score.ToString());
        scoreboardInfo.Add("Lap Time", lastLap);
        scoreboardInfo.Add("Best Lap", bestLap);
        scoreboardInfo.Add("Lap Diff", lapDiff);
        scoreboardInfo.Add("% on track", score.ToString());

        scoreboard.SetElements(scoreboardInfo);

        // Place midway triggers throughout the track
        for (int i = 0; i < NUM_MID_TRIGGERS; i++)
        {
            midwayTriggers.Add(Instantiate(midwayCollider.gameObject).GetComponent<BaseTarget>());

            // Start    Mid1     Mid2     End
            // |--------|--------|--------|
            if (endPoint < startPoint)
            {
                // If the end point comes before the start point

                float distance = 1 - startPoint + endPoint;

                midPoint = ((distance) / (NUM_MID_TRIGGERS + 1)) * (i + 1) + startPoint;

                if (midPoint > 1)
                    midPoint -= 1;
            }
            else
            {
                float distance = endPoint - startPoint;

                midPoint = ((distance) / (NUM_MID_TRIGGERS + 1)) * (i + 1) + startPoint;
            }

            gatePlacement.SetColliderPosition(midwayTriggers[i].GetComponent<BoxCollider>(), midPoint, GameObject.Find("Spline"));
            midwayTriggers[i].transform.parent = track.transform;
        }


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

        innerTrackColliders.AddRange(GameObject.Find("innertrack").transform.GetComponentsInChildren<BaseTarget>());

        if (ctrler.Session.currentTrialNum > 1)
        {
            trailGate1.GetComponentInChildren<ParticleSystem>().transform.position = trailGate1.transform.position;
            trailGate1.GetComponentInChildren<ParticleSystem>().transform.rotation = trailGate1.transform.rotation;
            trailGate1.GetComponentInChildren<ParticleSystem>().Play();
            trailSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];
            trailSpace.GetComponent<AudioSource>().Play();
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
            Vector3 tempPos1 = trailGate1.transform.position;
            Quaternion tempRot1 = trailGate1.transform.rotation;
            trailGate1.transform.position = trailGate2.transform.position;
            trailGate1.transform.rotation = trailGate2.transform.rotation;
            trailGate2.transform.rotation = tempRot1;
            trailGate2.transform.position = tempPos1;
        }
        car.transform.position = trailGate1.transform.position;
        raycastOrigins.AddRange(car.GetComponentsInChildren<Transform>());

        gatePlacement.SetCheckeredFlags(trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(0).gameObject,
        trailGate1.transform.GetChild(1).gameObject);

        gatePlacement.SetCheckeredFlags(trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(0).gameObject,
        trailGate2.transform.GetChild(1).gameObject);

    }

    public override float GetRotation()
    {
        return ctrler.Session.CurrentBlock.settings.GetFloat("per_block_rotation");
    }

    private void FixedUpdate()
    {
        switch (currentStep)
        {
            case 0:
                break;

            case 1:

                bool onTrack;
                if (useRayCasts)
                {
                    onTrack = true;
                    // Use raycasts to determine if car is on track
                    foreach (Transform t in raycastOrigins)
                    {
                        // if any rays don't hit a collider, then the car is at least partially off the track 
                        if (!Physics.Raycast(t.position, t.TransformDirection(Vector3.down)))
                            onTrack = false;
                    }
                }
                else
                {
                    onTrack = false;
                    // Use inner track to determine if car (must be a cylinder with 0.5 scale) is on the track
                    foreach (BaseTarget innerTrackSegment in innerTrackColliders)
                    {
                        // if the cylinder is at least slightly touching any inner track segment, then the car is still on the main track.
                        if (innerTrackSegment.Collided)
                        {
                            onTrack = true;
                        }
                    }
                }

                if (isOnTrack && !onTrack)
                {
                    isOnTrack = onTrack;
                    numImpacts++;
                    car.GetComponent<MeshRenderer>().material.color = Color.red;
                    score = numImpacts;
                    trailSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];
                    trailSpace.GetComponent<AudioSource>().Play();
                }
                else if (!isOnTrack && onTrack)
                {
                    isOnTrack = onTrack;
                    car.GetComponent<MeshRenderer>().material.color = Color.white;
                }    

                break;
        }

        
    }

    // Update is called once per frame
    void Update()
    {
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
                carPath.Add(new Vector2(car.transform.position.x, car.transform.position.z));
                cursorPath.Add(new Vector2 (ctrler.CursorController.GetHandPosition().x, ctrler.CursorController.GetHandPosition().z));

                foreach (BaseTarget t in midwayTriggers)
                {
                    // if the car hits the midway trigger, it is going the correct way
                    if (t.Collided)
                        carPastMidpoint = true;
                }    
                

                // if the car hits the start gate trigger, it is not going the right way 
                if (startCollider.GetComponent<BaseTarget>().Collided)
                    carPastMidpoint = false;

                // car position = mouse position
                Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main);
                car.transform.position = ctrler.CursorController.ConvertPosition(mousePoint);

                if (isOnTrack)
                    inTrackTime += Time.deltaTime;
                else
                    outTrackTime += Time.deltaTime;

                // checks if car has gone through finish line
                if(carPastMidpoint && trailGate2.transform.GetChild(3).GetComponent<BaseTarget>().Collided){
                    IncrementStep();
                }
                onTrackFrameStatus.Add(isOnTrack);
                break;
            case 2:
                if(ctrler.GetBestLapTime() == 0 || ctrler.GetBestLapTime() > outTrackTime + inTrackTime || ctrler.Session.currentTrialNumInBlock == 1){
                    ctrler.SetLapDiff(ctrler.GetBestLapTime(), outTrackTime + inTrackTime);
                    ctrler.SetBestLapTime(outTrackTime + inTrackTime);
                    ctrler.SetLastLapTime(outTrackTime + inTrackTime);
                }
                else if (ctrler.GetBestLapTime() < outTrackTime + inTrackTime){
                    ctrler.SetLapDiff(ctrler.GetBestLapTime(), outTrackTime + inTrackTime);
                    ctrler.SetLastLapTime(outTrackTime + inTrackTime);
                }
                IncrementStep();
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
                if (!carPastMidpoint)
                    return false;

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
        ctrler.Session.CurrentTrial.result["per_block_type"] = ctrler.Session.CurrentBlock.settings.GetString("per_block_type");
        ctrler.LogVector2List("car_path", carPath);
        ctrler.LogVector2List("cursor_path", cursorPath);
        ctrler.Session.CurrentTrial.result["time_on_track"] = inTrackTime;
        ctrler.Session.CurrentTrial.result["time_out_track"] = outTrackTime;
        ctrler.Session.CurrentTrial.result["percent_on_track"] = inTrackTime / (inTrackTime + outTrackTime)*100;
        ctrler.Session.CurrentTrial.result["lap_time"] = outTrackTime + inTrackTime;
        // ctrler.LogBoolList("on_track_per_frame_status", onTrackFrameStatus);

        ctrler.Session.CurrentTrial.result["demerit_points"] = score;

        ctrler.Session.CurrentTrial.result["start_gate_placement"] = startPoint;
        ctrler.Session.CurrentTrial.result["start_gate_placement"] = endPoint;
        ctrler.Session.CurrentTrial.result["oclusion_placement"] = ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_occlusion");

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
