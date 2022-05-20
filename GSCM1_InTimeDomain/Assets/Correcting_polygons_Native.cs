using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
//using System;


public class Correcting_polygons_Native : MonoBehaviour
{

    

    public float WW1; // 3m width of the spawning area for MPC1
    public float WW2; // 12m width of the spawning area for DMC
    
    // visualizer
    public bool MPC_visualizer_ECS;

    // converting GameObjects to Entities
    public GameObject MPC1_Prefab;
    public GameObject MPC2_Prefab;
    public GameObject MPC3_Prefab;
    public GameObject DMC_Prefab;
    public GameObject CornerPrefab;

    private GameObject[] Buildings;
    //private GameObject[] Observation_Points;

    [HideInInspector] public NativeList<V6> ActiveV6_DMC_NativeList;
    [HideInInspector] public NativeList<V6> ActiveV6_MPC1_NativeList;
    [HideInInspector] public NativeList<V6> ActiveV6_MPC2_NativeList;
    [HideInInspector] public NativeList<V6> ActiveV6_MPC3_NativeList;
    
    [HideInInspector] public NativeList<V6> ActiveV6_MPC_NativeList;

    
    
    [HideInInspector] public NativeArray<float> ActiveV6_DMC_Power;
    [HideInInspector] public NativeArray<float> ActiveV6_MPC1_Power;
    [HideInInspector] public NativeArray<float> ActiveV6_MPC2_Power;
    [HideInInspector] public NativeArray<float> ActiveV6_MPC3_Power;
    
    [HideInInspector] public NativeArray<float> ActiveV6_MPC_Power;

    // defining positive directions for orthogonal tripples { MPC_Normal, MPC_Up, MPC_perp} (Note the sequence of vectors, it's important!)

    [HideInInspector] public NativeList<Vector3> Active_DMC_Perpendiculars;
    [HideInInspector] public NativeList<Vector3> Active_MPC1_Perpendiculars;
    [HideInInspector] public NativeList<Vector3> Active_MPC2_Perpendiculars;
    [HideInInspector] public NativeList<Vector3> Active_MPC3_Perpendiculars;

    [HideInInspector] public NativeList<Vector3> Active_MPC_Perpendiculars;

    // Data for diffraction effect
    [HideInInspector] public NativeList<Vector3> Active_CornersNormalsPerpendiculars;

    private void OnEnable()
    {
        ActiveV6_MPC1_NativeList = new NativeList<V6>(Allocator.Persistent);
        ActiveV6_MPC2_NativeList = new NativeList<V6>(Allocator.Persistent);
        ActiveV6_MPC3_NativeList = new NativeList<V6>(Allocator.Persistent);
        ActiveV6_DMC_NativeList = new NativeList<V6>(Allocator.Persistent);

        ActiveV6_MPC_NativeList = new NativeList<V6>(Allocator.Persistent);

        
        Active_DMC_Perpendiculars = new NativeList<Vector3>(Allocator.Persistent);
        Active_MPC1_Perpendiculars = new NativeList<Vector3>(Allocator.Persistent);
        Active_MPC2_Perpendiculars = new NativeList<Vector3>(Allocator.Persistent);
        Active_MPC3_Perpendiculars = new NativeList<Vector3>(Allocator.Persistent);

        Active_MPC_Perpendiculars = new NativeList<Vector3>(Allocator.Persistent);

        // Data for diffraction effect
        Active_CornersNormalsPerpendiculars = new NativeList<Vector3>(Allocator.Persistent);
    }
    private void OnDestroy()
    {
        ActiveV6_MPC1_NativeList.Dispose();
        ActiveV6_MPC2_NativeList.Dispose();
        ActiveV6_MPC3_NativeList.Dispose();
        ActiveV6_DMC_NativeList.Dispose();

        ActiveV6_MPC_NativeList.Dispose();


        ActiveV6_DMC_Power.Dispose();
        ActiveV6_MPC1_Power.Dispose();
        ActiveV6_MPC2_Power.Dispose();
        ActiveV6_MPC3_Power.Dispose();

        ActiveV6_MPC_Power.Dispose();


        Active_DMC_Perpendiculars.Dispose();
        Active_MPC1_Perpendiculars.Dispose();
        Active_MPC2_Perpendiculars.Dispose();
        Active_MPC3_Perpendiculars.Dispose();

        Active_MPC_Perpendiculars.Dispose();

        // Data for diffraction effect
        Active_CornersNormalsPerpendiculars.Dispose();
    }


    // Start is called before the first frame update
    void Start()
    {
        float t_native = Time.realtimeSinceStartup;
        GameObject MPC_Spawner = GameObject.Find("MPC_spawner");
        Scatterers_Spawning2 MPC_Script = MPC_Spawner.GetComponent<Scatterers_Spawning2>();
        List<Vector3> MPC1 = MPC_Script.MPC1_possiblepositionList;
        List<Vector3> MPC2 = MPC_Script.MPC2_possiblepositionList;
        List<Vector3> MPC3 = MPC_Script.MPC3_possiblepositionList;

        NativeList<V4> MPC1_Native = MPC_Script.MPC1_possiblepositionNativeList;
        NativeList<V4> MPC2_Native = MPC_Script.MPC2_possiblepositionNativeList;
        NativeList<V4> MPC3_Native = MPC_Script.MPC3_possiblepositionNativeList;
        NativeList<V4> DMC_Native = MPC_Script.DMC_possiblepositionNativeList;
        NativeArray<Vector3> OBS_Native = MPC_Script.Observation_Points_NativeArray;
        
        NativeList<V5> MPC_Native = MPC_Script.MPC_possiblepositionNativeList;
        
        

        // Declaring buildings and observation points
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        // The number of observation points should not be too big, a few is enough
        //Observation_Points = GameObject.FindGameObjectsWithTag("Observation_Points");
        // Define the main road direction
        //Vector3 main_axis = (Observation_Points[0].transform.position - Observation_Points[1].transform.position).normalized;
        Vector3 main_axis = (OBS_Native[0] - OBS_Native[1]).normalized;
        Vector3 perp_axis = new Vector3(main_axis.z, 0, -main_axis.x);

        // subset of the seen Buldings
        List<GameObject> Building_list = new List<GameObject>();

        // Going through all observation points
        for (int k = 0; k < OBS_Native.Length; k++)
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
                        //if (!Physics.Linecast(Observation_Points[k].transform.position, vrtx[v] + 1.1f * nrml[v]))
                        if (!Physics.Linecast(OBS_Native[k], vrtx[v] + 1.1f * nrml[v]))
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

        // int inclusion_count = 0;
        // analyzing each seen building separately
        NativeArray<V6> MPC1_Pick = new NativeArray<V6>(MPC1_Native.Length, Allocator.TempJob);
        NativeArray<V6> MPC2_Pick = new NativeArray<V6>(MPC2_Native.Length, Allocator.TempJob);
        NativeArray<V6> MPC3_Pick = new NativeArray<V6>(MPC3_Native.Length, Allocator.TempJob);
        NativeArray<V6> DMC_Pick = new NativeArray<V6>(DMC_Native.Length, Allocator.TempJob);
        // perpendicular directions
        NativeArray<Vector3> MPC1_Pick_perp = new NativeArray<Vector3>(MPC1_Native.Length, Allocator.TempJob);
        NativeArray<Vector3> MPC2_Pick_perp = new NativeArray<Vector3>(MPC2_Native.Length, Allocator.TempJob);
        NativeArray<Vector3> MPC3_Pick_perp = new NativeArray<Vector3>(MPC3_Native.Length, Allocator.TempJob);
        NativeArray<Vector3> DMC_Pick_perp = new NativeArray<Vector3>(DMC_Native.Length, Allocator.TempJob);

        

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

            // This procedure is needed for defining the corner the building for further Diffraction simulation
            string building_name = Building_list[k].name;
            
            Vector3 corner1 = new Vector3(0, 0, 0);
            Vector3 normal1 = new Vector3(0, 0, 0);
            Vector3 perpen1 = new Vector3(0, 0, 0);

            Vector3 corner2 = new Vector3(0, 0, 0);
            Vector3 normal2 = new Vector3(0, 0, 0);
            Vector3 perpen2 = new Vector3(0, 0, 0);

            if (building_name == "Building258")
            {
                Debug.Log(building_name);
                corner1 = SortedByX[2];
                corner2 = SortedByX[6];

                GameObject Corner1 = Instantiate(CornerPrefab, corner1, Quaternion.identity);
                GameObject Corner2 = Instantiate(MPC1_Prefab, corner2, Quaternion.identity);

                int corner1_count = 0;
                for (int v = 0; v < floor_vrtx.Count; v++)
                {
                    if (corner1 == floor_vrtx[v])
                    {
                        if (corner1_count == 0)
                        {
                            normal1 = floor_nrml[v]; // I use the assumption that the vertex is used only twice
                            corner1_count = 1;
                        }
                    }
                    if (corner2 == floor_vrtx[v])
                    {
                        normal2 = floor_nrml[v]; // I use the assumption that the vertex is used only twice
                    }
                }
                normal1 = normal1.normalized;
                perpen1 = new Vector3(-normal1.z, 0, normal1.x);
                Debug.DrawLine(corner1, corner1 + 5 * normal1, Color.red, 30f);

                normal2 = normal2.normalized;
                perpen2 = new Vector3(-normal2.z, 0, normal2.x);
                Debug.DrawLine(corner2, corner2 + 5 * normal2, Color.red, 30f);

                

                Active_CornersNormalsPerpendiculars.Add(corner1);
                Active_CornersNormalsPerpendiculars.Add(normal1);
                Active_CornersNormalsPerpendiculars.Add(perpen1);

                Active_CornersNormalsPerpendiculars.Add(corner2);
                Active_CornersNormalsPerpendiculars.Add(normal2);
                Active_CornersNormalsPerpendiculars.Add(perpen2);

                // this is done to model single edge building (keep in mind, multipath go through the angle)
                // Vector3 corner_test = (corner1 + corner2) / 2 + 3.0f * (normal1 + normal2) / 2;
                Vector3 corner_test = new Vector3(53.64f, 0.0f, -26.89f);
                GameObject Corner_Test = Instantiate(CornerPrefab, corner_test, Quaternion.identity);

                Active_CornersNormalsPerpendiculars.Add(corner_test);
                
            }

            /*
            if (building_name == "Building321")
            {
                Debug.Log(building_name);
                corner321 = SortedByX[SortedByX.Count - 1];

                GameObject Corner321 = Instantiate(CornerPrefab, corner321, Quaternion.identity);

                for (int v = 0; v < floor_vrtx.Count; v++)
                {
                    if (corner321 == floor_vrtx[v])
                    {
                        normal321 += floor_nrml[v]; // I use the assumption that the vertex is used only twice
                    }
                }
                normal321 = normal321.normalized;
                perpen321 = new Vector3(-normal321.z, 0, normal321.x);
                Debug.DrawLine(corner321, corner321 + 5 * normal321, Color.red, 30f);

                Active_CornersNormalsPerpendiculars.Add(corner321);
                Active_CornersNormalsPerpendiculars.Add(normal321);
                Active_CornersNormalsPerpendiculars.Add(perpen321);
            }
            if (building_name == "Building334")
            {
                Debug.Log(building_name);
                corner334 = SortedByX[0];
                GameObject Corner334 = Instantiate(CornerPrefab, corner334, Quaternion.identity);
                
                for (int v = 0; v < floor_vrtx.Count; v++)
                {
                    if (corner334 == floor_vrtx[v])
                    {
                        normal334 += floor_nrml[v]; // I use the assumption that the vertex is used only twice
                    }
                }
                normal334 = normal334.normalized;
                perpen334 = new Vector3(-normal334.z, 0, normal334.x);
                Debug.DrawLine(corner334, corner334 + 5 * normal334, Color.red, 30f);

                Active_CornersNormalsPerpendiculars.Add(corner334);
                Active_CornersNormalsPerpendiculars.Add(normal334);
                Active_CornersNormalsPerpendiculars.Add(perpen334);
            }
            */

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Defining areas around building where scatterers can be located
            //////////////////////////////////////////////////////////////////////////////////////////////////////////



            // defining necessary elements for the polygon that connects the last and the first vertices
            Vector3 n1_0 = floor_nrml[floor_vrtx.Count - 1];
            Vector3 p1_0 = floor_vrtx[floor_vrtx.Count - 1];
            Vector3 s1_0 = floor_vrtx[floor_vrtx.Count - 1] + WW1 * floor_nrml[floor_vrtx.Count - 1];
            Vector3 s1_0_dmc = floor_vrtx[floor_vrtx.Count - 1] + WW2 * floor_nrml[floor_vrtx.Count - 1];

            Vector3 n2_0 = floor_nrml[0];
            Vector3 p2_0 = floor_vrtx[0]; 
            Vector3 s2_0 = floor_vrtx[0] + WW1 * floor_nrml[0];
            Vector3 s2_0_dmc = floor_vrtx[0] + WW2 * floor_nrml[0];

            Area34 area = new Area34(p1_0, s1_0, p2_0, s2_0);
            


            //float t1 = Time.realtimeSinceStartup;
            // MPC1
            PickMPCParallel pickMPCParallel1 = new PickMPCParallel
            {
                n1 = n1_0,
                p1 = p1_0,
                s1 = s1_0,
                n2 = n2_0,
                p2 = p2_0,
                s2 = s2_0,

                Xleft = x_l,
                Xright = x_r,
                Zdown = z_d,
                Zup = z_u,

                MPC_array = MPC1_Native,
                MPC_V6 = MPC1_Pick,
                MPC_perp = MPC1_Pick_perp,
            };
            JobHandle jobHandle_Pick1 = pickMPCParallel1.Schedule(MPC1_Native.Length, 100);
            // MPC2
            PickMPCParallel pickMPCParallel2 = new PickMPCParallel
            {
                n1 = n1_0,
                p1 = p1_0,
                s1 = s1_0,
                n2 = n2_0,
                p2 = p2_0,
                s2 = s2_0,

                Xleft = x_l,
                Xright = x_r,
                Zdown = z_d,
                Zup = z_u,

                MPC_array = MPC2_Native,
                MPC_V6 = MPC2_Pick,
                MPC_perp = MPC2_Pick_perp,
            };
            JobHandle jobHandle_Pick2 = pickMPCParallel2.Schedule(MPC2_Native.Length, 100, jobHandle_Pick1);
            // MPC3
            PickMPCParallel pickMPCParallel3 = new PickMPCParallel
            {
                n1 = n1_0,
                p1 = p1_0,
                s1 = s1_0,
                n2 = n2_0,
                p2 = p2_0,
                s2 = s2_0,

                Xleft = x_l,
                Xright = x_r,
                Zdown = z_d,
                Zup = z_u,

                MPC_array = MPC3_Native,
                MPC_V6 = MPC3_Pick,
                MPC_perp = MPC3_Pick_perp,
            };
            JobHandle jobHandle_Pick3 = pickMPCParallel3.Schedule(MPC3_Native.Length, 100, jobHandle_Pick2);
            // DMC
            PickMPCParallel pickMPCParallel4 = new PickMPCParallel
            {
                n1 = n1_0,
                p1 = p1_0,
                s1 = s1_0_dmc,
                n2 = n2_0,
                p2 = p2_0,
                s2 = s2_0_dmc,

                Xleft = x_l,
                Xright = x_r,
                Zdown = z_d,
                Zup = z_u,

                MPC_array = DMC_Native,
                MPC_V6 = DMC_Pick,
                MPC_perp = DMC_Pick_perp,
            };
            JobHandle jobHandle_Pick4 = pickMPCParallel4.Schedule(DMC_Native.Length, 100, jobHandle_Pick3);

            jobHandle_Pick1.Complete();
            jobHandle_Pick2.Complete();
            jobHandle_Pick3.Complete();
            jobHandle_Pick4.Complete();

            //Debug.Log("Check Time t1: " + ((Time.realtimeSinceStartup - t1) * 1000f) + " ms");


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

                // MPC1
                PickMPCParallel pickMPCParallel_for = new PickMPCParallel
                {
                    n1 = floor_nrml[l],
                    p1 = point1,
                    s1 = shift1,
                    n2 = floor_nrml[l + 1],
                    p2 = point2,
                    s2 = shift2,

                    Xleft = x_l,
                    Xright = x_r,
                    Zdown = z_d,
                    Zup = z_u,

                    MPC_array = MPC1_Native,
                    MPC_V6 = MPC1_Pick,
                    MPC_perp = MPC1_Pick_perp,
                };
                JobHandle jobHandle_Pick1_for = pickMPCParallel_for.Schedule(MPC1_Native.Length, 100);
                // MPC2
                PickMPCParallel pickMPCParallel2_for = new PickMPCParallel
                {
                    n1 = floor_nrml[l],
                    p1 = point1,
                    s1 = shift1,
                    n2 = floor_nrml[l + 1],
                    p2 = point2,
                    s2 = shift2,

                    Xleft = x_l,
                    Xright = x_r,
                    Zdown = z_d,
                    Zup = z_u,

                    MPC_array = MPC2_Native,
                    MPC_V6 = MPC2_Pick,
                    MPC_perp = MPC2_Pick_perp,
                };
                JobHandle jobHandle_Pick2_for = pickMPCParallel2_for.Schedule(MPC2_Native.Length, 100, jobHandle_Pick1_for);
                // MPC3
                PickMPCParallel pickMPCParallel3_for = new PickMPCParallel
                {
                    n1 = floor_nrml[l],
                    p1 = point1,
                    s1 = shift1,
                    n2 = floor_nrml[l + 1],
                    p2 = point2,
                    s2 = shift2,

                    Xleft = x_l,
                    Xright = x_r,
                    Zdown = z_d,
                    Zup = z_u,

                    MPC_array = MPC3_Native,
                    MPC_V6 = MPC3_Pick,
                    MPC_perp = MPC3_Pick_perp,
                };
                JobHandle jobHandle_Pick3_for = pickMPCParallel3_for.Schedule(MPC3_Native.Length, 100, jobHandle_Pick2_for);
                // DMC
                PickMPCParallel pickMPCParallel4_for = new PickMPCParallel
                {
                    n1 = floor_nrml[l],
                    p1 = point1,
                    s1 = dmc_shift1,
                    n2 = floor_nrml[l + 1],
                    p2 = point2,
                    s2 = dmc_shift2,

                    Xleft = x_l,
                    Xright = x_r,
                    Zdown = z_d,
                    Zup = z_u,

                    MPC_array = DMC_Native,
                    MPC_V6 = DMC_Pick,
                    MPC_perp = DMC_Pick_perp,
                };
                JobHandle jobHandle_Pick4_for = pickMPCParallel4_for.Schedule(DMC_Native.Length, 100, jobHandle_Pick3_for);

                jobHandle_Pick1_for.Complete();
                jobHandle_Pick2_for.Complete();
                jobHandle_Pick3_for.Complete();
                jobHandle_Pick4_for.Complete();
            }
        }

        //float t_seq = Time.realtimeSinceStartup;
        // DMC
        int inclusion_num0 = 0;
        for (int ii = 0; ii < DMC_Native.Length; ii++)
        {
            if (DMC_Native[ii].Inclusion == 1)
            {
                inclusion_num0 += 1;
                ActiveV6_DMC_NativeList.Add(DMC_Pick[ii]); // DMCs only
                ActiveV6_MPC_NativeList.Add(DMC_Pick[ii]); // all MPCs
                // MPC perpendicular directions
                Active_DMC_Perpendiculars.Add(DMC_Pick_perp[ii]);
                Active_MPC_Perpendiculars.Add(DMC_Pick_perp[ii]);

                if (MPC_visualizer_ECS == true)
                {
                    GameObject DMC_clone = Instantiate(DMC_Prefab, DMC_Native[ii].Coordinates, Quaternion.identity);

                    //GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    //cleared_sphere.transform.position = DMC_Native[ii].Coordinates;
                    //Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                    //cleared_sphere.name = "DMC #" + (inclusion_num4 - 1);

                    //Debug.DrawLine(DMC_Pick[ii].Coordinates, DMC_Pick[ii].Coordinates + 5*DMC_Pick[ii].Normal, Color.red, 30f);
                }
            }
        }

        // MPC1
        int inclusion_num1 = 0;
        for (int ii = 0; ii < MPC1_Native.Length; ii++)
        {
            if (MPC1_Native[ii].Inclusion == 1)
            { 
                inclusion_num1 += 1;
                ActiveV6_MPC1_NativeList.Add(MPC1_Pick[ii]);
                ActiveV6_MPC_NativeList.Add(MPC1_Pick[ii]);

                // MPC perpendicular directions
                Active_MPC1_Perpendiculars.Add(MPC1_Pick_perp[ii]);
                Active_MPC_Perpendiculars.Add(MPC1_Pick_perp[ii]);

                if (MPC_visualizer_ECS == true)
                { GameObject MPC1_clone = Instantiate(MPC1_Prefab, MPC1_Native[ii].Coordinates, Quaternion.identity); }
            }
        }

        // MPC2
        int inclusion_num2 = 0;
        for (int ii = 0; ii < MPC2_Native.Length; ii++)
        {
            if (MPC2_Native[ii].Inclusion == 1)
            {
                
                ActiveV6_MPC2_NativeList.Add(MPC2_Pick[ii]);
                ActiveV6_MPC_NativeList.Add(MPC2_Pick[ii]);
                // MPC perpendicular directions
                Active_MPC2_Perpendiculars.Add(MPC2_Pick_perp[ii]);
                Active_MPC_Perpendiculars.Add(MPC2_Pick_perp[ii]);

                if (MPC_visualizer_ECS == true)
                { 
                    GameObject MPC2_clone = Instantiate(MPC2_Prefab, MPC2_Native[ii].Coordinates, Quaternion.identity);
                    MPC2_clone.name = "MPC2 # " + inclusion_num2;
                    //Debug.DrawLine(MPC2_Pick[ii].Coordinates, MPC2_Pick[ii].Coordinates + 5 * MPC2_Pick[ii].Normal, Color.cyan, 10f);
                    //Debug.DrawLine(MPC2_Pick[ii].Coordinates, MPC2_Pick[ii].Coordinates + 5 * MPC2_Pick_perp[ii], Color.yellow, 10f);
                }
                inclusion_num2 += 1;
            }
        }
        
        // MPC3
        int inclusion_num3 = 0;
        for (int ii = 0; ii < MPC3_Native.Length; ii++)
        {
            if (MPC3_Native[ii].Inclusion == 1)
            {
                ActiveV6_MPC3_NativeList.Add(MPC3_Pick[ii]);
                ActiveV6_MPC_NativeList.Add(MPC3_Pick[ii]);
                // MPC perpendicular directions
                Active_MPC3_Perpendiculars.Add(MPC3_Pick_perp[ii]);
                Active_MPC_Perpendiculars.Add(MPC3_Pick_perp[ii]);
                if (MPC_visualizer_ECS == true)
                { 
                    GameObject MPC3_clone = Instantiate(MPC3_Prefab, MPC3_Native[ii].Coordinates, Quaternion.identity);
                    MPC3_clone.name = "MPC3 # " + inclusion_num3;
                    //Debug.DrawLine(MPC3_Pick[ii].Coordinates, MPC3_Pick[ii].Coordinates + 5 * MPC3_Pick[ii].Normal, Color.cyan, 10f);
                    //Debug.DrawLine(MPC3_Pick[ii].Coordinates, MPC3_Pick[ii].Coordinates + 5 * MPC3_Pick_perp[ii], Color.yellow, 10f);
                }
                inclusion_num3 += 1;
            }
        }

        //Debug.Log("Check Time for Sequ: " + ((Time.realtimeSinceStartup - t_seq) * 1000f) + " ms");
        Debug.Log("DMC " + inclusion_num0 + "; MPC1 " + inclusion_num1 + "; MPC2 " + inclusion_num2 + "; MPC3 " + inclusion_num3);


        #region Generating attenuation coefficients for MPCs
        // Power generation for MPCs
        ActiveV6_DMC_Power = new NativeArray<float>(ActiveV6_DMC_NativeList.Length, Allocator.Persistent);
        ActiveV6_MPC1_Power = new NativeArray<float>(ActiveV6_MPC1_NativeList.Length, Allocator.Persistent);
        ActiveV6_MPC2_Power = new NativeArray<float>(ActiveV6_MPC2_NativeList.Length, Allocator.Persistent);
        ActiveV6_MPC3_Power = new NativeArray<float>(ActiveV6_MPC3_NativeList.Length, Allocator.Persistent);
        int MPC_length = ActiveV6_DMC_NativeList.Length + ActiveV6_MPC1_NativeList.Length + ActiveV6_MPC2_NativeList.Length + ActiveV6_MPC3_NativeList.Length;
        ActiveV6_MPC_Power = new NativeArray<float>(MPC_length, Allocator.Persistent);
        //int clbrcoef = 10;
        Vector2 DMCPower = new Vector2(-80, -68);  // + new Vector2(1,1) * clbrcoef; // (Mathf.Pow(10, (-80/20)), Mathf.Pow(10, (-68/20))); // (-80, -68)[dB] % Diffuse 1st)
        Vector2 MPC1Power = new Vector2(-65, -48); // (Mathf.Pow(10, (-65/20)), Mathf.Pow(10, (-48/20))); // (-65, -48)[dB] % 1st
        Vector2 MPC2Power = new Vector2(-70, -59); // (Mathf.Pow(10, (-70/20)), Mathf.Pow(10, (-59/20))); // (-70, -59)[dB] % 2nd
        Vector2 MPC3Power = new Vector2(-75, -65); // (Mathf.Pow(10, (-75/20)), Mathf.Pow(10, (-65/20))); // (-75, -65)[dB] % 3rd
        
        // this solution comes from https://forum.unity.com/threads/mathematics-random-with-in-ijobprocesscomponentdata.598192/#post-4009273
        NativeArray<Unity.Mathematics.Random> rngs = new NativeArray<Unity.Mathematics.Random>(JobsUtility.MaxJobThreadCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        { 
            rngs[i] = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue)); 
        }

        MPCPowerGeneration dmcPowerGeneration = new MPCPowerGeneration
        {
            rngs = rngs,
            MinMax = DMCPower,
            Array = ActiveV6_DMC_Power,
        };
        JobHandle power0 = dmcPowerGeneration.Schedule(ActiveV6_DMC_Power.Length, 64);
        power0.Complete();
        for (int i = 0; i < ActiveV6_DMC_Power.Length; i++)
        {
            ActiveV6_MPC_Power[i] = ActiveV6_DMC_Power[i];
        }

        MPCPowerGeneration mpc1PowerGeneration = new MPCPowerGeneration
        {
            rngs = rngs,
            MinMax = MPC1Power,
            Array = ActiveV6_MPC1_Power,
        };
        JobHandle power1 = mpc1PowerGeneration.Schedule(ActiveV6_MPC1_Power.Length, 64);
        power1.Complete();
        for (int i = 0; i < ActiveV6_MPC1_Power.Length; i++)
        {
            ActiveV6_MPC_Power[i + ActiveV6_DMC_Power.Length] = ActiveV6_MPC1_Power[i];
        }

        MPCPowerGeneration mpc2PowerGeneration = new MPCPowerGeneration
        {
            rngs = rngs,
            MinMax = MPC2Power,
            Array = ActiveV6_MPC2_Power,
        };
        JobHandle power2 = mpc2PowerGeneration.Schedule(ActiveV6_MPC2_Power.Length, 64);
        power2.Complete();
        for (int i = 0; i < ActiveV6_MPC2_Power.Length; i++)
        {
            
            
            ActiveV6_MPC_Power[i + ActiveV6_DMC_Power.Length + ActiveV6_MPC1_Power.Length] = Mathf.Pow( ActiveV6_MPC2_Power[i], 0.5f ); // It seems that Carl uses only one multiplication to the coefficient
            ActiveV6_MPC2_Power[i] = Mathf.Pow(ActiveV6_MPC2_Power[i], 0.5f);

            float asd = ActiveV6_MPC2_Power[i];
            float zxc = Mathf.Pow(asd, 0.5f);
            //float qwe = 1.0f;
        }


        MPCPowerGeneration mpc3PowerGeneration = new MPCPowerGeneration
        {
            rngs = rngs,
            MinMax = MPC3Power,
            Array = ActiveV6_MPC3_Power,
        };
        JobHandle power3 = mpc3PowerGeneration.Schedule(ActiveV6_MPC3_Power.Length, 64);
        power3.Complete();
        for (int i = 0; i < ActiveV6_MPC3_Power.Length; i++)
        {
            float asd = ActiveV6_MPC3_Power[i];
            float zxc = Mathf.Pow(ActiveV6_MPC3_Power[i], 0.3333f);
            ActiveV6_MPC_Power[i + ActiveV6_DMC_Power.Length + ActiveV6_MPC1_Power.Length + ActiveV6_MPC2_Power.Length] = Mathf.Pow( ActiveV6_MPC3_Power[i], 0.3333f ); // It seems that Carl uses only one multiplication to the coefficient
            ActiveV6_MPC3_Power[i] = Mathf.Pow(ActiveV6_MPC3_Power[i], 0.3333f);
        }
        #endregion





        MPC1_Pick.Dispose();
        MPC2_Pick.Dispose();
        MPC3_Pick.Dispose();
        DMC_Pick.Dispose();
        MPC1_Pick_perp.Dispose();
        MPC2_Pick_perp.Dispose();
        MPC3_Pick_perp.Dispose();
        DMC_Pick_perp.Dispose();

        rngs.Dispose();
        Debug.Log("Check Time t_native: " + ((Time.realtimeSinceStartup - t_native) * 1000f) + " ms");
    }

    
    void DrawArea(Area34 area)
    {
        Debug.DrawLine(area.p1, area.p2, Color.cyan, 1.0f);
        Debug.DrawLine(area.p2, area.s2, Color.yellow, 1.0f);
        Debug.DrawLine(area.s2, area.s1, Color.green, 1.0f);
        Debug.DrawLine(area.s1, area.p1, Color.yellow, 1.0f);
    }
    
}

[BurstCompile]
public struct MPCPowerGeneration : IJobParallelFor
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Unity.Mathematics.Random> rngs;
    [NativeSetThreadIndex]
    readonly int threadId;

    public Vector2 MinMax;
    public NativeArray<float> Array;
    
    public void Execute(int index)
    {
        var rng = rngs[threadId];
        float PowerAdjustingShift = 32.0f;
        float MPC_power = rng.NextFloat(MinMax.x + PowerAdjustingShift, MinMax.y + PowerAdjustingShift);
        
        rngs[threadId] = rng;

        //float asd = Mathf.Pow(10, MPC_power / 20);

        Array[index] = Mathf.Pow(10, MPC_power/20); // have to calculate power here since float cannot distinguish between 0.001 and 0.00105
    }
}

[BurstCompile]
public struct NativeListAddFunc : IJob
{
    [ReadOnly] public NativeArray<V6> Array;
    public NativeList<V6> List;
    
    public void Execute()
    {
        for (int i = 0; i < Array.Length; i++)
        {
            if (Array[i].Coordinates.x != 0)
            { List.Add(Array[i]); }
        }
    }
}

[BurstCompile]
public struct Append : IJobParallelForFilter
{
    public NativeArray<V4> Array;
    public bool Execute(int index)
    {
        return Array[index].Inclusion == 1;
    }
}

