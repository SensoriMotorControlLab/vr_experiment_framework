using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurlingToolTask : ToolTask
{
    
    Vector3 pos = new Vector3();
    Vector3 look = new Vector3();
    //private Vector3 shotDir;
    private LTDescr d;
    private int id;
    bool toolGrabed = false;

    public override void Setup()
    {
        base.Setup();

        look = new Vector3(Home.transform.position.x, Home.transform.position.y, 0);



        Cursor.visible = false;

        baseObject.GetComponent<SphereCollider>().material.bounciness = 1f;

        curlingStone.SetActive(true);
        //Home.transform.position = new Vector3(Home.transform.position.x, Home.transform.position.y, -0.2f);
        //ballObjects.transform.position = new Vector3(ballObjects.transform.position.x, ballObjects.transform.position.y, -0.2f);
        //curlingStone.transform.position = new Vector3(curlingStone.transform.position.x, curlingStone.transform.position.y, -0.2f);


        baseObject.GetComponent<ToolObjectScript>().enabled = false;
        baseObject.SetActive(false);
        toolObjects.transform.position = new Vector3(toolObjects.transform.position.x, toolObjects.transform.position.y, -0.3f);
    }

    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            baseObject.SetActive(true);
            toolObjects.transform.rotation = toolSpace.transform.rotation;
        }

        switch (currentStep)
        {
            case 1:
                break;
        }
            
      
        return base.IncrementStep();
    }

    protected void Animate()
    {
        id = LeanTween.rotateY(toolObjects, 0, 0.3f).id;
    }

    void FixedUpdate(){
         switch (currentStep)
        {
            // the user triggers the object 
            case 1:

                ObjectFollowMouse(toolObjects, Vector3.zero);
                //// Ball follows mouse
                ObjectFollowMouse(baseObject, Vector3.zero);

                d = LeanTween.descr(id);
                if (d == null)
                {
                    toolObjects.transform.LookAt(look, surfaceParent.transform.up);
                }

                pos = toolObjects.transform.position;

                Vector3 startPos = new Vector3();
                Vector3 shotDir = new Vector3();

                float time = 0f;
                //non vr and vr control of the curling
                if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
                {
                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
                        //startPos = mousePoint;
                    }

                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.2f && curlingStone.transform.position.z > Home.transform.position.z)
                    {
                        
                        shotDir = startPos - mousePoint;
                        shotDir /= time;
                        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
                        {
                            shotDir = RotateShot(shotDir);
                        }
                        shotDir = new Vector3(shotDir.x, 0, shotDir.z);
                        baseObject.GetComponent<BilliardsBallBehaviour>().FireBilliardsBall(-shotDir, 0.5f);
                        pos = toolObjects.transform.position;
                        IncrementStep();
                    }
                }
                else
                {
                   if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.12f)
                   {
                       time += Time.fixedDeltaTime;
                       //startPos = ctrllerPoint;
                   }

                   // Vibrate controller (scaled to velocity)
                   if (toolObjects.GetComponent<Rigidbody>().velocity.magnitude > 0.02f)
                       VibrateController(0, Mathf.Lerp(0.1f, 0.3f, toolObjects.GetComponent<Rigidbody>().velocity.magnitude / 10f), Time.deltaTime, devices);
                   //VibrateController(0, 0.2f, Time.deltaTime, devices);

                   if ((curlingStone.transform.position.z - Home.transform.position.z) > 0.1f)
                   {
                       
                       shotDir = ctrler.CursorController.GetVelocity();
                       if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
                        {
                            shotDir = RotateShot(shotDir);
                        }
  
                        shotDir = new Vector3(shotDir.x, 0, shotDir.z);
                        baseObject.GetComponent<BilliardsBallBehaviour>().FireBilliardsBall(shotDir, 2f);
                        Debug.Log(shotDir);
                        pos = toolObjects.transform.position;
                        IncrementStep();
                   }
                }
                break;
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();


        switch (currentStep)
        {
            // initlize the scene 
            case 0:
                // ObjectFollowMouse(toolObjects, Vector3.zero);
                // ToolLookAtBall();

                baseObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

                // when close to the tool the controller vibrates
                if (Vector3.Distance(mousePoint, toolObjects.transform.position) <= 0.07f && !toolGrabed)
                {
                    // To Fix: the above positions are casted to the plane, y doesn't matter
                    VibrateController(0, 0.2f, Time.deltaTime, devices);
                }

                // grab object
                if (Vector3.Distance(mousePoint, toolObjects.transform.position) <= 0.07f && (Input.GetMouseButton(0) || ctrler.CursorController.IsTriggerDown()) && !toolGrabed)
                {
                    VibrateController(0, 0.34f, Time.deltaTime, devices);
                    toolOffset = mousePoint - toolObjects.transform.position;
                    toolGrabed = true;
                }

                if (toolGrabed){
                    ObjectFollowMouse(toolObjects, Vector3.zero);
                }

                if (Vector3.Distance(mousePoint, ballObjects.transform.position) <= 0.02f && toolGrabed)
                {
                    Animate();
                    IncrementStep();
                    ToolLookAtBall();
                }
                else if (Vector3.Distance(ctrllerPoint, ballObjects.transform.position) <= 0.02f && toolGrabed)
                {
                    Animate();
                    IncrementStep();
                    ToolLookAtBall();
                }

                break;
            case 2:
                toolObjects.transform.position = pos;
                break;
            case 3:
                toolObjects.transform.position = pos;
                break;
        }
    }
}
