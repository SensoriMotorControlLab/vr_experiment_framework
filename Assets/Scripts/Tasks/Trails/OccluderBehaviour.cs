using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OccluderBehaviour : MonoBehaviour
{   
    Trails trail;
    // Start is called before the first frame update
    void Start()
    {
        trail = GameObject.Find("ExperimentController").GetComponent<Trails>();
    }

    // Update is called once per frame

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Car")
        {
            // Debug.Log("Occluder exit");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Car")
        {
            // Debug.Log("Occluder stay");
        }
    }
}
