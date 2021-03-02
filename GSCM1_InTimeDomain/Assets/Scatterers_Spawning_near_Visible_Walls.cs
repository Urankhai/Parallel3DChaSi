using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class Scatterers_Spawning_near_Visible_Walls : MonoBehaviour
{

    private GameObject[] Buildings;
    private GameObject[] Roads;
    private GameObject[] Streets;

    public Material Corner_Material;
    public Material ObsPoint_Material;

    // Start is called before the first frame update
    void Start()
    {
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        Roads = GameObject.FindGameObjectsWithTag("Roads");
        Streets = GameObject.FindGameObjectsWithTag("CG");

        //Debug.Log("Number of buildings = " + Buildings.Length);
        //Debug.Log("Number of roads = " + Roads.Length);
        //Debug.Log("Number of streets cubes = " + Streets.Length);

        //Sorting a list of gameobjects by name. It is helpful when the streets ends are not defined sequentially.
        var sortedStreets = Streets.OrderBy(go => go.name).ToList();


        int street_number = 0;
        var all_obs_points = new List<Vector3>();
        Vector3 elev_cg = new Vector3(0, 1, 0);
        //SphereCollider obs_point_col;

        for (int k = 0; k < Streets.Length; k++)
        {
            //Debug.Log("Loop number = " + Streets[k].name);
            //Debug.Log("Street end name = " + sortedStreets[k].name);
            if (k % 2 == 1)
            {
                street_number += 1;
                Vector3 street_start2end = sortedStreets[k].transform.position - sortedStreets[k - 1].transform.position;
                int strlen = Mathf.CeilToInt(street_start2end.magnitude);
                //Debug.Log("Street number = " + street_number + " with length " + strlen);
                //Debug.DrawLine(sortedStreets[k].transform.position, sortedStreets[k-1].transform.position, Color.cyan, 5.0f);
                int step_length = 10;
                for (int l = 0; l < Mathf.CeilToInt(strlen / step_length); l++)
                {
                    Vector3 obs_point = sortedStreets[k - 1].transform.position + l * step_length * street_start2end.normalized + elev_cg;
                    all_obs_points.Add(obs_point);

                    GameObject obs_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    // remove the collider components
                    Destroy(obs_sphere.GetComponent<SphereCollider>());

                    obs_sphere.transform.position = obs_point;
                    var sphereRenderer = obs_sphere.GetComponent<Renderer>();
                    //sphereRenderer.material.SetColor("_Color", Color.red);
                    sphereRenderer.material = ObsPoint_Material;
                }
            }
        }


        var seen_vrtx_list = new List<Vector3>();
        var seen_nrml_list = new List<Vector3>();

        int success_count = 0;


        foreach (GameObject building in Buildings)
        {
            Vector3[] vrtx = building.GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] nrml = building.GetComponent<MeshFilter>().mesh.normals;

            int vrtx_count = 0;
            foreach (Vector3 v in vrtx)
            {
                if (v.y == 0)
                {
                    // checking if the vertex is visible from the road side
                    for (int j = 0; j < all_obs_points.Count; j++)
                    {
                        // Cast a ray from observation point to a building's vertex
                        RaycastHit hit;
                        // if it collides with something, then the vertex is not visible, however its unseen neighbour must be added to spawn scatterers
                        if (Physics.Linecast(all_obs_points[j], v + 0.1f * nrml[vrtx_count], out hit))
                        {

                        }
                        else
                        {
                            // add to the list of seen elements

                            // check if the vertex together with its normal are already in the list
                            if (seen_vrtx_list.Contains(v) && seen_nrml_list.Contains(nrml[vrtx_count]))
                            {
                                // do nothing
                            }
                            else
                            {
                                seen_vrtx_list.Add(v);
                                seen_nrml_list.Add(nrml[vrtx_count]);
                                success_count += 1;

                                GameObject v_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                v_sphere.transform.position = v;
                                Destroy(v_sphere.GetComponent<SphereCollider>()); // remove collider
                                var sphereRenderer = v_sphere.GetComponent<Renderer>();
                                sphereRenderer.material = Corner_Material;
                            }

                        }
                    }
                }
                vrtx_count += 1;
            }

        }
        Debug.Log("The length of the seen vertexes list = " + seen_vrtx_list.Count);
        



    }
    
}
