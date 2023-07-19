﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A general target that could be expanded into other uses
public class BaseTarget : MonoBehaviour
{
    protected ExperimentController ctrler;

    float hold_still_time = 0.3f;

    [SerializeField]
    public bool Collided { get; private set; }
    public Collider CollidedWith { get; private set; }
    public bool isStillTime = false;

    /// <summary>
    /// When true, objects attached with this script will not increment the step upon collision
    /// </summary>
    public bool CollisionModeOnly = false;

    private void Start()
    {
        ctrler = GameObject.Find("ExperimentController").GetComponent<ExperimentController>();
    }

    private void AdvanceStep()
    {
        ctrler.CurrentTask.IncrementStep();
    }

    public void Update(){
        if(isStillTime && ctrler.CursorController.stillTime > 0.5f){
            AdvanceStep();
            isStillTime = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enabled) return;

        if (!CollisionModeOnly)
        {
            switch (collision.gameObject.tag)
            {
                default:
                    Debug.LogWarning("Tag not implemented, see BaseTarget.cs");
                    break;
            }
        }

        Collided = true;
        CollidedWith = collision.collider;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(ctrler);
        if (!enabled) return;

        if (!CollisionModeOnly)
        {
            switch (other.gameObject.tag)
            {
                case "Target":
                    if(ctrler.CursorController.useHand == true)
                    {
                        switch(ctrler.CurrentTask.GetCurrentStep)
                        {
                            case 1:
                                if(ctrler.CurrentTask.GetIsNoCursor){
                                    isStillTime = true;
                                }
                                else {
                                    AdvanceStep();
                                }
                                break;
                            default:
                                AdvanceStep();
                                break;
                        }
                    }
                    break;
                case "Car":
                    AdvanceStep();
                    break;
                default:
                    //Debug.LogWarning("Tag not implemented, see BaseTarget.cs");
                    break;
            }
        }

        Collided = true;
        CollidedWith = other;
    }

    private void OnCollisionExit(Collision collision)
    {
        Collided = false;
        CollidedWith = null;
    }

    private void OnTriggerExit(Collider other)
    {
        Collided = false;
        CollidedWith = null;
    }
}
