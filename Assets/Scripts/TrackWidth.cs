using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackWidth : MonoBehaviour
{
    List<Mesh> meshes = new List<Mesh>();
    public float vertexDistance = 0.3f; // This is the width of the track you can control in the Inspector

    public LineRenderer lineRendererOne;
    public LineRenderer lineRendererTwo;

    List<Vector3> lineOne = new List<Vector3>();
    List<Vector3> lineTwo = new List<Vector3>();

    Dictionary<Vector3, Vector3> innerPoints = new Dictionary<Vector3, Vector3>();
    Dictionary<Vector3, Vector3> outerPoints = new Dictionary<Vector3, Vector3>();
    List<Vector3> pointsDir = new List<Vector3>();
    public float thinnerPercentage = 0.4f;

    public Material trackMaterial;


    // Start is called before the first frame update
    public void ThinTrack()
    {
        GameObject genMesh = transform.GetChild(0).gameObject;

        for (int i = 0; i < genMesh.transform.childCount; i++)
        {
            meshes.Add(genMesh.transform.GetChild(i).GetComponent<MeshFilter>().mesh);
        }
        
        for (int i = 0; i < meshes.Count; i++)
        {
            for (int j = 0; j < meshes[i].vertices.Length; j++)
            {
                Vector3 vertex = meshes[i].vertices[j];
                if(lineOne.Count == 0)
                {
                    innerPoints.Add(vertex, vertex);
                    lineOne.Add(vertex);
                    continue;
                }
                Vector3 lastVertex = lineOne[lineOne.Count - 1];
                if(Vector3.Distance(vertex, lastVertex) > vertexDistance && lineTwo.Count == 0)
                {
                    outerPoints.Add(vertex, vertex);
                    lineTwo.Add(vertex);
                    continue;
                }
                Vector3 lastVertexTwo = lineTwo[lineTwo.Count - 1];
                if (Vector3.Distance(vertex, lastVertex) > vertexDistance && Vector3.Distance(vertex, lastVertexTwo) < vertexDistance)
                {
                    if(!outerPoints.ContainsKey(vertex))
                    {
                        outerPoints.Add(vertex, vertex);
                        lineTwo.Add(vertex);
                    }

                }
                else if(Vector3.Distance(vertex, lastVertexTwo) > vertexDistance && Vector3.Distance(vertex, lastVertex) < vertexDistance)
                {
                    if(!innerPoints.ContainsKey(vertex))
                    {
                        innerPoints.Add(vertex, vertex);
                        lineOne.Add(vertex);
                    }
                }

            }
        }

        lineRendererOne.positionCount = lineOne.Count;
        lineRendererTwo.positionCount = lineTwo.Count;

        lineRendererOne.SetPositions(lineOne.ToArray());
        lineRendererTwo.SetPositions(lineTwo.ToArray());

        lineOne.Add(lineOne[0]);
        lineTwo.Add(lineTwo[0]);

        innerPoints.Add(Vector3.zero, lineOne[0]);
        outerPoints.Add(Vector3.zero, lineTwo[0]);

        MoveInnerPointsCloser(lineTwo, innerPoints.Values.ToList());
        MoveInnerPointsCloser(lineOne, outerPoints.Values.ToList());

        lineRendererTwo.SetPositions(lineTwo.ToArray());
        lineRendererOne.SetPositions(lineOne.ToArray());

        CreateMesh(lineOne, pointsDir, false, true);
        CreateMesh(lineTwo, pointsDir, true, false);

    }

    List<Vector3> InterpolateInnerPoints(List<Vector3> innerPoints, int targetCount)
    {
        List<Vector3> interpolatedPoints = new List<Vector3>();
        float step = (float)(innerPoints.Count - 1) / (targetCount - 1);

        for (int i = 0; i < targetCount; i++)
        {
            float index = i * step;
            int lowerIndex = Mathf.FloorToInt(index);
            int upperIndex = Mathf.CeilToInt(index);
            float t = index - lowerIndex;

            Vector3 interpolatedPoint = Vector3.Lerp(innerPoints[lowerIndex], innerPoints[upperIndex], t);
            interpolatedPoints.Add(interpolatedPoint);
        }

        return interpolatedPoints;
    }

    void MoveInnerPointsCloser(List<Vector3> innerPoints, List<Vector3> outerPoints)
    {
        float dist = Vector3.Distance(innerPoints[0], outerPoints[0])/2;

        for (int i = 0; i < innerPoints.Count; i++)
        {
            Vector3 direction = (outerPoints[i] - innerPoints[i]).normalized;
            pointsDir.Add(direction);
            innerPoints[i] += direction * (dist * thinnerPercentage); 
        }
    }

    void CreateMesh(List<Vector3> line, List<Vector3> dir, bool outer, bool invert)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < line.Count; i++)
        {
            vertices.Add(line[i]);
            if(outer)
            {
                vertices.Add(line[i] - dir[i] * 1f);
            }
            else
            {
                vertices.Add(line[i] + dir[i] * 1f);
            }

            float uvX = (float)i / (line.Count - 1);
            uvs.Add(new Vector2(uvX, 0));
            uvs.Add(new Vector2(uvX, 1));

            if(i < line.Count - 1)
            {
                triangles.Add(i * 2);
                triangles.Add(i * 2 + 1);
                triangles.Add(i * 2 + 2);

                triangles.Add(i * 2 + 1);
                triangles.Add(i * 2 + 3);
                triangles.Add(i * 2 + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        GameObject newMesh = new GameObject();
        newMesh.transform.parent = transform;
        newMesh.transform.localPosition = new Vector3(0, 0.01f, 0);
        newMesh.AddComponent<MeshFilter>().mesh = mesh;
        newMesh.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = newMesh.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        newMesh.GetComponent<MeshRenderer>().material = trackMaterial;
        newMesh.tag = "Grass";

        if(invert)
        {
            newMesh.transform.localScale = new Vector3(1, -1, 1);
        }
    }
}
