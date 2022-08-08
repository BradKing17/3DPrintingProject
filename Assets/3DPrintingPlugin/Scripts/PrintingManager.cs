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
    public LayerMask selectableLayers;
    [HideInInspector] public GameObject selectedObj = null;
    Renderer selectedRend = null;
    [HideInInspector] public Mesh selectedMesh = null;
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
    [HideInInspector] public List<Vector3> objVerts;
    [HideInInspector] public Vector3[] localVerts;
    [HideInInspector] public Vector3 lowestPointOnMesh;
    [HideInInspector] public List<Vector3> lowestPointsOnMesh;

    [Header("---Base Settings---")]
    public float baseSize = 2.0f;
    public float baseHeight = 0.1f;

    [Header("---Mesh Cutting---")]
    public float minMeshWidth = 0.05f;
    public Material slicingMat;
    public LineRenderer crossSectionRenderer = default;
    public bool isSlicing = false;
    public float sliceStep = 0.05f;
    public float sliceTimer = 1.0f;
    [HideInInspector] public float curSliceTime = 1.0f;
    [HideInInspector] public float highestYPos = 0.0f;
    [HideInInspector] public float meshHeight = 0.0f;
    [HideInInspector] public Vector3 highestPointOnMesh;
    [HideInInspector] public List<Vector3> slicedVerts;
    [HideInInspector] public List<(Vector3, Vector3)> intersectedEdges = new List<(Vector3, Vector3)>();
    [HideInInspector] public List<(Vector3, Vector3)> allEdges = new List<(Vector3, Vector3)>();
    [HideInInspector] public List<Vector3> intersections = default;



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
                    if (!hit.collider.CompareTag("SlicingPlane"))
                    {
                        if (selectableLayers == (selectableLayers | (1 << hit.collider.gameObject.layer)))
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

        //Slicing steps

        if (isSlicing)
        {
            if (slicingPlane.transform.position.y < highestYPos)
            {
                curSliceTime -= Time.deltaTime;

                if (curSliceTime < 0)
                {
                    slicingPlane.transform.Translate(0, sliceStep, 0);
                    curSliceTime = sliceTimer;
                    SliceMesh();
                }
            }
            else
            {
                isSlicing = false;
            }
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
    //Overhang Shader
    public void AssignVertexMaterial()
    {
        selectedRend.material = vertexMat;
    }
    //Centre of Mass
    public void ShowCentreOfMass()
    {
        centreOfMassSprite.enabled = true;
    }
    //Find bottom of mesh
    public void FindBaseOfMesh()
    { 
        float lowestYPos = FindLowestPoint();
        
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

    float FindLowestPoint()
    {
        lowestPointOnMesh = objVerts[0];
        float lowestYPos = lowestPointOnMesh.y;
        foreach (Vector3 vert in objVerts)
        {

            if (vert.y < lowestYPos)
            {
                lowestPointOnMesh = vert;
                lowestYPos = vert.y;
            }
        }

        return lowestYPos;
    }

    void GenerateBase()
    {
        Debug.Log("Generating Base");

        GameObject objBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        objBase.transform.position = new Vector3(lowestPointOnMesh.x, lowestPointOnMesh.y - baseHeight, lowestPointOnMesh.z);
        objBase.transform.localScale = new Vector3(baseSize, baseHeight, baseSize);
    }

    public void StartSlice()
    {
        slicingPlane.transform.SetPositionAndRotation(new Vector3(selectedObj.transform.position.x, FindLowestPoint(), selectedObj.transform.position.z), Quaternion.identity);
        //Find highest point on mesh
        foreach (Vector3 vert in objVerts)
        {
            if (vert.y > highestYPos)
            {
                highestPointOnMesh = vert;
                highestYPos = vert.y;
            }
        }
        isSlicing = true;
    }

    public void SliceMesh()
    {
        intersections.Clear();
        foreach (Vector3 vert in objVerts)
        {
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
            
        }


        //Convert points to Vector2
        List<Vector2> intersections2D = new List<Vector2>();
        foreach(Vector3 point in intersections)
        {
            intersections2D.Add(new Vector2(point.x, point.z));
        }

        


        float xMaxExtent = selectedObj.transform.position.x;
        float xMinExtent = selectedObj.transform.position.x;
        float zMaxExtent = selectedObj.transform.position.z;
        float zMinExtent = selectedObj.transform.position.z;

        foreach (Vector3 point in intersections)
        {
            if (point.x > xMaxExtent)
            {
                xMaxExtent = point.x;
            }
            else if(point.x < xMinExtent)
            {
                xMinExtent = point.x;
            }
            if (point.z > zMaxExtent)
            {
                zMaxExtent = point.z;
            }
            else if (point.z < zMinExtent)
            {
                zMinExtent = point.z;
            }

            if(point.x  > selectedObj.transform.position.x)
            {

            }
        }

        crossSectionRenderer.enabled = true;
        
        crossSectionRenderer.positionCount = intersections.Count;
        crossSectionRenderer.SetPositions(intersections.ToArray());


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
        intersectedEdges.Clear();
        intersections.Clear();
        isSlicing = false;
        centreOfMassSprite.enabled = false;
        inspectorUI.enabled = false;
        crossSectionRenderer.enabled = false;
        
    }



    private void OnDrawGizmos()
    {

    }
}

