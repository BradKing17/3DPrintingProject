using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesignateMesh : MonoBehaviour
{
    GameObject selectedObj = null;

    Material vertexMat = null;
    Material outlineMat = null;
    Material objMat = null;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Select"))
        {
            Debug.Log("Pressed");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                if(hit.collider != null)
                {
                    selectedObj = hit.collider.gameObject;
                    Debug.Log("Clicked on: " + selectedObj);
                    SetMaterials();
                }
            }
        }
    }
    void SetMaterials()
    {
        objMat = selectedObj.GetComponent<Renderer>().material;
        selectedObj.GetComponent<Renderer>().material = vertexMat;
    }
}
