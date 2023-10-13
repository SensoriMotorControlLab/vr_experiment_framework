﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour
{
    public bool WorldSpace = true;
    public bool TrackTrials = false;

    public GameObject CameraSpaceCanvas, WorldSpaceCanvas;
    public Text CamSpaceText, WorldSpaceText, TrialTrackText;

    public String ManualScoreText = "";
    public string scorePrefixText = "";

    /// <summary>
    /// When true, the score will not update based on the ExperimentController's score
    /// </summary>
    public bool AllowManualSet = false;

    /// <summary>
    /// When true, the score text will be prefaced with "Score: "
    /// </summary>
    public bool ScorePrefix = true;
    public bool overideScorePrefix = false;
    private Dictionary<string, string> scoreboardElem = new Dictionary<string, string>();

    public bool isCustomScore = false;

    // Start is called before the first frame update
    void Start()
    {
        if (WorldSpace)
        {
            CameraSpaceCanvas.SetActive(false);
            WorldSpaceCanvas.SetActive(true);

            TrialTrackText.gameObject.SetActive(TrackTrials);

            TrialTrackText.text = "Trials Remaining: " +
                                  (ExperimentController.Instance().Session.Trials.Count() -
                                  ExperimentController.Instance().Session.currentTrialNum + 1);

            //ManualScoreText = ExperimentController.Instance().Score.ToString();
        }
        else
        {
            CameraSpaceCanvas.SetActive(true);
            WorldSpaceCanvas.SetActive(false);

            //ManualScoreText = ExperimentController.Instance().Score.ToString();
        }
    }

    public void SetElements(Dictionary<string, string> scoreboardInfo)
    {
        scoreboardElem = scoreboardInfo;
    }

    public void SetElement(string key, string value)
    {
        scoreboardElem[key] = value;
    }

    // Update is called once per frame
    void Update()
    {
        Text target = WorldSpace ? WorldSpaceText : CamSpaceText;
        if(isCustomScore && overideScorePrefix){
            target.text = "";
            foreach (var key in scoreboardElem.Keys)
            {
                target.text += key + ": " + scoreboardElem[key] + "\n";
            }
            return;
        }
        if (AllowManualSet)
        {
            if (ScorePrefix) target.text = "Score: " + ManualScoreText;
            else target.text = ManualScoreText;
        }
        else
        {
            if (ScorePrefix) target.text = "Score: " + ExperimentController.Instance().Score;
            else if (overideScorePrefix) target.text = scorePrefixText + " " + ExperimentController.Instance().Score;
            else target.text = "" + ExperimentController.Instance().Score;
        }
    }
}