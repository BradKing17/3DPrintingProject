using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrintingManager : MonoBehaviour
{
    //References
    Canvas inspectorUI = null; 
    Camera mainCam = null;
    public GameObject slicingPlane;

    [Header("---Object Selection---")]
    public GameObject selectedObj = null;
    Renderer selectedRend = null;
    public Mesh selectedMesh = null;
    public Material outlineMat = null;
    public GameObject[] objects;

    [Header("---Overhang Detection---")]
    public Material vertexMat = null;
    Material baseMat = null;

    [Header("---Centre of Mass---")]
    public Image centreOfMassSprite = null;

    
    
    [Header("---Base Detection---")]
    public float baseTolerance = 0.2f;
    public int minNumOfBasePoints = 2;
    public float floorHeight = 0.0f;
    public bool isFloating = false;
    public List<Vector3> objVerts;
    [HideInInspector] public Vector3[] localVerts;
    [HideInInspector] public Vector3 lowestPointOnMesh;
    [HideInInspector] public List<Vector3> lowestPointsOnMesh;

    [Header("---Base Settings---")]
    public float baseSize = 2.0f;
    public float baseHeight = 0.1f;

    [Header("---Mesh Cutting---")]
    public float minMeshWidth = 0.05f;
    public float meshHeight = 0.0f;
    public Vector3 highestPointOnMesh;
    public Material slicingMat;
    [HideInInspector] public List<Vector3> slicedVerts;
    public List<(Vector3, Vector3)> intersectedEdges = new List<(Vector3, Vector3)>();
    public List<(Vector3, Vector3)> allEdges = new List<(Vector3, Vector3)>();
    public List<Vector3> intersections = default;
    public LineRenderer crossSectionRenderer = default;


    // Start is called before the first frame update
    void Start()
    {
        inspectorUI = GameObject.Find("3DUI").GetComponent<Canvas>();
        inspectorUI.enabled = false; 
        mainCam = Camera.main;
        centreOfMassSprite.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Testremove"))
        {
            UnassignSelectedObject();
        }
        if (Input.GetButtonDown("Select"))
        {
            Debug.Log("Pressed");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;


            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    if (selectedObj != hit.collider.gameObject)
                    {
                        if (selectedObj)
                        {
                            UnassignSelectedObject();
                        }
                        selectedObj = hit.collider.gameObject;
                        selectedRend = selectedObj.GetComponent<Renderer>();
                        Debug.Log("Clicked on: " + selectedObj);
                        inspectorUI.enabled = true;
                        selectedMesh = selectedObj.GetComponent<MeshFilter>().mesh;
                        localVerts = selectedMesh.vertices;
                        foreach (Vector3 vert in localVerts)
                        {
                           
                                objVerts.Add(selectedObj.transform.TransformPoint(vert));
                        
                        }
                        SetOutline();
                    }
                }
                else
                {
                    if (selectedObj)
                    {
                        UnassignSelectedObject();
                    }
                }
            }
        }
        //Update CoM Sprite pos
        if (selectedObj)
        { 
            Rigidbody rb;
            if (!selectedObj.GetComponent<Rigidbody>())
            { selectedObj.AddComponent<Rigidbody>(); }
            rb = selectedObj.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            centreOfMassSprite.rectTransform.position = mainCam.WorldToScreenPoint(rb.worldCenterOfMass);
        }
    }
    //Show outline on selection
    void SetOutline()
    {
        Material[] tempMats = selectedRend.materials;
        tempMats[1] = outlineMat;
        selectedRend.materials = tempMats;
        baseMat = selectedRend.material;
    }

    public void AssignVertexMaterial()
    {
        selectedRend.material = vertexMat;
    }

    public void ShowCentreOfMass()
    {
        centreOfMassSprite.enabled = true;
    }

    public void FindBaseOfMesh()
    {
        lowestPointOnMesh = objVerts[0];
        float lowestYPos = lowestPointOnMesh.y;

        foreach (Vector3 vert in objVerts)
        {
            if(vert.y < lowestYPos)
            {
                lowestPointOnMesh = vert;
                lowestYPos = vert.y;
            }
           
        }
        lowestPointsOnMesh.Add(lowestPointOnMesh);

        for(int i = 0; i < objVerts.Count; i++)
        {
            if(objVerts[i].y < lowestYPos + baseTolerance)
            {
                if (Vector3.Dot(selectedObj.transform.TransformDirection(localVerts[i]), Vector3.up) < -0.5f)
                {
                    if (!lowestPointsOnMesh.Contains(objVerts[i]))
                    {
                        lowestPointsOnMesh.Add(objVerts[i]);
                    }
                }
            }
        }
        
        if(lowestPointOnMesh.y > floorHeight)
        {
            isFloating = true;
        }
        if(lowestPointsOnMesh.Count < minNumOfBasePoints)
        {
            if (!isFloating)
            {
                GenerateBase();
            }
        }
    }

    void GenerateBase()
    {
        Debug.Log("Generating Base");

        GameObject objBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        objBase.transform.position = new Vector3(lowestPointOnMesh.x, lowestPointOnMesh.y - baseHeight, lowestPointOnMesh.z);
        objBase.transform.localScale = new Vector3(baseSize, baseHeight, baseSize);
    }

    public void SliceMesh()
    {
        intersections.Clear();
        float highestYPos = 0.0f;
        foreach (Vector3 vert in objVerts)
        {
            //Find highest point on mesh
            if (vert.y > highestYPos)
            {
                highestPointOnMesh = vert;
                highestYPos = vert.y;
            }

            //Find all verts above slicer plane
            var dir = vert - slicingPlane.transform.position;
            if(Vector3.Dot(slicingPlane.transform.up, dir) > 0)
            {
                slicingMat.SetFloat(0, Vector3.Dot(slicingPlane.transform.up, dir));
                slicedVerts.Add(vert);
            }
        }
        //Find edges that intersect with plane 
        for (int i = 0; i < selectedMesh.triangles.Length; i+=3)
        {
            Vector3 p1 = objVerts[selectedMesh.triangles[i]];
            Vector3 p2 = objVerts[selectedMesh.triangles[i + 1]];
            Vector3 p3 = objVerts[selectedMesh.triangles[i + 2]];


                if (slicedVerts.Contains(p1) ^ slicedVerts.Contains(p2))
                {
                    intersectedEdges.Add((p1, p2));
                }
                if (slicedVerts.Contains(p1) ^ slicedVerts.Contains(p3))
                {
                    intersectedEdges.Add((p1, p3));
                }
                if (slicedVerts.Contains(p2) ^ slicedVerts.Contains(p3))
                {
                    intersectedEdges.Add((p2, p3));
                }
        }
        //Find point of intersection for each edge
        foreach (var edge in intersectedEdges)
        {
            Ray ray = new Ray(edge.Item1, (edge.Item2 - edge.Item1));
            Plane plane = new Plane(slicingPlane.transform.up, 0);
            plane.SetNormalAndPosition(slicingPlane.transform.up, slicingPlane.transform.position);
            plane.Raycast(ray, out float distance);

            intersections.Add(ray.GetPoint(distance));

            crossSectionRenderer.enabled = true;
            crossSectionRenderer.positionCount = intersections.Count;
            crossSectionRenderer.SetPositions(intersections.ToArray());

        }
        meshHeight = highestPointOnMesh.y - lowestPointOnMesh.y;

    }

    void UnassignSelectedObject()
    {
        Debug.Log("Doing it");
        Material[] tempMats = { baseMat, null };
        selectedRend.materials = tempMats;
        selectedObj = null;
        selectedRend = null;
        objVerts.Clear();
        centreOfMassSprite.enabled = false;
        inspectorUI.enabled = false;
        crossSectionRenderer.enabled = false;
    }



    private void OnDrawGizmos()
    {
        foreach (Vector3 vert in objVerts)
        {
            if (slicedVerts.Contains(vert))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(vert, 0.01f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(vert, 0.01f);
            }
        }

        for(int i = 0; i <intersections.Count; i++)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(intersections[i], 0.01f);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(intersections[i], intersections[i + 1]);
        }
    }
}

