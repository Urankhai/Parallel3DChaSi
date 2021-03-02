using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scatterers_Spawning : MonoBehaviour
{
    public int distanceMax;

    public float chiD;  // =  6*0.1023; %[scatterers/m^2]
    public float WD; // width of the spawning area for DMC
    public float chiW1; // = 0.044; %W1 is first order
    public float WW1; // width of the spawning area for MPC1


    public float chiW2; // = 1.5*0.044; %W2 is second order
    public float WWH; // width of the spawning area for higher order MPCs
    public float chiW3; // = 2*0.044; %W3 is third order
    public Material MPC1_mat;
    public Material MPC2_mat;
    public Material MPC3_mat;
    public Material DMC_mat;

    private Vector3 v_shift;

    // Start is called before the first frame update
    private GameObject[] Buildings;
    private Mesh mesh;
    void Start()
    {
        v_shift = new Vector3(0,0,0); //new Vector3(2.3f, 0.0f, -1.81f);
        //if (nearBuildings == null)
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        /*for (int k = 0; k < Buildings.Length; k++)
        {
            var BuildingRenderer = Buildings[k].GetComponent<Renderer>();
            BuildingRenderer.material = MPC2_mat;
        }*/

        //Debug.Log("0");
        //Debug.Log("Number of buildings = " + Buildings.Length);

        /*foreach (GameObject inBuilding in Buildings)
        {
            Debug.Log(inBuilding.name);
            //Debug.Log("1");
        }*/

        Debug.Log("Intersection of interest is located at [" + transform.position + "]");

        Collider[] nearBuildings = Physics.OverlapSphere(transform.position, distanceMax);
        Debug.Log("Number of buildings near the point of interest = " + nearBuildings.Length);

        foreach (Collider buidling in nearBuildings)
        {
            Vector3[] vertices = buidling.GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] normals = buidling.GetComponent<MeshFilter>().mesh.normals;

            //print name of the building
            //Debug.Log("Building name and number of vertices " + buidling.name + vertices.Length);

            // Drawing the normal for the first vertex
            /*
            GameObject first_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            first_sphere.transform.position = vertices[0] + v_shift;
            GameObject second_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            second_sphere.transform.position = vertices[1] + v_shift;
            GameObject third_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            third_sphere.transform.position = vertices[2] + v_shift;
            


            //Debug.Log("Normal = " + normals[0]);
            
            // Drawing normals to vertices
            Debug.DrawRay(vertices[0] + v_shift, normals[0]*10, Color.yellow,100.0f);
            

            //------------------------------------------------------
            //Color manipulation
            //Get the Renderer component from the new cube
            var sphereRenderer = first_sphere.GetComponent<Renderer>();
            //Call SetColor using the shader property name "_Color" and setting the color to red
            sphereRenderer.material.SetColor("_Color", Color.green);
            //------------------------------------------------------
            */

            // Drwaing spheres around each vertices
            int vert_count = 0;

            List<Vector3> floor_vrtx = new List<Vector3>();
            List<Vector3> floor_nrml = new List<Vector3>();

            foreach (Vector3 vert in vertices)
            {



                // Drawing normals to vertices
                if (vert.y == 0)
                {
                    //Debug.Log("Vertex #" + vert_count + vert + normals[vert_count]);
                    //Debug.DrawRay(vert + v_shift, normals[vert_count] * 10, Color.yellow, 10.0f);

                    /*
                    string sphere_name = "sphere" + vert_count;
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.position = vert + v_shift;
                    sphere.name = sphere_name;
                    */

                    floor_vrtx.Add(vert + v_shift);
                    floor_nrml.Add(normals[vert_count]);
                }
                vert_count += 1;
            }
            //Debug.Log("Number of floor elements = " + floor_nrml.Count);

            for (int k = 0; k < floor_nrml.Count - 1; k++)
            {
                //Debug.Log("OK1");
                if (floor_vrtx[k] != floor_vrtx[k + 1])
                {
                    /*
                    GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube1.transform.position = floor_vrtx[k] + floor_nrml[k] * WW1;

                    GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube2.transform.position = floor_vrtx[k + 1] + floor_nrml[k + 1] * WW1;
                    */

                    Vector3 edge_len = floor_vrtx[k] - floor_vrtx[k + 1];
                    float S = WW1 * edge_len.magnitude;
                    float S2 = WWH * edge_len.magnitude;
                    float S3 = WWH * edge_len.magnitude;
                    float SD = WD * edge_len.magnitude;
                    //Debug.Log("Area = " + S);
                    //Debug.Log("Length = " + edge_len.magnitude);
                    //Debug.Log("length of normal vector = " + floor_nrml[k].magnitude);
                    //Debug.Log("length of normal vector = " + floor_nrml[k+1].magnitude);

                    float threshold1 = S * chiW1;
                    float threshold2 = S2 * chiW2;
                    float threshold3 = S3 * chiW3;

                    float thresholdD = SD * chiD;

                    // number of MPCs within the area
                    int num_MPC1 = Mathf.FloorToInt(threshold1);
                    int num_MPC2 = Mathf.FloorToInt(threshold2);
                    int num_MPC3 = Mathf.FloorToInt(threshold3);
                    // number of DMCs within the area
                    int num_DMC = Mathf.FloorToInt(thresholdD);

                    //Debug.Log("Number of MPC = " + num_MPC1);

                    // since it is a probabilistic, we add additional MPC in case of luck
                    float prob_add_MPC1 = threshold1 - num_MPC1;
                    //Debug.Log("Probability of addition MPC = " + prob_add_MPC1);
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
                    //Debug.Log("Probability of addition MPC = " + prob_add_MPC1);
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
                    //Debug.Log("Probability of addition MPC = " + prob_add_MPC1);
                    int add_MPC3 = 0;
                    int rand_num3 = Random.Range(1, 1001);
                    if (rand_num3 < prob_add_MPC3 * 1000)
                    {
                        add_MPC3 = 1;
                    }
                    // the total number of MPCs1 = num_MPC1 + add_MPC1
                    num_MPC3 = num_MPC3 + add_MPC3;

                    // number of DMC
                    float prob_add_DMC = thresholdD - num_DMC;
                    //Debug.Log("Probability of addition MPC = " + prob_add_MPC1);
                    int add_DMC = 0;
                    int rand_numD = Random.Range(1, 1001);
                    if (rand_numD < prob_add_DMC * 1000)
                    {
                        add_DMC = 1;
                    }
                    // the total number of MPCs1 = num_MPC1 + add_MPC1
                    num_DMC = num_DMC + add_DMC;
                    


                    // spawning scatterers in the given area
                    if (k > -1)
                    {
                        //Debug.Log("k = " + k);
                        if (num_MPC1 != 0)
                        {
                            // calculate transformation parameters
                            Vector3 cetner_rec = (floor_vrtx[k] + floor_vrtx[k + 1] + floor_nrml[k + 1] * WW1) / 2; //(floor_vrtx[k] + floor_vrtx[k + 1] + cube1.transform.position + cube2.transform.position)/4;

                            /*// checking for the correctness
                            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            cube.transform.position = cetner_rec;
                            var cubeRenderer = cube.GetComponent<Renderer>();
                            cubeRenderer.material.SetColor("_Color", Color.red);
                            */
                            /*float[][] rot_mtrx =
                            {
                            new[] {edge_len.normalized.x, 0.0f, floor_nrml[k].x},
                            new[] {0.0f, 1.0f, 0.0f},
                            new[] {edge_len.normalized.z, 0.0f, floor_nrml[k].z}
                            };
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
                                //Debug.Log("e1 vector = " + edge_len.normalized);
                                //Debug.Log("e2 vector = " + floor_nrml[k + 1]);

                                MPC1_positionArray[i].x = MPC1_randomArray[i].x * edge_len.normalized.x + MPC1_randomArray[i].z * edge_len.normalized.z + cetner_rec.x;
                                MPC1_positionArray[i].y = MPC1_randomArray[i].y + cetner_rec.y;
                                MPC1_positionArray[i].z = -MPC1_randomArray[i].x * floor_nrml[k + 1].x - MPC1_randomArray[i].z * floor_nrml[k + 1].z + cetner_rec.z;

                                //GameObject capsule0 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                                //capsule0.transform.position = MPC1_positionArray[i];

                                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                capsule.transform.position = MPC1_positionArray[i];
                                //capsule.material = MPC1_mat;

                                var capsuleRenderer = capsule.GetComponent<Renderer>();
                                //Call SetColor using the shader property name "_Color" and setting the color to red
                                //capsuleRenderer.material.SetColor("_Color", Color.green);
                                //capsuleRenderer.lightProbeUsage = "off";
                                capsuleRenderer.material = MPC1_mat;
                                capsule.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                                capsule.name = "MPC1"+floor_nrml[k + 1];
                                capsule.tag = "MPC1";

                                // Spheres colliders are disabled for MPCs
                                //MPC1_Collider = capsule.GetComponent<Collider>();
                                //MPC1_Collider.enabled = false;
                                
                                /*Mesh mesh = capsule.GetComponent<MeshFilter>().mesh;
                                mesh.normals[0] = floor_nrml[k + 1];
                                Debug.Log("asd " + mesh.normals[0]);*/
                            }


                        }

                        // MPC2
                        if (num_MPC2 != 0)
                        {
                            // calculate transformation parameters
                            Vector3 cetner_rec = (floor_vrtx[k] + floor_vrtx[k + 1] + floor_nrml[k + 1] * WWH) / 2; //(floor_vrtx[k] + floor_vrtx[k + 1] + cube1.transform.position + cube2.transform.position)/4;
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
                                capsule2.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                                capsule2.name = "MPC2"+floor_nrml[k + 1];
                                capsule2.tag = "MPC2";

                                //MPC2_Collider = capsule2.GetComponent<Collider>();
                                //MPC2_Collider.enabled = false;
                            }
                        }

                        // MPC3
                        if (num_MPC3 != 0)
                        {
                            // calculate transformation parameters
                            Vector3 cetner_rec = (floor_vrtx[k] + floor_vrtx[k + 1] + floor_nrml[k + 1] * WWH) / 2; //(floor_vrtx[k] + floor_vrtx[k + 1] + cube1.transform.position + cube2.transform.position)/4;
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
                                capsule3.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                                capsule3.name = "MPC3"+floor_nrml[k + 1];
                                capsule3.tag = "MPC3";

                                //MPC3_Collider = capsule3.GetComponent<Collider>();
                                //MPC3_Collider.enabled = false;
                            }
                        }

                        
                        // DMC
                        if (num_DMC != 0)
                        {
                            // calculate transformation parameters
                            Vector3 cetner_recD = (floor_vrtx[k] + floor_vrtx[k + 1] + floor_nrml[k + 1] * WD) / 2; //(floor_vrtx[k] + floor_vrtx[k + 1] + cube1.transform.position + cube2.transform.position)/4;
                            // generate random positions for a given number of MPCs
                            Vector3[] DMC_randomArray = new Vector3[num_DMC];
                            Vector3[] DMC_positionArray = new Vector3[num_DMC];
                            //Collider DMC_Collider;
                            for (int i = 0; i < num_DMC; i++)
                            {
                                DMC_randomArray[i].x = Random.Range(-edge_len.magnitude / 2, edge_len.magnitude / 2); // takes into account the length of the wall
                                DMC_randomArray[i].y = Random.Range(1.2f, 2.2f); // takes into account the average height of cars and dispersion 50 cm
                                DMC_randomArray[i].z = Random.Range(-WD / 2, WD / 2); // takes into account the width of MPCs from the paper (3 meters)

                                DMC_positionArray[i].x = DMC_randomArray[i].x * edge_len.normalized.x + DMC_randomArray[i].z * edge_len.normalized.z + cetner_recD.x;
                                DMC_positionArray[i].y = DMC_randomArray[i].y + cetner_recD.y;
                                DMC_positionArray[i].z = -DMC_randomArray[i].x * floor_nrml[k + 1].x - DMC_randomArray[i].z * floor_nrml[k + 1].z + cetner_recD.z;

                                GameObject capsuleD = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                capsuleD.transform.position = DMC_positionArray[i];

                                var capsuleRendererD = capsuleD.GetComponent<Renderer>();
                                //capsuleRendererD.material.SetColor("_Color", Color.grey);
                                capsuleRendererD.material = DMC_mat;
                                capsuleD.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                                capsuleD.name = "DMC"+floor_nrml[k + 1];
                                capsuleD.tag = "DMC";
                                //DMC_Collider = capsuleD.GetComponent<Collider>();
                                //DMC_Collider.enabled = false;
                                

//                                var whereitfaces = capsuleD.GetComponent<Data_holding>();
//                                whereitfaces.Facing = floor_nrml[k + 1];
                            }
                        }

                    }
                    //Debug.Log("-------------------------------------------------------------");
                }
            }

        }

    }


}
