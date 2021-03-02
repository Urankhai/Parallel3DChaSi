using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class Scatterers_Spawning_near_Visible_Walls_upd2 : MonoBehaviour
{

    private GameObject[] Buildings;
    //private GameObject[] Roads;
    private GameObject[] Streets;

    public Material Corner_Material;
    public Material ObsPoint_Material;

    //public float chiD;  // =  6*0.1023; %[scatterers/m^2]
    //public float WD; // width of the spawning area for DMC
    public float chiW1; // = 0.044; %W1 is first order
    public float WW1; // 3m width of the spawning area for MPC1
    public float chiW2; // = 1.5*0.044; %W2 is second order
    public float WWH; // 3m width of the spawning area for higher order MPCs
    public float chiW3; // = 2*0.044; %W3 is third order
    public Material MPC1_mat;
    public Material MPC2_mat;
    public Material MPC3_mat;

    // Start is called before the first frame update
    void Start()
    {
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        //Roads = GameObject.FindGameObjectsWithTag("Roads");
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

                    /*GameObject obs_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    // remove the collider components
                    Destroy(obs_sphere.GetComponent<SphereCollider>());

                    obs_sphere.transform.position = obs_point;
                    var sphereRenderer = obs_sphere.GetComponent<Renderer>();
                    //sphereRenderer.material.SetColor("_Color", Color.red);
                    sphereRenderer.material = ObsPoint_Material;*/
                }
            }
        }


        var seen_vrtx_list = new List<Vector3>();
        var seen_nrml_list = new List<Vector3>();

        int success_count = 0;

        int total_num_scatterers = 0;


        foreach (GameObject building in Buildings)
        {
            Vector3[] vrtx = building.GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] nrml = building.GetComponent<MeshFilter>().mesh.normals;

            var floor_vrtx = new List<Vector3>();
            var floor_nrml = new List<Vector3>();
            //int floor_count = 0;

            int vrtx_count = 0;
            foreach (Vector3 v in vrtx)
            {
                if (v.y == 0)
                {
                    floor_vrtx.Add(v);
                    floor_nrml.Add(nrml[vrtx_count]);
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

                                /*GameObject v_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                v_sphere.transform.position = v;
                                Destroy(v_sphere.GetComponent<SphereCollider>()); // remove collider
                                var sphereRenderer = v_sphere.GetComponent<Renderer>();
                                sphereRenderer.material = Corner_Material;*/
                            }

                        }
                    }
                }
                vrtx_count += 1;
            }

            for (int k = 0; k < floor_vrtx.Count - 1; k++)
            {
                if (floor_vrtx[k] != floor_vrtx[k + 1])
                {
                    if (seen_vrtx_list.Contains(floor_vrtx[k]) && seen_nrml_list.Contains(floor_nrml[k]) && seen_vrtx_list.Contains(floor_vrtx[k + 1]) && seen_nrml_list.Contains(floor_nrml[k + 1]))
                    //if (floor_vrtx[k] != floor_vrtx[k + 1])
                    {
                        Vector3 edge_len = floor_vrtx[k] - floor_vrtx[k + 1];
                        float S = WW1 * edge_len.magnitude;
                        float S2 = WWH * edge_len.magnitude;
                        float S3 = WWH * edge_len.magnitude;
                        //float SD = WD * edge_len.magnitude;

                        float threshold1 = S * chiW1;
                        float threshold2 = S2 * chiW2;
                        float threshold3 = S3 * chiW3;

                        int num_MPC1 = Mathf.FloorToInt(threshold1);
                        int num_MPC2 = Mathf.FloorToInt(threshold2);
                        int num_MPC3 = Mathf.FloorToInt(threshold3);

            // since it is a probabilistic, we add additional MPC in case of luck
                        float prob_add_MPC1 = threshold1 - num_MPC1;
                        int add_MPC1 = 0;
                        int rand_num = Random.Range(1, 1001);
                        if (rand_num < prob_add_MPC1 * 1000)
                        {
                            add_MPC1 = 1;
                        }
                        // the total number of MPCs1 = num_MPC1 + add_MPC1
                        num_MPC1 = num_MPC1 + add_MPC1;
                        
                        // number of MPC2
                        float prob_add_MPC2 = threshold2 - num_MPC2;
                        int add_MPC2 = 0;
                        int rand_num2 = Random.Range(1, 1001);
                        if (rand_num2 < prob_add_MPC2 * 1000)
                        {
                            add_MPC2 = 1;
                        }
                        // the total number of MPCs1 = num_MPC1 + add_MPC1
                        num_MPC2 = num_MPC2 + add_MPC2;

                        // number of MPC3
                        float prob_add_MPC3 = threshold3 - num_MPC3;
                        int add_MPC3 = 0;
                        int rand_num3 = Random.Range(1, 1001);
                        if (rand_num3 < prob_add_MPC3 * 1000)
                        {
                            add_MPC3 = 1;
                        }
                        // the total number of MPCs1 = num_MPC1 + add_MPC1
                        num_MPC3 = num_MPC3 + add_MPC3;

                        total_num_scatterers = total_num_scatterers + num_MPC1 + num_MPC2 + num_MPC3;

             // spawning scatterers in the given area
                        if (num_MPC1 != 0)
                        {
                            // calculate transformation parameters
                            Vector3 cetner_rec = (floor_vrtx[k] + floor_vrtx[k + 1] + floor_nrml[k + 1] * WW1) / 2; 
                            //(floor_vrtx[k] + floor_vrtx[k + 1] + cube1.transform.position + cube2.transform.position)/4;
                            /*
                            // checking for the correctness
                            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            cube.transform.position = cetner_rec;
                            Destroy(cube.GetComponent<SphereCollider>()); // remove collider
                            var cubeRenderer = cube.GetComponent<Renderer>();
                            cubeRenderer.material = MPC2_mat;
                            */

                            // generate random positions for a given number of MPCs
                            Vector3[] MPC1_randomArray = new Vector3[num_MPC1];
                            Vector3[] MPC1_positionArray = new Vector3[num_MPC1];
                            //Collider MPC1_Collider; // MPCs are considered as transperent for rays
                            for (int i = 0; i < num_MPC1; i++)
                            {
                                MPC1_randomArray[i].x = Random.Range(-edge_len.magnitude / 2, edge_len.magnitude / 2); // takes into account the length of the wall
                                MPC1_randomArray[i].y = Random.Range(1.2f, 2.2f); // takes into account the average height of cars and dispersion 50 cm
                                MPC1_randomArray[i].z = Random.Range(-WW1 / 2, WW1 / 2); // takes into account the width of MPCs from the paper (3 meters)
                                
                                MPC1_positionArray[i].x = MPC1_randomArray[i].x * edge_len.normalized.x + MPC1_randomArray[i].z * edge_len.normalized.z + cetner_rec.x;
                                MPC1_positionArray[i].y = MPC1_randomArray[i].y + cetner_rec.y;
                                MPC1_positionArray[i].z = -MPC1_randomArray[i].x * floor_nrml[k + 1].x - MPC1_randomArray[i].z * floor_nrml[k + 1].z + cetner_rec.z;

                                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                capsule.transform.position = MPC1_positionArray[i];
                                Destroy(capsule.GetComponent<SphereCollider>()); // remove collider
                                //capsule.material = MPC1_mat;

                                var capsuleRenderer = capsule.GetComponent<Renderer>();
                                capsuleRenderer.material = MPC1_mat;
                                capsule.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                                capsule.name = "MPC1" + floor_nrml[k + 1];
                                capsule.tag = "MPC1";

                            }


                            // MPC2
                            if (num_MPC2 != 0)
                            {
                                // calculate transformation parameters
                                //Vector3 cetner_rec = (floor_vrtx[k] + floor_vrtx[k + 1] + floor_nrml[k + 1] * WWH) / 2; //(floor_vrtx[k] + floor_vrtx[k + 1] + cube1.transform.position + cube2.transform.position)/4;
                                                                                                                        // generate random positions for a given number of MPCs
                                Vector3[] MPC2_randomArray = new Vector3[num_MPC2];
                                Vector3[] MPC2_positionArray = new Vector3[num_MPC2];
                                //Collider MPC2_Collider;
                                for (int i = 0; i < num_MPC2; i++)
                                {
                                    MPC2_randomArray[i].x = Random.Range(-edge_len.magnitude / 2, edge_len.magnitude / 2); // takes into account the length of the wall
                                    MPC2_randomArray[i].y = Random.Range(1.2f, 2.2f); // takes into account the average height of cars and dispersion 50 cm
                                    MPC2_randomArray[i].z = Random.Range(-WWH / 2, WWH / 2); // takes into account the width of MPCs from the paper (3 meters)

                                    MPC2_positionArray[i].x = MPC2_randomArray[i].x * edge_len.normalized.x + MPC2_randomArray[i].z * edge_len.normalized.z + cetner_rec.x;
                                    MPC2_positionArray[i].y = MPC2_randomArray[i].y + cetner_rec.y;
                                    MPC2_positionArray[i].z = -MPC2_randomArray[i].x * floor_nrml[k + 1].x - MPC2_randomArray[i].z * floor_nrml[k + 1].z + cetner_rec.z;

                                    GameObject capsule2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                    capsule2.transform.position = MPC2_positionArray[i];

                                    var capsuleRenderer2 = capsule2.GetComponent<Renderer>();
                                    //capsuleRenderer2.material.SetColor("_Color", Color.red);
                                    capsuleRenderer2.material = MPC2_mat;
                                    capsule2.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                                    capsule2.name = "MPC2" + floor_nrml[k + 1];
                                    capsule2.tag = "MPC2";

                                    //MPC2_Collider = capsule2.GetComponent<Collider>();
                                    //MPC2_Collider.enabled = false;
                                }
                            }

                            // MPC3
                            if (num_MPC3 != 0)
                            {
                                // calculate transformation parameters
                                //Vector3 cetner_rec = (floor_vrtx[k] + floor_vrtx[k + 1] + floor_nrml[k + 1] * WWH) / 2; //(floor_vrtx[k] + floor_vrtx[k + 1] + cube1.transform.position + cube2.transform.position)/4;
                                                                                                                        // generate random positions for a given number of MPCs
                                Vector3[] MPC3_randomArray = new Vector3[num_MPC3];
                                Vector3[] MPC3_positionArray = new Vector3[num_MPC3];
                                //Collider MPC3_Collider;
                                for (int i = 0; i < num_MPC3; i++)
                                {
                                    MPC3_randomArray[i].x = Random.Range(-edge_len.magnitude / 2, edge_len.magnitude / 2); // takes into account the length of the wall
                                    MPC3_randomArray[i].y = Random.Range(1.2f, 2.2f); // takes into account the average height of cars and dispersion 50 cm
                                    MPC3_randomArray[i].z = Random.Range(-WWH / 2, WWH / 2); // takes into account the width of MPCs from the paper (3 meters)

                                    MPC3_positionArray[i].x = MPC3_randomArray[i].x * edge_len.normalized.x + MPC3_randomArray[i].z * edge_len.normalized.z + cetner_rec.x;
                                    MPC3_positionArray[i].y = MPC3_randomArray[i].y + cetner_rec.y;
                                    MPC3_positionArray[i].z = -MPC3_randomArray[i].x * floor_nrml[k + 1].x - MPC3_randomArray[i].z * floor_nrml[k + 1].z + cetner_rec.z;

                                    GameObject capsule3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                    capsule3.transform.position = MPC3_positionArray[i];

                                    var capsuleRenderer3 = capsule3.GetComponent<Renderer>();
                                    //capsuleRenderer3.material.SetColor("_Color", Color.yellow);
                                    capsuleRenderer3.material = MPC3_mat;
                                    capsule3.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                                    capsule3.name = "MPC3" + floor_nrml[k + 1];
                                    capsule3.tag = "MPC3";

                                    //MPC3_Collider = capsule3.GetComponent<Collider>();
                                    //MPC3_Collider.enabled = false;
                                }
                            }

                        }
                    }
                }
            }

        }
        Debug.Log("The length of the seen vertexes list = " + seen_vrtx_list.Count);
        Debug.Log("The total number of scatterers = " + total_num_scatterers);



    }
}
