using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using System;
//using System.IO;
using System.Text;

public class Visible_Triangles_Extraction : MonoBehaviour
{
    // Start is called before the first frame update
    private GameObject[] Buildings;
    private GameObject[] Roads;

    
    void Start()
    {
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        Roads = GameObject.FindGameObjectsWithTag("Roads");


        Debug.Log("Number of buildings = " + Buildings.Length);
        Debug.Log("Number of roads = " + Roads.Length);
        
        
        Vector3 road_shift = Roads[0].transform.position;
        //GameObject road_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //Vector3 road_position = Roads[0].transform;
        //road_sphere.transform.position = Roads[0].transform.position;
        //Debug.Log(Roads[0].name);

        
        int road_count = 0;
        
        foreach(GameObject road in Roads)
        {
            road_count += 1;
            //Debug.Log(road.name);
            Vector3[] road_vertices = road.GetComponent<MeshFilter>().mesh.vertices;
            
            //if (road_count == 1)
            //{
                
                Debug.Log("Number of verteces in the road mesh = " + road_vertices.Length);

                Vector3 road_cg = new Vector3(0,0,0);
                for (int k=0; k < road_vertices.Length; k++)
                {
                    road_cg = road_cg + road_vertices[k];
                }
                road_cg = road_cg/road_vertices.Length;

                Debug.Log("Position of the road elm "+road_cg);
                GameObject road_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                road_sphere.transform.position = road_cg;
            //}
        }
        
        //Debug.Log("Number of roads = "+ road_count);
        

        int building_count = 0;
        foreach(GameObject building in Buildings)
        {
            building_count += 1;
            Vector3[] vertices = building.GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] normals = building.GetComponent<MeshFilter>().mesh.normals;

            int[] triangles = building.GetComponent<MeshFilter>().mesh.triangles;
            if (building_count == 1)
            {
                Debug.Log("Number of triangles in the first building = " + triangles.Length/3);
                
                Debug.Log(vertices[triangles[0]] + " with normal " + normals[triangles[0]]);
                GameObject first_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                first_sphere.transform.position = vertices[triangles[0]];
                var sphereRenderer1 = first_sphere.GetComponent<Renderer>();
                sphereRenderer1.material.SetColor("_Color", Color.red);

                Debug.Log(vertices[triangles[1]] + " with normal " + normals[triangles[1]]);
                GameObject second_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                second_sphere.transform.position = vertices[triangles[1]];
                var sphereRenderer2 = second_sphere.GetComponent<Renderer>();
                sphereRenderer2.material.SetColor("_Color", Color.yellow);

                Debug.Log(vertices[triangles[2]] + " with normal " + normals[triangles[2]]);
                GameObject third_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                third_sphere.transform.position = vertices[triangles[2]];
                var sphereRenderer3 = third_sphere.GetComponent<Renderer>();
                sphereRenderer3.material.SetColor("_Color", Color.green);
                
                /*
                for(int k=0; k < triangles.Length; k = k + 3)
                {
                    Debug.Log("Triangle #"+k/3+" consists of ("+triangles[k]+", "+triangles[k+1]+", "+triangles[k+2]+")");
                }
                */

            }
        }

        /*StreamWriter sw = new StreamWriter("Roupeiro_camisa.txt");
        sw.Write("Item escolhido: ");
        sw.WriteLine(item.name);
        sw.Write("Tempo: ");
        sw.WriteLine(Time.time);
        sw.Close();*/

    }

}
