using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintingManager : MonoBehaviour
{
    public GameObject selectedObj = null;
    Renderer selectedRend = null;

    public Material vertexMat = null;
    public Material outlineMat = null;
    Material baseMat = null;


    // Start is called before the first frame update
    void Start()
    {
        
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
    }
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

    void UnassignSelectedObject()
    {
        Debug.Log("Doing it");
        Material[] tempMats = { baseMat, null };
        selectedRend.materials = tempMats;
        selectedObj = null;
        selectedRend = null;
    }
}

