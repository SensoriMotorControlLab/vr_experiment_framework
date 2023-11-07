using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GatePlacement : MonoBehaviour
{
    public List<Mesh> mesh = new List<Mesh>();

    private List<Vector3> v = new List<Vector3>();




    // Start is called before the first frame update
    public void Setup()
    {
        foreach (Mesh m in mesh)
        {
            // Vector2[] RotatedUVs = m.uv;//Store the existing UV's


            // for (var i = 0; i < RotatedUVs.Length; i++)
            // {//Go through the array
            //     var rot = Quaternion.Euler(0, 0, -90);
            //     RotatedUVs[i] = rot * RotatedUVs[i];
            // }

            // m.uv = RotatedUVs;//re-apply the adjusted uvs

            for (int i = 2; i < (m.vertices.Length); i += 1)
                v.Add(m.vertices[i]);
        }        
    }
    public void ObstructionPlacement(GameObject obst, float percent, GameObject track){
        // Get a vertex position in array from percent
        int placement = Mathf.RoundToInt((percent) * (v.Count - 3));
        obst.transform.position = track.transform.TransformPoint(v[placement]) + (Vector3.up * 0.5f);
        Vector3 diff = track.transform.TransformPoint(v[placement]) - track.transform.TransformPoint(v[placement + 1]);
        float rad = MathF.Atan2(diff.z, diff.x);
        float angle = Mathf.Rad2Deg * rad;
        obst.transform.Rotate(0, 180 - angle, 0, Space.Self);

    }

    public void SetGatePosition(GameObject gateParent, GameObject gate1, GameObject gate2, LineRenderer lr, BoxCollider col, float percent, GameObject track)
    {
        // Detach children
        List<Transform> gateChildren = new List<Transform>();
        gateChildren.AddRange(gateParent.GetComponentsInChildren<Transform>());
        gateParent.transform.DetachChildren();

        // Get a vertex position in array from percent
        int placement = Mathf.RoundToInt((percent) * (v.Count - 3));

        Vector3 p1 = track.transform.TransformPoint(v[placement]);
        Vector3 p2 = track.transform.TransformPoint(v[placement + 1]);
        Vector3 p3 = track.transform.TransformPoint(v[placement + 2]);

        /*
         * Vertex positions along splinemesh track:
         *
         *   2 
         *  \/\/\/\/\/\/
         *  1 3 
         *  
         */

        // Place first pole between p1/p3
        gate1.transform.position = p3 + Vector3.up * 0.5f;
        // Place second pole at p2
        gate2.transform.position = p2 + Vector3.up * 0.5f;

        // Place gate parent between gate poles
        gateParent.transform.position = (gate1.transform.position + gate2.transform.position) / 2;

        // Place collider between gate points
        col.transform.position = (gate1.transform.position + gate2.transform.position) / 2;
        // Stretch collider to meet both gate points
        col.size = new Vector3(Vector3.Distance(gate1.transform.position, gate2.transform.position), col.size.y, col.size.z);

        // Find direction perpendicular to the line between the two gate points
        Vector3 dir = gate1.transform.position - gate2.transform.position;
        Vector3 forward = Vector3.Cross(dir, Vector3.up).normalized;
        gate1.transform.forward = forward;
        gate2.transform.forward = forward;
        gateParent.transform.forward = forward;
        col.transform.forward = forward;

        // Reparent to gateParent
        foreach (Transform child in gateChildren)
        {
            child.SetParent(gateParent.transform);
        }
    }

    public void SetCheckeredFlags(LineRenderer lr, GameObject gate1, GameObject gate2)
    {
        // Stretch line renderer between gate poles
        lr.SetPosition(0, gate1.transform.position + Vector3.up * 0.45f);
        lr.SetPosition(1, gate2.transform.position + Vector3.up * 0.45f);
    }

    public void SetColliderPosition(BoxCollider col, float percent, GameObject track)
    {
         // Get a vertex position in array from percent
        int placement = Mathf.RoundToInt((percent) * (v.Count - 3));


        col.transform.position = track.transform.TransformPoint(v[placement]) + (Vector3.up * 0.5f);
        Vector3 diff = track.transform.TransformPoint(v[placement]) - track.transform.TransformPoint(v[placement - 1]);
        float rad = MathF.Atan2(diff.z, diff.x);
        float angle = Mathf.Rad2Deg * rad;
        col.transform.Rotate(0, 180 - angle, 0, Space.Self);


        // Vector3 p1 = track.transform.TransformPoint(v[placement]);
        // Vector3 p2 = track.transform.TransformPoint(v[placement + 1]);
        // Vector3 p3 = track.transform.TransformPoint(v[placement + 2]);

        // // Place first point between p1/p3
        // Vector3 point1 = (p1 + p3) / 2 + Vector3.up * 0.5f;
        // // Place second point at p2
        // Vector3 point2 = p2 + Vector3.up * 0.5f;

        // Place collider between gate points
        col.center = new Vector3(0.8f, 0, 0);
        // Stretch collider to meet both gate points
        col.size = new Vector3(1.75f, 1, 0.2f);

        // // Find direction perpendicular to the line between the two gate points
        // Vector3 dir = point1 - point2;
        // Vector3 forward = Vector3.Cross(dir, Vector3.up).normalized;
        // col.transform.forward = forward;
    }
}
