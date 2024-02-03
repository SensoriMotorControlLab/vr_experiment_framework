using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    private GameObject GridLayout;

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
    private int numMidTriggersHit = 0;

    private List<BaseTarget> midwayTriggers = new List<BaseTarget>();

    // Whether to use raycasts or use the inner track to dermine whether offtrack
    private bool useRayCasts = true;

    private float inTrackTime, outTrackTime, pitStopTime;
    private float startOcclusionTime, endOcclusionTime, occludedTime;
    private List<bool> onTrackFrameStatus = new List<bool>();
    private List<Vector2> carPath = new List<Vector2>();
    private List<Vector3> cursorPath = new List<Vector3>();
    private List<Vector3> outTrackPath = new List<Vector3>();
    private List<Vector3> inTrackPath = new List<Vector3>();
    private List<Vector2> RF_Path = new List<Vector2>();
    private List<float> RF_OutPathTime = new List<float>();
    private List<float> RF_InPathTime = new List<float>();
    private List<Vector2> LF_Path = new List<Vector2>();
    private List<float> LF_OutPathTime = new List<float>();
    private List<float> LF_InPathTime = new List<float>();
    private List<Vector2> RR_Path = new List<Vector2>();
    private List<float> RR_OutPathTime = new List<float>();
    private List<float> RR_InPathTime = new List<float>();
    private List<Vector2> LR_Path = new List<Vector2>();
    private List<float> LR_OutPathTime = new List<float>();
    private List<float> LR_InPathTime = new List<float>();

    [SerializeField]
    private int score;
    private Scoreboard scoreboard;

    private string lapDiff = "-.--";
    private string bestLap = "-.--";
    private string lastLap = "-.--";
    private Dictionary<string, string> scoreboardInfo = new Dictionary<string, string>();
    private bool hasSoundPlayed = false;
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
        GridLayout = GameObject.Find("GridLayoutGroup");

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
        endPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_endPoint");
        startPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_startPoint");
        bool isInverted = (startPoint > endPoint) ? true : false;
        gatePlacement.SetGatePosition(trailGate1, trailGate1.transform.GetChild(0).gameObject, trailGate1.transform.GetChild(1).gameObject,
            trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(3).GetComponent<BoxCollider>(), startPoint, roadSegments, isInverted);

        endPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_endPoint");
        gatePlacement.SetGatePosition(trailGate2, trailGate2.transform.GetChild(0).gameObject, trailGate2.transform.GetChild(1).gameObject,
            trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(3).GetComponent<BoxCollider>(), endPoint, roadSegments, isInverted);

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
        scoreboardInfo.Add("% on track", score.ToString());

        scoreboard.SetElements(scoreboardInfo);

        // set up how many midway triggers to place according to the distance between start and end point
        if (endPoint < startPoint)
        midTriggersCounter = Mathf.RoundToInt((endPoint - startPoint + 1) * 10);
        else
        midTriggersCounter = Mathf.RoundToInt((endPoint - startPoint) *10 );

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
            trailGate1.transform.localScale = new Vector3(-1,1,1);
            trailGate2.transform.localScale = new Vector3(-1,1,1);
            track.transform.localScale = new Vector3(-1,1,1);
        }
        if(ctrler.Session.CurrentBlock.settings.GetFloat("per_block_track_orientation") != 0){
            track.transform.Rotate(0,ctrler.Session.CurrentBlock.settings.GetFloat("per_block_track_orientation"),0);
        }
        
        car.transform.position = trailGate1.transform.position;
        raycastOrigins.AddRange(car.GetComponentsInChildren<Transform>());

        gatePlacement.SetCheckeredFlags(trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(0).gameObject,
        trailGate1.transform.GetChild(1).gameObject);

        gatePlacement.SetCheckeredFlags(trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(0).gameObject,
        trailGate2.transform.GetChild(1).gameObject);

        gatePlacement.CarPlacement(car, startPoint, roadSegments);

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

        // create textmeshpro object for playerprefs to store median laptime and percent on track of all the blocks separately until the current block
        if(ctrler.Session.currentBlockNum > 1 && ctrler.Session.currentTrialNumInBlock == 1)
        {
            for(int i = 0; i < ctrler.Session.currentBlockNum - 1; i++){

                GameObject medianPercentOnTrack = new GameObject("medianPercentOnTrack");
                medianPercentOnTrack.transform.parent = GridLayout.transform;
                medianPercentOnTrack.transform.localScale = new Vector3(1,1,1);
                medianPercentOnTrack.transform.localRotation = Quaternion.Euler(0,0,0);
                medianPercentOnTrack.AddComponent<TextMeshPro>();
                medianPercentOnTrack.GetComponent<TextMeshPro>().text = ctrler.pBlockMedPercentTrackList[i].ToString();
                medianPercentOnTrack.GetComponent<TextMeshPro>().fontSize = 90;
                medianPercentOnTrack.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
                Color TextColor = Color.white;
                if(i-1 >= 0)
                {
                    TextColor = (ctrler.pBlockMedPercentTrackList[i-1] < ctrler.pBlockMedPercentTrackList[i]) ? Color.green : Color.red;
                    TextColor = (ctrler.pBlockMedPercentTrackList[i-1] == ctrler.pBlockMedPercentTrackList[i]) ? Color.white : TextColor;
                }
                medianPercentOnTrack.GetComponent<TextMeshPro>().color = TextColor;

                GameObject medianLapTime = new GameObject("medianLapTime");
                medianLapTime.transform.parent = GridLayout.transform;
                medianLapTime.transform.localScale = new Vector3(1,1,1);
                medianLapTime.transform.localRotation = Quaternion.Euler(0,0,0);
                medianLapTime.AddComponent<TextMeshPro>();
                medianLapTime.GetComponent<TextMeshPro>().text = ctrler.pBlockMedLapTimeList[i].ToString();
                medianLapTime.GetComponent<TextMeshPro>().fontSize = 90;
                medianLapTime.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
                TextColor = Color.white;
                if(i-1 >= 0)
                {
                    TextColor = (ctrler.pBlockMedLapTimeList[i-1] > ctrler.pBlockMedLapTimeList[i]) ? Color.green : Color.red;
                    TextColor = (ctrler.pBlockMedLapTimeList[i-1] == ctrler.pBlockMedLapTimeList[i]) ? Color.white : TextColor;
                }
                medianLapTime.GetComponent<TextMeshPro>().color = TextColor;

                // now for playerprefs demerit points
                GameObject demeritPoints = new GameObject("demeritPoints");
                demeritPoints.transform.parent = GridLayout.transform;                
                demeritPoints.transform.localScale = new Vector3(1,1,1);
                demeritPoints.transform.localRotation = Quaternion.Euler(0,0,0);
                demeritPoints.AddComponent<TextMeshPro>();
                demeritPoints.GetComponent<TextMeshPro>().text = ctrler.pBlockTotalDemeritsList[i].ToString();
                demeritPoints.GetComponent<TextMeshPro>().fontSize = 90;
                demeritPoints.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
                TextColor = Color.white;
                if(i-1 >= 0)
                {
                    TextColor = (ctrler.pBlockTotalDemeritsList[i-1] > ctrler.pBlockTotalDemeritsList[i]) ? Color.green : Color.red;
                    TextColor = (ctrler.pBlockTotalDemeritsList[i-1] == ctrler.pBlockTotalDemeritsList[i]) ? Color.white : TextColor;
                }
                demeritPoints.GetComponent<TextMeshPro>().color = TextColor;

                GameObject Set = new GameObject("Set");
                Set.transform.parent = GridLayout.transform;
                Set.transform.localScale = new Vector3(1,1,1);
                Set.transform.localRotation = Quaternion.Euler(0,0,0);
                Set.AddComponent<TextMeshPro>();
                Set.GetComponent<TextMeshPro>().text = (i+1).ToString();
                Set.GetComponent<TextMeshPro>().fontSize = 90;
                Set.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
                Set.GetComponent<TextMeshPro>().color = Color.white;
            }
        }
        else
            GridLayout.SetActive(false);


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
                if (carVelocity.normalized != Vector3.zero)
                {
                    targetRotation = Quaternion.LookRotation(carVelocity.normalized) * Quaternion.Euler(0, -90, 0);
                    car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, turnRatio);
                }

                // Use raycasts to determine if car is on track
                foreach (Transform t in raycastOrigins)
                {
                    // if any rays don't hit a collider, then the car is at least partially off the track 
                    if (!Physics.Raycast(t.position, t.TransformDirection(Vector3.down)) || t.transform.tag == "Grass"){
                        isOnTrack = false;

                        switch(t.name){
                            case "RF":
                                RF_OutPathTime.Add(Time.time);
                                break;
                            case "LF":
                                LF_OutPathTime.Add(Time.time);
                                break;
                            case "RR":
                                RR_OutPathTime.Add(Time.time);
                                break;
                            case "LR":
                                LR_OutPathTime.Add(Time.time);
                                break;
                        }
                    }
                    else
                    {
                        switch(t.name){
                            case "RF":
                                RF_InPathTime.Add(Time.time);
                                break;
                            case "LF":
                                LF_InPathTime.Add(Time.time);
                                break;
                            case "RR":
                                RR_InPathTime.Add(Time.time);
                                break;
                            case "LR":
                                LR_InPathTime.Add(Time.time);
                                break;
                        }
                    }

                    switch(t.name)
                    {
                        case "RF":
                            RF_Path.Add(new Vector3(t.position.x, t.position.z));
                            break;
                        case "LF":
                            LF_Path.Add(new Vector3(t.position.x, t.position.z));
                            break;
                        case "RR":
                            RR_Path.Add(new Vector3(t.position.x, t.position.z));
                            break;
                        case "LR":
                            LR_Path.Add(new Vector3(t.position.x, t.position.z));
                            break;
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
                cursorPath.Add(new Vector3(mousePoint.x, mousePoint.z, Time.time));

                foreach (BaseTarget t in midwayTriggers)
                {
                    // if the car hits the midway trigger, it is going the correct way
                    if (t.Collided && !t.hasCollided)
                    {
                        t.hasCollided = true;
                        numMidTriggersHit++;
                        if(numMidTriggersHit >= (midwayTriggers.Count/2)+1)
                        {
                            carPastMidpoint = true;
                        }
                        else if(midwayTriggers.Count == 1)
                        {
                            carPastMidpoint = true;
                        }
                        else
                        {
                            carPastMidpoint = false;
                        }
                        // Debug.Log("Car past midpoint: " + carPastMidpoint);
                    }
                        
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
                scoreboard.SetElement("% on track", (inTrackTime / (inTrackTime + outTrackTime)*100).ToString("0.00"));
                scoreboard.SetElement("Lap Time", (outTrackTime + inTrackTime).ToString("0.000"));
                break;
            case 2:
                pitStopTime += Time.deltaTime;
                mousePoint = ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main);
                car.transform.position = ctrler.CursorController.ConvertPosition(mousePoint);

                turnRatio = carVelocity.magnitude * 150 * Time.deltaTime;
                if (carVelocity.normalized != Vector3.zero)
                {
                    targetRotation = Quaternion.LookRotation(carVelocity.normalized) * Quaternion.Euler(0, -90, 0);
                    car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, turnRatio);
                }

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
                ctrler.StartTimer();
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
        float percentOnTrack = inTrackTime / (inTrackTime + outTrackTime)*100;
        float lapTime = outTrackTime + inTrackTime;
        
        int currentBlockNum = ctrler.Session.currentBlockNum;
        int currentTrialNum = ctrler.Session.currentTrialNumInBlock;
   
        if(currentTrialNum == 1)
        {
            ctrler.currBlockLapTimeList.Clear();
            ctrler.currBlockPercentOnTrackList.Clear();
            ctrler.currBlockTotalDemerits = 0;
        }           
        ctrler.currBlockTotalDemerits += score;
        ctrler.currBlockLapTimeList.Add(lapTime);
        ctrler.currBlockPercentOnTrackList.Add(percentOnTrack);
        if(currentTrialNum == ctrler.Session.CurrentBlock.trials.Count)
        {
            //put all saved laptime and percent on track into separate lists
            List<float> lapTimeList = new List<float>();
            List<float> percentOnTrackList = new List<float>();

            //get the median of the laptime and percent on track
            lapTimeList = ctrler.currBlockLapTimeList.OrderBy(x => x).ToList();
            percentOnTrackList = ctrler.currBlockPercentOnTrackList.OrderBy(x => x).ToList();

            float medianLapTime;
            float medianPercentOnTrack;

            if(lapTimeList.Count() == 1 && percentOnTrackList.Count() == 1){
                medianLapTime = lapTimeList[0];
                medianPercentOnTrack = percentOnTrackList[0];
            }
            else
            {
                int medianIndexTime = lapTimeList.Count()/2;
                int medianIndexPercent = percentOnTrackList.Count()/2;
                medianLapTime = lapTimeList[medianIndexTime];
                medianPercentOnTrack = percentOnTrackList[medianIndexPercent];
            }

            ctrler.pBlockMedLapTimeList.Add(medianLapTime);
            ctrler.pBlockMedPercentTrackList.Add(medianPercentOnTrack);
            ctrler.pBlockTotalDemeritsList.Add(ctrler.currBlockTotalDemerits);
        }
       
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
        ctrler.LogVector3List("cursor_path", cursorPath);
        ctrler.LogVector2List("car_path", carPath);
        ctrler.LogPositionTime("out_track_path", outTrackPath);
        ctrler.Session.CurrentTrial.result["distance_out_track"] = distanceOut;
        ctrler.LogPositionTime("in_track_path", inTrackPath);
        ctrler.Session.CurrentTrial.result["distance_in_track"] = distanceIn;
        ctrler.LogVector2List("RFW_in_track_path", RF_Path);
        ctrler.LogList("RFW_in_track_time", RF_InPathTime);
        ctrler.LogList("RFW_out_track_time", RF_OutPathTime);
        ctrler.LogVector2List("LFW_in_track_path", LF_Path);
        ctrler.LogList("LFW_in_track_time", LF_InPathTime);
        ctrler.LogList("LFW_out_track_time", LF_OutPathTime);
        ctrler.LogVector2List("RRW_in_track_path", RR_Path);
        ctrler.LogList("RRW_in_track_time", RR_InPathTime);
        ctrler.LogList("RRW_out_track_time", RR_OutPathTime);
        ctrler.LogVector2List("LRW_in_track_path", LR_Path);
        ctrler.LogList("LRW_in_track_time", LR_InPathTime);
        ctrler.LogList("LRW_out_track_time", LR_OutPathTime);
        ctrler.Session.CurrentTrial.result["time_on_track"] = inTrackTime;
        ctrler.Session.CurrentTrial.result["time_out_track"] = outTrackTime;
        ctrler.Session.CurrentTrial.result["percent_on_track"] = percentOnTrack;
        ctrler.Session.CurrentTrial.result["lap_time"] = lapTime;
        // ctrler.LogBoolList("on_track_per_frame_status", onTrackFrameStatus);

        ctrler.Session.CurrentTrial.result["demerit_points"] = score;

        ctrler.Session.CurrentTrial.result["start_gate_placement"] = startPoint;
        ctrler.Session.CurrentTrial.result["end_gate_placement"] = endPoint;
        ctrler.Session.CurrentTrial.result["occlusion_placement"] = ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_occlusion");
        ctrler.Session.CurrentTrial.result["occluded_time"] = occludedTime;
        ctrler.Session.CurrentTrial.result["pit_stop_time"] = pitStopTime;
        ctrler.Session.CurrentTrial.result["Track_orientation"] = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_track_orientation");

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
