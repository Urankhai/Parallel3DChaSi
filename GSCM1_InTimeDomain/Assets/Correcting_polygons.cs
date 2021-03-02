using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
//using System;


public class Correcting_polygons : MonoBehaviour
{
    public float WW1; // 3m width of the spawning area for MPC1
    public float WW2; // 12m width of the spawning area for DMC
    // setting materials
    public bool MPC_visualizer_ECS;

    // converting GameObjects to Entities
    public GameObject MPC1_Prefab;
    public GameObject MPC2_Prefab;
    public GameObject MPC3_Prefab;
    public GameObject DMC_Prefab;

    
    private GameObject[] Buildings;
    private GameObject[] Observation_Points;

    
    public List<V6> SeenV6_MPC1 = new List<V6>();
    public List<V7> SeenV7_MPC1 = new List<V7>();
    int MPC1_num = 0;
    public List<V6> SeenV6_MPC2 = new List<V6>();
    public List<V7> SeenV7_MPC2 = new List<V7>();
    int MPC2_num = 0;
    public List<V6> SeenV6_MPC3 = new List<V6>();
    public List<V7> SeenV7_MPC3 = new List<V7>();
    int MPC3_num = 0;

    public List<V6> SeenV6_DMC = new List<V6>();
    public List<V7> SeenV7_DMC = new List<V7>();
    int DMC_num = 0;

    

    // Start is called before the first frame update
    void Start()
    {
        float t_original = Time.realtimeSinceStartup;
        GameObject MPC_Spawner = GameObject.Find("MPC_spawner");
        Scatterers_Spawning2 MPC_Script = MPC_Spawner.GetComponent<Scatterers_Spawning2>();
        List<Vector3> MPC1 = MPC_Script.MPC1_possiblepositionList;
        List<Vector3> MPC2 = MPC_Script.MPC2_possiblepositionList;
        List<Vector3> MPC3 = MPC_Script.MPC3_possiblepositionList;
        List<Vector3> DMC =  MPC_Script.DMC_possiblepositionList;

        
        // Declaring buildings and observation points
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        // The number of observation points should not be too big, a few is enough
        Observation_Points = GameObject.FindGameObjectsWithTag("Observation_Points");
        // Define the main road direction
        Vector3 main_axis = (Observation_Points[0].transform.position - Observation_Points[1].transform.position).normalized;
        Vector3 perp_axis = new Vector3(main_axis.z, 0, -main_axis.x);

        // subset of the seen Buldings
        List<GameObject> Building_list = new List<GameObject>();

        // Going through all observation points
        for (int k = 0; k < Observation_Points.Length; k++)
        {
            // analyzing which buidings are seen
            for (int b = 0; b < Buildings.Length; b++)
            {
                Vector3[] vrtx = Buildings[b].GetComponent<MeshFilter>().mesh.vertices;
                Vector3[] nrml = Buildings[b].GetComponent<MeshFilter>().mesh.normals;

                int seen_vrtx_count = 0;

                for (int v = 0; v < vrtx.Length; v++)
                {
                    if (vrtx[v].y == 0)
                    {
                        if (!Physics.Linecast(Observation_Points[k].transform.position, vrtx[v] + 1.1f * nrml[v]))
                        {
                            seen_vrtx_count += 1;
                        }
                    }
                }
                if (seen_vrtx_count != 0)
                {
                    if (!Building_list.Contains(Buildings[b]))
                    {
                        Building_list.Add(Buildings[b]);
                    }
                }
            }
        }

        // deactivating nonseen buildings
        for (int b = 0; b < Buildings.Length; b++)
        {
            if (!Building_list.Contains(Buildings[b]))
            {
                Buildings[b].SetActive(false);
            }

        }
        Debug.Log("The number of seen Buildings = " + Building_list.Count);

        List<Vector3> GlobalMPC1 = new List<Vector3>();

        List<Vector3> GlobalMPC2 = new List<Vector3>();

        List<Vector3> GlobalMPC3 = new List<Vector3>();

        List<Vector3> GlobalDMC = new List<Vector3>();

        // analyzing each seen building separately
        for (int k = 0; k < Building_list.Count; k++)
        {
            // writing MPCs into a txt file
            //string writePath;
            List<string> Obj_List = new List<string>();

            Vector3[] vrtx = Building_list[k].GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] nrml = Building_list[k].GetComponent<MeshFilter>().mesh.normals;

            List<Vector3> floor_vrtx = new List<Vector3>();
            List<Vector3> floor_nrml = new List<Vector3>();
            var pairs = new List<V6>();

            List<Vector3> possible_vrtx = new List<Vector3>();
            List<Vector3> possible_nrml = new List<Vector3>();



            for (int l = 0; l < vrtx.Length; l++)
            {
                if (vrtx[l].y == 0 && Mathf.Abs(nrml[l].y) < 0.9)
                {
                    floor_vrtx.Add(vrtx[l]);
                    floor_nrml.Add(nrml[l]);

                    // adding coordinates and normals of the vertices into a single object V6 (like a N x 6 matrix)
                    V6 valid_pair = new V6(vrtx[l], nrml[l]);
                    pairs.Add(valid_pair);

                }
            }
            // finding the edges of each building
            List<Vector3> SortedByX = floor_vrtx.OrderBy(vertex => vertex.x).ToList();
            List<Vector3> SortedByZ = floor_vrtx.OrderBy(vertex => vertex.z).ToList();
            float x_l = SortedByX[0].x - WW2;                     // x left
            float x_r = SortedByX[SortedByX.Count - 1].x + WW2;   // x right
            float z_d = SortedByZ[0].z - WW2;                     // z down
            float z_u = SortedByZ[SortedByX.Count - 1].z + WW2;   // z up

            // searching which scatterers are located near the building
            NearbyElements(x_l, x_r, z_d, z_u, MPC1, out List<Vector3> NearbyMPC1);
            NearbyElements(x_l, x_r, z_d, z_u, MPC2, out List<Vector3> NearbyMPC2);
            NearbyElements(x_l, x_r, z_d, z_u, MPC3, out List<Vector3> NearbyMPC3);
            NearbyElements(x_l, x_r, z_d, z_u, DMC, out List<Vector3> NearbyDMC);

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Defining areas around building where scatterers can be located
            //////////////////////////////////////////////////////////////////////////////////////////////////////////



            // defining necessary elements for the polygon that connects the last and the first vertices
            Vector3 n1_0 = floor_nrml[floor_vrtx.Count - 1];
            Vector3 p1_0 = floor_vrtx[floor_vrtx.Count - 1];
            Vector3 s1_0 = floor_vrtx[floor_vrtx.Count - 1] + WW1 * floor_nrml[floor_vrtx.Count - 1];
            Vector3 dmc_s1_0 = floor_vrtx[floor_vrtx.Count - 1] + WW2 * floor_nrml[floor_vrtx.Count - 1];

            Vector3 n2_0 = floor_nrml[0];
            Vector3 p2_0 = floor_vrtx[0]; 
            Vector3 s2_0 = floor_vrtx[0] + WW1 * floor_nrml[0];
            Vector3 dmc_s2_0 = floor_vrtx[0] + WW2 * floor_nrml[0];


            Area34 area = new Area34(p1_0, s1_0, p2_0, s2_0);
            //DrawArea(area);

            PickMPC(n1_0, p1_0, s1_0, n2_0, p2_0, s2_0, NearbyMPC1, out List<V6> MPC1_V6);
            PickMPC(n1_0, p1_0, s1_0, n2_0, p2_0, s2_0, NearbyMPC2, out List<V6> MPC2_V6);
            PickMPC(n1_0, p1_0, s1_0, n2_0, p2_0, s2_0, NearbyMPC3, out List<V6> MPC3_V6);
            PickMPC(n1_0, p1_0, dmc_s1_0, n2_0, p2_0, dmc_s2_0, NearbyMPC3, out List<V6> DMC_V6);


            //int MPC1_num = 0;
            for (int ii = 0; ii < MPC1_V6.Count; ii++)
            {
                if (!GlobalMPC1.Contains(MPC1_V6[ii].Coordinates))
                {

                    SeenV6_MPC1.Add(MPC1_V6[ii]);
                    V7 tempV7_MPC1 = new V7(MPC1_V6[ii], MPC1_num);
                    SeenV7_MPC1.Add(tempV7_MPC1);
                    GlobalMPC1.Add(MPC1_V6[ii].Coordinates);

                    //Entity newMPC1Entity = entityManager.Instantiate(MPC1_entity);
                    //entityManager.SetComponentData(newMPC1Entity, new Position);

                    //EntityManager.SetComponentData(newMPC1Entity, new Translation { Value = MPC1_V6[ii].Coordinates }) ;
                    //MPC1_Prefab.transform.position = MPC1_V6[ii].Coordinates;

                    if (MPC_visualizer_ECS == true)
                    {
                        GameObject MPC1_clone = Instantiate(MPC1_Prefab, MPC1_V6[ii].Coordinates, Quaternion.identity);
                        MPC1_clone.name = "mpc1 #" + MPC1_num;
                    }

                    MPC1_num += 1;
                }
            }

            //int MPC2_num = 0;
            for (int ii = 0; ii < MPC2_V6.Count; ii++)
            {
                if (!GlobalMPC2.Contains(MPC2_V6[ii].Coordinates))
                {
                    
                    SeenV6_MPC2.Add(MPC2_V6[ii]);
                    V7 tempV7_MPC2 = new V7(MPC2_V6[ii], MPC2_num);
                    SeenV7_MPC2.Add(tempV7_MPC2);
                    GlobalMPC2.Add(MPC2_V6[ii].Coordinates);

                    if (MPC_visualizer_ECS == true)
                    {
                        GameObject MPC2_clone = Instantiate(MPC2_Prefab, MPC2_V6[ii].Coordinates, Quaternion.identity);
                        MPC2_clone.name = "mpc2 #" + MPC2_num;
                    }

                    MPC2_num += 1;
                }
            }

            //int MPC3_num = 0;
            for (int ii = 0; ii < MPC3_V6.Count; ii++)
            {
                if (!GlobalMPC3.Contains(MPC3_V6[ii].Coordinates))
                {

                    SeenV6_MPC3.Add(MPC3_V6[ii]);
                    V7 tempV7_MPC3 = new V7(MPC3_V6[ii], MPC3_num);
                    SeenV7_MPC3.Add(tempV7_MPC3);
                    GlobalMPC3.Add(MPC3_V6[ii].Coordinates);

                    if (MPC_visualizer_ECS == true)
                    {
                        GameObject MPC3_clone = Instantiate(MPC3_Prefab, MPC3_V6[ii].Coordinates, Quaternion.identity);
                        MPC3_clone.name = "mpc3 #" + MPC3_num;
                    }

                    MPC3_num += 1;
                }
            }
            //int DMC_num = 0;
            for (int ii = 0; ii < DMC_V6.Count; ii++)
            {
                if (!GlobalDMC.Contains(DMC_V6[ii].Coordinates))
                {

                    SeenV6_DMC.Add(DMC_V6[ii]);
                    V7 tempV7_DMC = new V7(DMC_V6[ii], DMC_num);
                    SeenV7_DMC.Add(tempV7_DMC);
                    GlobalDMC.Add(DMC_V6[ii].Coordinates);

                    if (MPC_visualizer_ECS == true)
                    {
                        GameObject DMC_clone = Instantiate(DMC_Prefab, DMC_V6[ii].Coordinates, Quaternion.identity);
                        DMC_clone.name = "mpc3 #" + DMC_num;
                    }

                    DMC_num += 1;
                }
            }


            
            // draw areas for all vertices
            Vector3 point1 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 point2 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 shift1 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 shift2 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 dmc_shift1 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 dmc_shift2 = new Vector3(0.0f, 0.0f, 0.0f);

            for (int l = 0; l < floor_vrtx.Count - 1; l++)
            {
                point1 = floor_vrtx[l];
                point2 = floor_vrtx[l + 1];

                shift1 = floor_vrtx[l] + WW1 * floor_nrml[l];
                shift2 = floor_vrtx[l + 1] + WW1 * floor_nrml[l + 1];

                dmc_shift1 = floor_vrtx[l] + WW2 * floor_nrml[l];
                dmc_shift2 = floor_vrtx[l + 1] + WW2 * floor_nrml[l + 1];

                Area34 area_forloop = new Area34(point1, dmc_shift1, point2, dmc_shift2);
                //DrawArea(area_forloop);

                PickMPC(floor_nrml[l], point1, shift1, floor_nrml[l + 1], point2, shift2, NearbyMPC1, out List<V6> for_MPC1_V6);
                PickMPC(floor_nrml[l], point1, shift1, floor_nrml[l + 1], point2, shift2, NearbyMPC2, out List<V6> for_MPC2_V6);
                PickMPC(floor_nrml[l], point1, shift1, floor_nrml[l + 1], point2, shift2, NearbyMPC3, out List<V6> for_MPC3_V6);
                // picking DMCs
                PickMPC(floor_nrml[l], point1, dmc_shift1, floor_nrml[l + 1], point2, dmc_shift2, NearbyDMC, out List<V6> for_DMC_V6);

                // MPC1
                for (int ii = 0; ii < for_MPC1_V6.Count; ii++)
                {
                    if (!GlobalMPC1.Contains(for_MPC1_V6[ii].Coordinates))
                    {

                        SeenV6_MPC1.Add(for_MPC1_V6[ii]);
                        V7 tempV7_MPC1 = new V7(for_MPC1_V6[ii], MPC1_num);
                        SeenV7_MPC1.Add(tempV7_MPC1);
                        GlobalMPC1.Add(for_MPC1_V6[ii].Coordinates);

                        if (MPC_visualizer_ECS == true)
                        {
                            GameObject clone = Instantiate(MPC1_Prefab, for_MPC1_V6[ii].Coordinates, Quaternion.identity);
                            clone.name = "mpc1 #" + MPC1_num;
                        }

                        MPC1_num += 1;
                    }
                }
                // MPC2
                for (int ii = 0; ii < for_MPC2_V6.Count; ii++)
                {
                    if (!GlobalMPC2.Contains(for_MPC2_V6[ii].Coordinates))
                    {

                        SeenV6_MPC2.Add(for_MPC2_V6[ii]);
                        V7 tempV7_MPC2 = new V7(for_MPC2_V6[ii], MPC2_num);
                        SeenV7_MPC2.Add(tempV7_MPC2);
                        GlobalMPC2.Add(for_MPC2_V6[ii].Coordinates);

                        if (MPC_visualizer_ECS == true)
                        {
                            GameObject MPC2_clone = Instantiate(MPC2_Prefab, for_MPC2_V6[ii].Coordinates, Quaternion.identity);
                            MPC2_clone.name = "mpc2 #" + MPC2_num;
                        }

                        MPC2_num += 1;
                    }
                }
                // MPC3
                for (int ii = 0; ii < for_MPC3_V6.Count; ii++)
                {
                    if (!GlobalMPC3.Contains(for_MPC3_V6[ii].Coordinates))
                    {

                        SeenV6_MPC3.Add(for_MPC3_V6[ii]);
                        V7 tempV7_MPC3 = new V7(for_MPC3_V6[ii], MPC3_num);
                        SeenV7_MPC3.Add(tempV7_MPC3);
                        GlobalMPC3.Add(for_MPC3_V6[ii].Coordinates);

                        if (MPC_visualizer_ECS == true)
                        {
                            GameObject MPC3_clone = Instantiate(MPC3_Prefab, for_MPC3_V6[ii].Coordinates, Quaternion.identity);
                            MPC3_clone.name = "mpc3 #" + MPC3_num;
                        }

                        MPC3_num += 1;
                    }
                }
                
                // DMCs
                for (int ii = 0; ii < for_DMC_V6.Count; ii++)
                {
                    if (!GlobalDMC.Contains(for_DMC_V6[ii].Coordinates))
                    {

                        SeenV6_DMC.Add(for_DMC_V6[ii]);
                        V7 tempV7_DMC = new V7(for_DMC_V6[ii], DMC_num);
                        SeenV7_DMC.Add(tempV7_DMC);
                        GlobalDMC.Add(for_DMC_V6[ii].Coordinates);

                        if (MPC_visualizer_ECS == true)
                        {
                            GameObject DMC_clone = Instantiate(DMC_Prefab, for_DMC_V6[ii].Coordinates, Quaternion.identity);
                            DMC_clone.name = "dmc #" + DMC_num;
                        }

                        DMC_num += 1;
                    }
                }

            }

            

            //Area2D(vv1, vv2, out float S11);

        }
        Debug.Log("Number of seen MPC1 = " + SeenV7_MPC1.Count + "; check number " + MPC1_num);
        Debug.Log("Number of seen MPC2 = " + SeenV7_MPC2.Count + "; check number " + MPC2_num);
        Debug.Log("Number of seen MPC3 = " + SeenV7_MPC3.Count + "; check number " + MPC3_num);
        Debug.Log("Number of seen DMCs = " + SeenV7_DMC.Count + "; check number " + DMC_num);

        Debug.Log("Check Time t_original: " + ((Time.realtimeSinceStartup - t_original) * 1000f) + " ms");
    }

    














    void PickMPC(Vector3 n1, Vector3 p1, Vector3 s1, Vector3 n2, Vector3 p2, Vector3 s2, List<Vector3> ListOfNearbyScatterers, out List<V6> MPC_V6)
    {
        MPC_V6 = new List<V6>();

        if (p1 == p2) // the polygon is a triangle
        {
            Vector3 side1 = s1 - p1;
            Vector3 side2 = s2 - p2;
            // the area of the polygon 
            Area2D(side1, side2, out float S);

            // check if MPC is within the given traingle
            for (int i = 0; i < ListOfNearbyScatterers.Count; i++)
            {
                Vector3 v1 = s1 - ListOfNearbyScatterers[i];
                Vector3 v2 = p1 - ListOfNearbyScatterers[i];
                Vector3 v3 = s2 - ListOfNearbyScatterers[i];
                // calculate areas of the triangles; if the sum of 3 is equal to the polygon area, then the point is in the trianlge
                Area2D(v1, v2, out float S1);
                Area2D(v2, v3, out float S2);
                Area2D(v3, v1, out float S3);
                if (Mathf.Abs(S - S1 - S2 - S3) < 0.001)
                {
                    // if the sum of areas is almost equal to the whole area, then the MPC is included into the active set of MPCs
                    MPC_V6.Add(new V6(ListOfNearbyScatterers[i], 0.5f * (n1 + n2)));
                }
            }
        }
        else // the polygon is not a triangle
        {
            float dot_prod_normals = Vector3.Dot(n1, n2);

            if (dot_prod_normals <= 0)    // then the polygon is not convex (or triangle where p1 != p2)
            {
                // in this case, we consider two triangles separately
                Vector3 v11 = p1 - s1;
                Vector3 v12 = p2 - s1;
                Area2D(v11, v12, out float S1);

                Vector3 v21 = p1 - s2;
                Vector3 v22 = p2 - s2;
                Area2D(v21, v22, out float S2);
                // we consider such strange triangles in order to cover the biggest area defined at leas with one correct triangle
                // and at the same time to avoid considering a non-convex quadrangle

                for (int i = 0; i < ListOfNearbyScatterers.Count; i++)
                {
                    Vector3 vp1 = p1 - ListOfNearbyScatterers[i];
                    Vector3 vp2 = p2 - ListOfNearbyScatterers[i];

                    Vector3 vs1 = s1 - ListOfNearbyScatterers[i];
                    Vector3 vs2 = s2 - ListOfNearbyScatterers[i];

                    // common area that is related to both S1 and S2
                    Area2D(vp1, vp2, out float Spp);

                    // areas related to S1
                    Area2D(vp1, vs1, out float Sp1s1);
                    Area2D(vp2, vs1, out float Sp2s1);
                    
                    // areas related to S2
                    Area2D(vp1, vs2, out float Sp1s2);
                    Area2D(vp2, vs2, out float Sp2s2);
                    
                    // check if the point [i] belongs to S1 or S2
                    if (Mathf.Abs(S1 - Spp - Sp1s1 - Sp2s1) < 0.001)
                    {
                        MPC_V6.Add(new V6(ListOfNearbyScatterers[i], n1));
                    }
                    else if (Mathf.Abs(S2 - Spp - Sp1s2 - Sp2s2) < 0.001)
                    {
                        MPC_V6.Add(new V6(ListOfNearbyScatterers[i], n2));
                    }
                }
            }
            else // the polygon is convex
            {
                // the first triangle of the quadrangle
                Vector3 v11 = p1 - s1; 
                Vector3 v12 = s2 - s1;
                Area2D(v11, v12, out float S1);
                
                // the second triangle of the quadrangle
                Vector3 v21 = p1 - p2; 
                Vector3 v22 = s2 - p2;
                Area2D(v21, v22, out float S2);

                for (int i = 0; i < ListOfNearbyScatterers.Count; i++)
                {
                    Vector3 v1 = p1 - ListOfNearbyScatterers[i];
                    Vector3 v2 = s1 - ListOfNearbyScatterers[i];
                    Vector3 v3 = s2 - ListOfNearbyScatterers[i];
                    Vector3 v4 = p2 - ListOfNearbyScatterers[i];

                    Area2D(v1, v2, out float A1);
                    Area2D(v2, v3, out float A2);
                    Area2D(v3, v4, out float A3);
                    Area2D(v4, v1, out float A4);

                    float area_diff = Mathf.Abs(S1 + S2 - A1 - A2 - A3 - A4);

                    if (area_diff < 1)
                    {
                        MPC_V6.Add(new V6(ListOfNearbyScatterers[i], 0.5f * (n1 + n2)));
                    }
                }
            }
        }
    }








    // time measurement can be performed using this code
    //var watch = System.Diagnostics.Stopwatch.StartNew();
    // the code that you want to measure comes here
    //watch.Stop();
    //var elapsedMs = watch.ElapsedMilliseconds;

    /* Example on functions that work with lists

    List<int> asd1 = new List<int>();
    for (int i = 0; i < 6; i++) { asd1.Add(i); }
    List<int> asd2 = new List<int>();
    for (int i = 0; i < 3; i++) { asd2.Add(i + 10); }

    // according to https://stackoverflow.com/questions/4488054/merge-two-or-more-lists-into-one-in-c-sharp-net, the used approach is the most effective one
    var twolists = new List<int>(asd1.Count + asd2.Count);
    twolists.AddRange(asd2);
    twolists.AddRange(asd1);
    string out2list = "";
    foreach (int inlist in twolists) { out2list += inlist + "; "; }
    Debug.Log(out2list);

    twolists.RemoveRange(1, 3);
    string out2listupd = "";
    foreach (int inlist in twolists) { out2listupd += inlist + "; "; }
    Debug.Log(out2listupd);

    */













    void Area2D(Vector3 v1, Vector3 v2, out float S)
    {
        S = 0.5f * Mathf.Abs(-v1.x * v2.z + v1.z * v2.x);
    }

    void NearbyElements(float x_l, float x_r, float z_d, float z_u, List<Vector3> MPCs, out List<Vector3> NearbyMPCs)
    {
        NearbyMPCs = new List<Vector3>();
        for (int n = 0; n < MPCs.Count; n++)
        {
            if (MPCs[n].x >= x_l && MPCs[n].x <= x_r && MPCs[n].z >= z_d && MPCs[n].z <= z_u)
            {
                NearbyMPCs.Add(MPCs[n]);
            }
        }
    }

    
    void DrawArea(Area34 area)
    {
        Debug.DrawLine(area.p1, area.p2, Color.cyan, 5.0f);
        Debug.DrawLine(area.p2, area.s2, Color.yellow, 5.0f);
        Debug.DrawLine(area.s2, area.s1, Color.green, 5.0f);
        Debug.DrawLine(area.s1, area.p1, Color.yellow, 5.0f);
    }
    
}

public struct V6
{
    public Vector3 Coordinates;
    public Vector3 Normal;


    public V6(Vector3 vrtx, Vector3 nrml)
    {
        Coordinates = vrtx;
        Normal = nrml;
    }

}

public class SortV7byX : IComparer<V7>
{
    public int Compare(V7 x, V7 y)
    {
        return x.CoordNorm.Coordinates.x.CompareTo(y.CoordNorm.Coordinates.x);
    }
}
public class SortV7byZ : IComparer<V7>
{
    public int Compare(V7 x, V7 y)
    {
        return x.CoordNorm.Coordinates.z.CompareTo(y.CoordNorm.Coordinates.z);
    }
}

public struct V7
{
    public V6 CoordNorm;
    public int Number;


    public V7(V6 MPC, int num)
    {
        CoordNorm = MPC;
        Number = num;
    }

}

public class Area34
{
    public Vector3 p1;
    public Vector3 s1; // s1 = p1 + n1 * Width

    public Vector3 p2;
    public Vector3 s2; // s2 = p2 + n2 * Width

    public Area34(Vector3 point1, Vector3 shift1, Vector3 point2, Vector3 shift2)
    {
        p1 = point1;
        s1 = shift1;

        p2 = point2;
        s2 = shift2;
    }

}

