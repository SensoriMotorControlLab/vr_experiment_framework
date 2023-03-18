using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;
using TMPro;
using System;

public class ReachTrack : ReachToTargetTask
{
    protected GameObject field;
    protected GameObject goal;
    //protected Vector3 mousePoint;
    protected GameObject baseObject;
    protected GameObject symbols;
    protected int velResult;
    protected AudioSource sound;
    protected GameObject ray;
    protected List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    GameObject speedometer;

    Vector3 vel = new Vector3();
    Vector3 prev = new Vector3();
    Vector3 cur = new Vector3();
    float dist;
    float angle;
    Vector3 rotationAxis = new Vector3();

    float hold_still_time = 0.3f;
    protected float req_targ_accuracy = 0.005f;


    float maxVel = 0;
    Vector3 newPos;
    Vector3 prevPos;
    float curVel;
    TextMeshPro text;
    float idealVel;
    float idealUpperBound;
    float idealLowerBound;
    float maxUpperVel;
    float minLowerVel;
    bool idealReached = false;
    bool hasPlayed = false;
    float originalDist;
    float curDist;
    float fieldLength;
    bool hasRotated = false;
    int scoreTrack;
    bool wasOutside = false;

    float timer = 0.5f;
    List<Vector4> handPos = new List<Vector4>();
    Vector4 tempHandPos = new Vector4(0, 0, 0, 0);
    public float rotation = 0;
    Vector2 pos_3cm_out = new Vector2(0, 0);
    bool outEvent = true;
    protected List<GameObject> targets = new List<GameObject>();


    // Start is called before the first frame update
    public override void Setup()
    {
        maxSteps = 5;
        ctrler = ExperimentController.Instance();
        trial = ctrler.Session.CurrentTrial;
        d = null;

        Cursor.visible = false;

        reachPrefab = Instantiate(ctrler.GetPrefab("ReachPrefab"));
        //reachPrefab.transform.SetParent(ctrler.transform);
        reachPrefab.transform.position = Vector3.zero;
        ctrler.TargetContainer.transform.localPosition = Vector3.zero;

        reachCam = GameObject.Find("ReachCam");
        reachSurface = GameObject.Find("Surface");
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        tint = GameObject.Find("Tint");
        field = GameObject.Find("soccer");
        goal = field.transform.GetChild(0).gameObject;
        baseObject = GameObject.Find("BaseObject");
        sound = baseObject.GetComponent<AudioSource>();
        ray = GameObject.Find("Ray");
        speedometer = GameObject.Find("speedometer");
        speedometer.transform.parent = field.transform;


        
        
        // Set up hand and cursor
        ctrler.CursorController.SetHandVisibility(false);
        ctrler.CursorController.SetCursorVisibility(true);

        // Set up the dock position
        targets.Add(GameObject.Find("Dock"));
        targets[0].transform.localPosition = ctrler.TargetContainer.transform.localPosition - ctrler.transform.forward * dock_dist;

        // Set up the home position
        targets.Add(GameObject.Find("Home"));
        targets[1].transform.localPosition = ctrler.TargetContainer.transform.localPosition;
        targets[1].SetActive(false);
        Home = targets[1];

        targets.Add(GameObject.Find("Target"));
        targets[2].transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        targets[2].transform.localPosition = targets[1].transform.localPosition +
                                        targets[2].transform.forward.normalized *
                                        (trial.settings.GetFloat("per_block_distance") / 100f);

        newPos = base.transform.position;
        prevPos = base.transform.position;

        idealVel = 0.4f;
        idealUpperBound = idealVel + 0.05f;
        idealLowerBound = idealVel - 0.05f;
        maxUpperVel = idealVel + 0.2f;
        minLowerVel = idealVel - 0.2f;

        field.SetActive(false);

        //SetSetup();

        field.transform.position = new Vector3(targets[1].transform.position.x, field.transform.position.y, targets[1].transform.position.z);

        ctrler.CursorController.Model.GetComponent<MeshRenderer>().enabled = false;

        field.transform.rotation = Quaternion.Euler(
            0f, -targetAngle - 90f, 0f);

        if (!ctrler.Session.settings.GetObjectList("optional_params").Contains("vr"))
        {
            speedometer.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        speedometer.transform.parent = reachPrefab.transform;
        speedometer.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
        speedometer.SetActive(false);

        originalDist = goal.transform.position.magnitude - field.transform.position.magnitude;

        goal.transform.position =
        new Vector3(targets[2].transform.position.x,
        targets[2].transform.position.y - 0.005f, targets[2].transform.position.z);

        curDist = goal.transform.position.magnitude - field.transform.position.magnitude;
        fieldLength = (((curDist * 100) / originalDist) * 0.01f) + 0.2f;
        goal.transform.parent = null;
        field.transform.localScale = new Vector3(field.transform.localScale.x, field.transform.localScale.y, field.transform.localScale.z * fieldLength);

        goal.transform.parent = field.transform;

        float width = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_width");
        field.transform.localScale = new Vector3(field.transform.localScale.x * width, field.transform.localScale.y, field.transform.localScale.z);

        targets[2].GetComponent<BaseTarget>().CollisionModeOnly = true;

        switch (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials"))
        {

            case "Glass":
                reachSurface.GetComponent<MeshRenderer>().material = ctrler.Materials["Glass"];
                break;
            case "Ice":
                reachSurface.GetComponent<MeshRenderer>().material = ctrler.Materials["Ice"];
                break;
            case "Pavement":
                reachSurface.GetComponent<MeshRenderer>().material = ctrler.Materials["Pavement"];
                break;
            case "Marble":
                reachSurface.GetComponent<MeshRenderer>().material = ctrler.Materials["Marble"];
                break;
            case "BrickMat":
                reachSurface.GetComponent<MeshRenderer>().material = ctrler.Materials["BrickMat"];
                break;
        }

        // ctrler.AddTrackedPosition("ball_path", baseObject);
        //ctrler.AddTrackedBool("is_ball_outside", wasOutside);
        ctrler.CursorController.useHand = false;

    }



    // Update is called once per frame
    public override void Update()
    {
        base.Update();

        UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);
        if (currentStep > 1)
        {
            field.SetActive(true);
            //VelocityTrack();
        }

        Vector3 mousePoint = GetMousePoint(baseObject.transform);
        Vector3 mousePlane = new Vector3(ctrler.CursorController.Model.transform.position.x, mousePoint.y, ctrler.CursorController.Model.transform.position.z);
        baseObject.transform.position = ctrler.CursorController.ConvertPosition(mousePlane);

        cur = baseObject.transform.localPosition;
        vel = (cur - prev) / Time.deltaTime;
        dist = vel.magnitude;
        angle = dist * (180f / Mathf.PI);
        prev = cur;
        rotationAxis = Vector3.Cross(Vector3.up, vel).normalized;
        //if (dist > 0.0000000001f)
        //{
        baseObject.transform.localRotation = Quaternion.Euler(rotationAxis * angle) * baseObject.transform.localRotation;
        //}
        //VelocityTrack();

        switch (currentStep)
        {
            case 0:
                if (!ctrler.Session.settings.GetObjectList("optional_params").Contains("return_visible"))
                {
                    // make the ball invisible
                    baseObject.GetComponent<Renderer>().enabled = false;
                }

                if (Mathf.Abs(targets[0].transform.localPosition.magnitude - baseObject.transform.localPosition.magnitude) < req_targ_accuracy
                && ctrler.CursorController.stillTime > hold_still_time)
                {
                    // make the ball visible
                    baseObject.GetComponent<Renderer>().enabled = true;
                    IncrementStep();
                }
                break;
            case 1:
                
                if (Mathf.Abs(targets[1].transform.localPosition.magnitude - baseObject.transform.localPosition.magnitude) < req_targ_accuracy
                && ctrler.CursorController.stillTime > hold_still_time)
                {
                    IncrementStep();
                }
                break;
            case 2:
                if(outEvent){
                    if(ctrler.CursorController.DistanceFromHome > 0.03){
                        pos_3cm_out = new Vector2(Vector3.Angle(targets[1].transform.right, ctrler.CursorController.transform.localPosition.normalized), Time.time);
                        outEvent = false;
                    }
                }
                tempHandPos = ctrler.CursorController.transform.position;
                handPos.Add(new Vector4(tempHandPos.x, tempHandPos.y, tempHandPos.z, Time.time));
                ray.transform.position = baseObject.transform.position;
                if (Physics.Raycast(ray.transform.position, -ray.transform.up, out RaycastHit hit, 0.1f))
                {
                    if (hit.collider.gameObject.name == "Surface" && !hasPlayed)
                    {
                        baseObject.GetComponent<Renderer>().material.color = Color.gray;
                        sound.clip = ctrler.AudioClips["incorrect"];
                        sound.Play();
                        hasPlayed = true;
                        wasOutside = true;
                        VibrateController(0, 0.34f, 0.15f, devices);
                    }
                    else if (hit.collider.gameObject.name == "soccer")
                    {
                        baseObject.GetComponent<Renderer>().material.color = Color.white;
                        hasPlayed = false;
                    }
                }
                if (ctrler.CursorController.stillTime > hold_still_time &&
                Mathf.Abs(targets[2].transform.localPosition.magnitude - baseObject.transform.localPosition.magnitude) < req_targ_accuracy)
                {
                    if (!wasOutside)
                    {
                        sound.clip = ctrler.AudioClips["correct"];
                        sound.Play();
                    }
                    if (trackScore)
                    {
                        if (wasOutside)
                        {
                            scoreTrack = 0;
                        }
                        else
                        {
                            scoreTrack = 5;
                        }
                        ctrler.Score += scoreTrack;
                    }
                    IncrementStep();
                }
                break;

            case 4:
                if (ctrler.Session.settings.GetObjectList("optional_params").Contains("return_visible"))
                {
                    targets[1].SetActive(true);
                    if (Mathf.Abs(targets[1].transform.localPosition.magnitude - baseObject.transform.localPosition.magnitude) < req_targ_accuracy
                        && ctrler.CursorController.stillTime > hold_still_time)
                    {
                        IncrementStep();
                    }
                }
                else
                {
                    timer = 1f;
                    StartCoroutine(Wait());
                }

                break;
        }

        if (currentStep > 2 && !hasRotated)
        {
            speedometer.transform.position = goal.transform.position + new Vector3(0, 0.02f, 0);
            hasRotated = true;

            // switch (velResult)
            // {
            //     case 75:
            //         speedometer.transform.GetChild(4).GetComponent<TextMeshPro>().text = "Too Fast";
            //         break;
            //     case 35:
            //         speedometer.transform.GetChild(4).GetComponent<TextMeshPro>().text = "Slow down a bit.";
            //         break;
            //     case 0:
            //         speedometer.transform.GetChild(4).GetComponent<TextMeshPro>().text = "Perfect speed!";
            //         break;
            //     case -35:
            //         speedometer.transform.GetChild(4).GetComponent<TextMeshPro>().text = "speed up a bit.";
            //         break;
            //     case -75:
            //         speedometer.transform.GetChild(4).GetComponent<TextMeshPro>().text = "Too Slow";
            //         break;
            // }

            if (trackScore)
            {
                speedometer.SetActive(true);
                speedometer.transform.GetChild(3).GetComponent<TextMeshPro>().text = "+" + scoreTrack.ToString();
            }
            else
            {
                speedometer.transform.GetChild(3).GetComponent<TextMeshPro>().text = "";
            }

            //symbols.GetComponent<Animator>().SetTrigger("rot");
            StartCoroutine(Wait());
        }
        
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(timer);
        base.IncrementStep();
        LogParameters();
    }

    public override float GetRotation()
    {
        return rotation;
    }

    void VelocityTrack()
    {
        newPos = baseObject.transform.position;
        curVel = ((newPos - prevPos) / Time.deltaTime).magnitude;
        prevPos = newPos;

        if (currentStep == 2)
        {
            if (maxVel < curVel)
            {
                maxVel = curVel;
            }
            if (idealReached)
            {
                velResult = 0;
            }
            else if (maxVel > maxUpperVel)
            {
                velResult = 75;
                scoreTrack = 1;
            }
            else if (maxVel > idealUpperBound && maxVel < maxUpperVel)
            {
                velResult = 35;
                scoreTrack = 2;
            }
            else if (maxVel < idealUpperBound && maxVel > idealLowerBound)
            {
                idealReached = true;
                scoreTrack = 5;
            }
            else if (maxVel < idealLowerBound && maxVel > minLowerVel)
            {
                velResult = -35;
                scoreTrack = 2;
            }
            else if (maxVel < minLowerVel)
            {
                velResult = -75;
                scoreTrack = 1;
            }

        }
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

    public override void LogParameters()
    {
        Session session = ctrler.Session;

        session.CurrentTrial.result["type"] = session.CurrentTrial.settings.GetString("per_block_type");
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
        session.CurrentTrial.result["pos_3cm_out_angle"] = pos_3cm_out.x;
        session.CurrentTrial.result["pos_3cm_out_time"] = pos_3cm_out.y;
        session.CurrentTrial.result["arc_aquired_angle"] = "";
        session.CurrentTrial.result["arc_aquired_time"] = "";
        session.CurrentTrial.result["localizing_angle"] = "";
        session.CurrentTrial.result["localizing_time"] = "";
    }
}


