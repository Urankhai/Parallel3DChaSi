using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelCheckerVerticesNormals : MonoBehaviour
{
    //public GameObject[] _buildings;

    public List<GameObject> _buildings;
    // Start is called before the first frame update
    void Start()
    {
        GameObject buildingsParent = GameObject.Find("Buildings");
        //Debug.Log("Child Objects number is " + buildingsParent.transform.childCount);

        foreach (Transform child in buildingsParent.transform)
        {
            _buildings.Add(child.gameObject);
            GameObject tempObject = _buildings[0];
            Mesh tempMesh = tempObject.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = tempMesh.vertices;
            Debug.Log("Child name is " + child.gameObject.name);
        }
        //Debug.Log("Extracted number of children is " + _buildings.Count);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
