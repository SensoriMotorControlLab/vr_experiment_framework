using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualHandTask : BaseTask
{
    ExperimentController ctrler;
    float speed = 0.75f;

    GameObject virtualHandPrefab;
    GameObject lHand;
    GameObject rHand;
    public override void Setup(){
        maxSteps = 4;
        ctrler = ExperimentController.Instance();

        ctrler.CursorController.SetHandVisibility(false);
        Cursor.visible = false;
        ctrler.CursorController.SetCursorVisibility(true);

        virtualHandPrefab = Instantiate(ctrler.GetPrefab("VirtualHandPrefab"));
        lHand = GameObject.Find("ik_hand_l_target");
        rHand = GameObject.Find("ik_hand_r_target");
        //virtualHandPrefab.transform.localPosition = Vector3.zero;
    }

    public override void Update()
    {
        base.Update();
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(-z, x, 0);
        rHand.transform.Translate(movement * speed *Time.deltaTime);
    }

    public override void Disable(){}

    public override void LogParameters(){}


}
