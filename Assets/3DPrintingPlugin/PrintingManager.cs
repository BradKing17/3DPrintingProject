using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrintingManager : MonoBehaviour
{
    //Main Variables
    Canvas inspectorUI = null; 
    Camera mainCam = null;

    //Object Selection
    public GameObject selectedObj = null;
    Renderer selectedRend = null;
    public Material outlineMat = null;
    public List<Vector3> objVerts;
    public Vector3 lowestPointOnMesh;


    //Vertex Shader
    public Material vertexMat = null;
    Material baseMat = null;

    //Physics and Centre of Mass
    public Image centreOfMassSprite = null; 

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
        if(Input.GetButtonDown("Testremove"))
        {
            UnassignSelectedObject();
        }
        if(Input.GetButtonDown("Select"))
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
                        Vector3[] localVerts = selectedObj.GetComponent<MeshFilter>().mesh.vertices;
                        foreach(Vector3 vert in localVerts)
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
        Rigidbody rb;
        if (!selectedObj.GetComponent<Rigidbody>())
        { selectedObj.AddComponent<Rigidbody>(); }
        rb = selectedObj.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        centreOfMassSprite.rectTransform.position = mainCam.WorldToScreenPoint(rb.worldCenterOfMass);
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
        Debug.Log(lowestPointOnMesh);
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
    }



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(lowestPointOnMesh, 0.1f);
    }
}

