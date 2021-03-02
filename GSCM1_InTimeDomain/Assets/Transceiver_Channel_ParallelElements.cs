using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

public class Transceiver_Channel_ParallelElements : MonoBehaviour
{
    //[SerializeField] private bool useJobs;

    public int Reachable_distance;
    // defining moved distance
    Vector3 d1; // for antenna #1

    // defining initial position of antennas and the area of search
    Vector3 old_position1; // for antenna #1


    float ant1_min_x;
    float ant1_max_x;
    float ant1_min_z;
    float ant1_max_z;


    private int XMIN_index1;
    private int XMAX_index1;
    private int ZMIN_index1;
    private int ZMAX_index1;

    private int XMIN_index2;
    private int XMAX_index2;
    private int ZMIN_index2;
    private int ZMAX_index2;

    private int XMIN_index3;
    private int XMAX_index3;
    private int ZMIN_index3;
    private int ZMAX_index3;

    public List<int> seen_MPC1 = new List<int>();
    public List<int> seen_MPC2 = new List<int>();
    public List<int> seen_MPC3 = new List<int>();

    //public bool Activate_MPCs;
    public bool Activate_MPC1;
    public bool Activate_MPC2;
    public bool Activate_MPC3;

    ///////////////////////////////////////////////////////////////////////////////////
    /// Start reading data from other scripts
    ///////////////////////////////////////////////////////////////////////////////////

    List<V6> MPC1;
    List<V7> V7MPC1;
    List<int> inarea_MPC1 = new List<int>();
    GameObject near_MPC1;

    List<V6> MPC2;
    List<V7> V7MPC2;
    List<int> inarea_MPC2 = new List<int>();
    GameObject near_MPC2;

    List<V6> MPC3;
    List<V7> V7MPC3;
    List<int> inarea_MPC3 = new List<int>();
    GameObject near_MPC3;

    // sorted lists by x and z: MPC1
    List<V7> byX_V7MPC1;
    List<V7> byZ_V7MPC1;
    // sorted lists by x and z: MPC1
    List<V7> byX_V7MPC2;
    List<V7> byZ_V7MPC2;
    // sorted lists by x and z: MPC1
    List<V7> byX_V7MPC3;
    List<V7> byZ_V7MPC3;
    ///////////////////////////////////////////////////////////////////////////////////
    /// Finish reading data from other scripts
    ///////////////////////////////////////////////////////////////////////////////////
    List<V7> temp_V7_list1 = new List<V7>(); // this list will be used for sorting
    List<V7> temp_V7_list2 = new List<V7>(); // this list will be used for sorting
    List<V7> temp_V7_list3 = new List<V7>(); // this list will be used for sorting

    // Start is called before the first frame update
    void Start()
    {

        old_position1 = transform.position; // antenna #1 position in the beginning
        // in x axis
        ant1_min_x = old_position1.x - Reachable_distance;
        ant1_max_x = old_position1.x + Reachable_distance;
        // in z axis
        ant1_min_z = old_position1.z - Reachable_distance;
        ant1_max_z = old_position1.z + Reachable_distance;

        ///////////////////////////////////////////////////////////////////////////////////
        /// Start reading data from other scripts
        ///////////////////////////////////////////////////////////////////////////////////
        // reading generated MPCs from the mpc spawner script
        GameObject MPC_Spawner = GameObject.Find("Direct_Solution");
        Correcting_polygons MPC_Script = MPC_Spawner.GetComponent<Correcting_polygons>();
        MPC1 = MPC_Script.SeenV6_MPC1;
        V7MPC1 = MPC_Script.SeenV7_MPC1;
        MPC2 = MPC_Script.SeenV6_MPC2;
        V7MPC2 = MPC_Script.SeenV7_MPC2;
        MPC3 = MPC_Script.SeenV6_MPC3;
        V7MPC3 = MPC_Script.SeenV7_MPC3;


        // sorted lists by x and z: MPC1
        byX_V7MPC1 = V7MPC1.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        byZ_V7MPC1 = V7MPC1.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();
        // sorted lists by x and z: MPC1
        byX_V7MPC2 = V7MPC2.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        byZ_V7MPC2 = V7MPC2.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();
        // sorted lists by x and z: MPC1
        byX_V7MPC3 = V7MPC3.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        byZ_V7MPC3 = V7MPC3.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();



        ///////////////////////////////////////////////////////////////////////////////////
        /// Finish reading data from other scripts
        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////
        // search for seen mpcs within the search area
        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC1

        ///////////////////////////////////////////////////////////////////////////////////
        //Debug.Log("Check0");

        for (int i = 0; i < MPC1.Count; i++)
        {
            if ((MPC1[i].Coordinates.x > ant1_min_x) && (MPC1[i].Coordinates.x < ant1_max_x) && (MPC1[i].Coordinates.z > ant1_min_z) && (MPC1[i].Coordinates.z < ant1_max_z))
            {
                //inarea_MPC1.Add(i);
                V7 temp_V7 = new V7(MPC1[i], i);
                temp_V7_list1.Add(temp_V7);
                inarea_MPC1.Add(i);
            }
        }

        // finding edges of inarea_MPC1 in x and z directions
        List<V7> XsortV7 = temp_V7_list1.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        List<V7> ZsortV7 = temp_V7_list1.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

        // finding elements indexes in the sorted lists
        // for MPC1
        XMIN_index1 = byX_V7MPC1.FindIndex(V7elm => V7elm.Number == XsortV7[0].Number);
        XMAX_index1 = byX_V7MPC1.FindIndex(V7elm => V7elm.Number == XsortV7[XsortV7.Count - 1].Number);
        ZMIN_index1 = byZ_V7MPC1.FindIndex(V7elm => V7elm.Number == ZsortV7[0].Number);
        ZMAX_index1 = byZ_V7MPC1.FindIndex(V7elm => V7elm.Number == ZsortV7[XsortV7.Count - 1].Number);

        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC2
        ///////////////////////////////////////////////////////////////////////////////////

        for (int i = 0; i < MPC2.Count; i++)
        {
            if ((MPC2[i].Coordinates.x > ant1_min_x) && (MPC2[i].Coordinates.x < ant1_max_x) && (MPC2[i].Coordinates.z > ant1_min_z) && (MPC2[i].Coordinates.z < ant1_max_z))
            {
                //inarea_MPC1.Add(i);
                V7 temp_V7 = new V7(MPC2[i], i);
                temp_V7_list2.Add(temp_V7);
                inarea_MPC2.Add(i);
            }
        }

        // finding edges of inarea_MPC1 in x and z directions
        List<V7> XsortV7_2 = temp_V7_list2.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        List<V7> ZsortV7_2 = temp_V7_list2.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

        // finding elements indexes in the sorted lists
        // for MPC2
        XMIN_index2 = byX_V7MPC2.FindIndex(V7elm => V7elm.Number == XsortV7_2[0].Number);
        XMAX_index2 = byX_V7MPC2.FindIndex(V7elm => V7elm.Number == XsortV7_2[XsortV7_2.Count - 1].Number);
        ZMIN_index2 = byZ_V7MPC2.FindIndex(V7elm => V7elm.Number == ZsortV7_2[0].Number);
        ZMAX_index2 = byZ_V7MPC2.FindIndex(V7elm => V7elm.Number == ZsortV7_2[XsortV7_2.Count - 1].Number);

        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC3
        ///////////////////////////////////////////////////////////////////////////////////

        for (int i = 0; i < MPC3.Count; i++)
        {
            if ((MPC3[i].Coordinates.x > ant1_min_x) && (MPC3[i].Coordinates.x < ant1_max_x) && (MPC3[i].Coordinates.z > ant1_min_z) && (MPC3[i].Coordinates.z < ant1_max_z))
            {
                //inarea_MPC1.Add(i);
                V7 temp_V7 = new V7(MPC3[i], i);
                temp_V7_list3.Add(temp_V7);
                inarea_MPC3.Add(i);
            }
        }

        // finding edges of inarea_MPC1 in x and z directions
        List<V7> XsortV7_3 = temp_V7_list3.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        List<V7> ZsortV7_3 = temp_V7_list3.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

        // finding elements indexes in the sorted lists
        // for MPC3
        XMIN_index3 = byX_V7MPC3.FindIndex(V7elm => V7elm.Number == XsortV7_3[0].Number);
        XMAX_index3 = byX_V7MPC3.FindIndex(V7elm => V7elm.Number == XsortV7_3[XsortV7_3.Count - 1].Number);
        ZMIN_index3 = byZ_V7MPC3.FindIndex(V7elm => V7elm.Number == ZsortV7_3[0].Number);
        ZMAX_index3 = byZ_V7MPC3.FindIndex(V7elm => V7elm.Number == ZsortV7_3[XsortV7_3.Count - 1].Number);
    }

    // Update is called once per frame
    void Update()
    {
        d1 = transform.position - old_position1;

        // update the old position. Now, current position become old for the next position.
        old_position1 = transform.position;
        // in x axis
        ant1_min_x += d1.x;
        ant1_max_x += d1.x;
        // in z axis
        ant1_min_z += d1.z;
        ant1_max_z += d1.z;

        if (Activate_MPC1 || Activate_MPC2 || Activate_MPC3)
        {
            // drawing the area around the moving car
            DrawArea(ant1_min_x, ant1_max_x, ant1_min_z, ant1_max_z);
        }


        ToRemove(temp_V7_list1, d1, ant1_min_x, ant1_max_x, ant1_min_z, ant1_max_z, out List<int> ToBeRemoved1);
        ToRemove(temp_V7_list2, d1, ant1_min_x, ant1_max_x, ant1_min_z, ant1_max_z, out List<int> ToBeRemoved2);
        ToRemove(temp_V7_list3, d1, ant1_min_x, ant1_max_x, ant1_min_z, ant1_max_z, out List<int> ToBeRemoved3);

        /* //this approach also slower than the standard one
         
        float startTime2 = Time.realtimeSinceStartup;
        ToRemoveParralel(temp_V7_list1, inarea_MPC1, d1, ant1_min_x, ant1_max_x, ant1_min_z, ant1_max_z, out List<int> ToBeRomeved);
        Debug.Log("Check 2: " + ((Time.realtimeSinceStartup - startTime2) * 1000000f) + " microsec");
        */

        if (ToBeRemoved1.Count > 0)
        {
            // Debug.Log("Something to delete");
            // remove the elements listed in ToBeRemoved list
            inarea_MPC1 = inarea_MPC1.Except(ToBeRemoved1).ToList();

            // update the seen MPCs list
            for (int i = 0; i < ToBeRemoved1.Count; i++)
            {
                temp_V7_list1.RemoveAll(elm => elm.Number == ToBeRemoved1[i]);
            }
            // the below function somewhy does not work for class V7
            // temp_V7_list1 = temp_V7_list1.Except(temp_remove_list).ToList();

            // drawing update
            if (Activate_MPC1)
            {
                // GameObjects can't be used in NativeArray and in all Jobs systems
                // NativeArray<GameObject> asd = new NativeArray<GameObject>(ToBeRemoved1.Count, Allocator.TempJob);
                // it won't work
                for (int i = 0; i < ToBeRemoved1.Count; i++)
                {

                    int temp_number = ToBeRemoved1[i];
                    string temp_name = "mpc1 #" + temp_number;
                    near_MPC1 = GameObject.Find(temp_name);
                    near_MPC1.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
            }
        }
        if (ToBeRemoved2.Count > 0)
        {
            // remove the elements listed in ToBeRemoved list
            inarea_MPC2 = inarea_MPC2.Except(ToBeRemoved2).ToList();

            // update the seen MPCs list
            for (int i = 0; i < ToBeRemoved2.Count; i++)
            {
                temp_V7_list2.RemoveAll(elm => elm.Number == ToBeRemoved2[i]);
            }

            // drawing update
            if (Activate_MPC2)
            {
                for (int i = 0; i < ToBeRemoved2.Count; i++)
                {
                    int temp_number = ToBeRemoved2[i];
                    string temp_name = "mpc2 #" + temp_number;
                    near_MPC2 = GameObject.Find(temp_name);
                    near_MPC2.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
            }
        }
        if (ToBeRemoved3.Count > 0)
        {
            // remove the elements listed in ToBeRemoved list
            inarea_MPC3 = inarea_MPC3.Except(ToBeRemoved3).ToList();

            // update the seen MPCs list
            for (int i = 0; i < ToBeRemoved3.Count; i++)
            {
                temp_V7_list3.RemoveAll(elm => elm.Number == ToBeRemoved3[i]);
            }

            // drawing update
            if (Activate_MPC3)
            {
                for (int i = 0; i < ToBeRemoved3.Count; i++)
                {
                    int temp_number = ToBeRemoved3[i];
                    string temp_name = "mpc3 #" + temp_number;
                    near_MPC3 = GameObject.Find(temp_name);
                    near_MPC3.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
            }
        }

        ToAdd(byX_V7MPC1, byZ_V7MPC1, d1, ant1_min_x, XMIN_index1, ant1_max_x, XMAX_index1, ant1_min_z, ZMIN_index1, ant1_max_z, ZMAX_index1, out List<int> ToBeAdd1);
        ToAdd(byX_V7MPC2, byZ_V7MPC2, d1, ant1_min_x, XMIN_index2, ant1_max_x, XMAX_index2, ant1_min_z, ZMIN_index2, ant1_max_z, ZMAX_index2, out List<int> ToBeAdd2);
        ToAdd(byX_V7MPC3, byZ_V7MPC3, d1, ant1_min_x, XMIN_index3, ant1_max_x, XMAX_index3, ant1_min_z, ZMIN_index3, ant1_max_z, ZMAX_index3, out List<int> ToBeAdd3);

        if (ToBeAdd1.Count > 0)
        {
            //Debug.Log("Something to add");
            // add the elements listed in ToBeAdd list
            var twolists = new List<int>(inarea_MPC1.Count + ToBeAdd1.Count);
            twolists.AddRange(inarea_MPC1);
            twolists.AddRange(ToBeAdd1);

            inarea_MPC1 = twolists;

            /*
            float startTime1 = Time.realtimeSinceStartup;
            
            // Update edges of the updated list InArea
            List<V7> test_V7_list1 = new List<V7>();
            for (int i = 0; i < inarea_MPC1.Count; i++)
            {
                V7 temp_V7 = new V7(MPC1[inarea_MPC1[i]], inarea_MPC1[i]);
                test_V7_list1.Add(temp_V7);
            }

            Debug.Log("Check 1: " + ((Time.realtimeSinceStartup - startTime1) * 1000000f) + "microsec, size of the list = " + test_V7_list1.Count);
            */

            // this approach tens of times quicker
            // float startTime2 = Time.realtimeSinceStartup;

            for (int i = 0; i < ToBeAdd1.Count; i++)
            {
                V7 temp_V7 = new V7(MPC1[ToBeAdd1[i]], ToBeAdd1[i]);
                temp_V7_list1.Add(temp_V7);
            }

            // Debug.Log("Check 2: " + ((Time.realtimeSinceStartup - startTime2) * 1000000f) + "microsec, size of the list = " + temp_V7_list1.Count);

            // finding edges of inarea_MPC1 in x and z directions
            List<V7> XsortV7_1 = temp_V7_list1.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
            List<V7> ZsortV7_1 = temp_V7_list1.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

            // finding elements indexes in the sorted lists
            XMIN_index1 = byX_V7MPC1.FindIndex(V7elm => V7elm.Number == XsortV7_1[0].Number);
            XMAX_index1 = byX_V7MPC1.FindIndex(V7elm => V7elm.Number == XsortV7_1[XsortV7_1.Count - 1].Number);

            ZMIN_index1 = byZ_V7MPC1.FindIndex(V7elm => V7elm.Number == ZsortV7_1[0].Number);
            ZMAX_index1 = byZ_V7MPC1.FindIndex(V7elm => V7elm.Number == ZsortV7_1[XsortV7_1.Count - 1].Number);

        }
        if (ToBeAdd2.Count > 0)
        {
            // add the elements listed in ToBeAdd list
            var twolists = new List<int>(inarea_MPC2.Count + ToBeAdd2.Count);
            twolists.AddRange(inarea_MPC2);
            twolists.AddRange(ToBeAdd2);

            inarea_MPC2 = twolists;

            // Update edges of the updated list InArea: See explanation about the complexity in MPC1
            for (int i = 0; i < ToBeAdd2.Count; i++)
            {
                V7 temp_V7 = new V7(MPC2[ToBeAdd2[i]], ToBeAdd2[i]);
                temp_V7_list2.Add(temp_V7);
            }

            // finding edges of inarea_MPC1 in x and z directions
            List<V7> XsortV7_2 = temp_V7_list2.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
            List<V7> ZsortV7_2 = temp_V7_list2.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

            // finding elements indexes in the sorted lists
            XMIN_index2 = byX_V7MPC2.FindIndex(V7elm => V7elm.Number == XsortV7_2[0].Number);
            XMAX_index2 = byX_V7MPC2.FindIndex(V7elm => V7elm.Number == XsortV7_2[XsortV7_2.Count - 1].Number);

            ZMIN_index2 = byZ_V7MPC2.FindIndex(V7elm => V7elm.Number == ZsortV7_2[0].Number);
            ZMAX_index2 = byZ_V7MPC2.FindIndex(V7elm => V7elm.Number == ZsortV7_2[XsortV7_2.Count - 1].Number);

        }
        if (ToBeAdd3.Count > 0)
        {
            // add the elements listed in ToBeAdd list
            var twolists = new List<int>(inarea_MPC3.Count + ToBeAdd3.Count);
            twolists.AddRange(inarea_MPC3);
            twolists.AddRange(ToBeAdd3);

            inarea_MPC3 = twolists;

            // Update edges of the updated list InArea: See explanation about the complexity in MPC1
            for (int i = 0; i < ToBeAdd3.Count; i++)
            {
                V7 temp_V7 = new V7(MPC3[ToBeAdd3[i]], ToBeAdd3[i]);
                temp_V7_list3.Add(temp_V7);
            }
            // finding edges of inarea_MPC1 in x and z directions
            List<V7> XsortV7_3 = temp_V7_list3.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
            List<V7> ZsortV7_3 = temp_V7_list3.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

            // finding elements indexes in the sorted lists
            XMIN_index3 = byX_V7MPC3.FindIndex(V7elm => V7elm.Number == XsortV7_3[0].Number);
            XMAX_index3 = byX_V7MPC3.FindIndex(V7elm => V7elm.Number == XsortV7_3[XsortV7_3.Count - 1].Number);

            ZMIN_index3 = byZ_V7MPC3.FindIndex(V7elm => V7elm.Number == ZsortV7_3[0].Number);
            ZMAX_index3 = byZ_V7MPC3.FindIndex(V7elm => V7elm.Number == ZsortV7_3[XsortV7_3.Count - 1].Number);

        }

        float startTime1 = Time.realtimeSinceStartup;


        seen_MPC1 = new List<int>();
        for (int i = 0; i < inarea_MPC1.Count; i++)
        {

            Vector3 temp_direction = MPC1[inarea_MPC1[i]].Coordinates - transform.position;
            if (Vector3.Dot(MPC1[inarea_MPC1[i]].Normal, temp_direction) < 0)
            {
                if (!Physics.Linecast(transform.position, MPC1[inarea_MPC1[i]].Coordinates))
                {
                    seen_MPC1.Add(inarea_MPC1[i]);
                    if (Activate_MPC1)
                    {
                        int temp_number = inarea_MPC1[i];
                        string temp_name = "mpc1 #" + temp_number;
                        near_MPC1 = GameObject.Find(temp_name);
                        near_MPC1.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    }
                }
            }
        }

        seen_MPC2 = new List<int>();
        for (int i = 0; i < inarea_MPC2.Count; i++)
        {

            Vector3 temp_direction = MPC2[inarea_MPC2[i]].Coordinates - transform.position;
            if (Vector3.Dot(MPC2[inarea_MPC2[i]].Normal, temp_direction) < 0)
            {
                if (!Physics.Linecast(transform.position, MPC2[inarea_MPC2[i]].Coordinates))
                {
                    seen_MPC2.Add(inarea_MPC2[i]);
                    if (Activate_MPC2)
                    {
                        int temp_number = inarea_MPC2[i];
                        string temp_name = "mpc2 #" + temp_number;
                        near_MPC2 = GameObject.Find(temp_name);
                        near_MPC2.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    }
                }
            }
        }

        seen_MPC3 = new List<int>();
        for (int i = 0; i < inarea_MPC3.Count; i++)
        {

            Vector3 temp_direction = MPC3[inarea_MPC3[i]].Coordinates - transform.position;
            if (Vector3.Dot(MPC3[inarea_MPC3[i]].Normal, temp_direction) < 0)
            {
                if (!Physics.Linecast(transform.position, MPC3[inarea_MPC3[i]].Coordinates))
                {
                    seen_MPC3.Add(inarea_MPC3[i]);
                    if (Activate_MPC3)
                    {
                        int temp_number = inarea_MPC3[i];
                        string temp_name = "mpc3 #" + temp_number;
                        near_MPC3 = GameObject.Find(temp_name);
                        near_MPC3.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    }
                }
            }
        }

        //float time1 = (Time.realtimeSinceStartup - startTime1) *1000000f;
        Debug.Log("Check 1: " + ((Time.realtimeSinceStartup - startTime1) * 1000000f) + " microsec");
        /*
        var threelists = new List<V7>(temp_V7_list1.Count + temp_V7_list2.Count + temp_V7_list3.Count);
        threelists.AddRange(temp_V7_list1);
        threelists.AddRange(temp_V7_list2);
        threelists.AddRange(temp_V7_list3);

        float startTime2 = Time.realtimeSinceStartup;
        //Seen_MPC_Parallel(List < V7 > InAreaMPC, Vector3 position, out List<int> SeenMPCID)

        Seen_MPC_Parallel(threelists, transform.position, out List<int> SeenMPCID);
        //Seen_MPC_Parallel(temp_V7_list1, transform.position, out List<int> SeenMPCID1);
        //Seen_MPC_Parallel(temp_V7_list2, transform.position, out List<int> SeenMPCID2);
        //Seen_MPC_Parallel(temp_V7_list3, transform.position, out List<int> SeenMPCID3);

        //float time2 = (Time.realtimeSinceStartup - startTime2) * 1000000f;
        Debug.Log("Check 2: " + ((Time.realtimeSinceStartup - startTime2) * 1000000f) + " microsec");
        */
    }






    /*
    [BurstCompile]
    void Seen_MPC_Parallel(List<V7> InAreaMPC, Vector3 position, out List<int> SeenMPCID)
    {
        SeenMPCID = new List<int>();

        var results = new NativeArray<RaycastHit>(InAreaMPC.Count, Allocator.TempJob);
        var commands = new NativeArray<RaycastCommand>(InAreaMPC.Count, Allocator.TempJob);

        for (int i = 0; i < InAreaMPC.Count; i++)
        {
            Vector3 direction = InAreaMPC[i].CoordNorm.Coordinates - position;
            float distance = direction.magnitude;
            // RaycastCommand(Vector3 from, Vector3 direction, float distance = float.MaxValue, int layerMask = -5, int maxHits = 1);
            commands[i] = new RaycastCommand(position, direction/distance, distance, 1);
        }
        int minCommandsPerJob = 5;
        var handle = RaycastCommand.ScheduleBatch(commands, results, minCommandsPerJob);

        handle.Complete();
        commands.Dispose();

        for (int i = 0; i < InAreaMPC.Count; i++)
        {
            if (results[i].collider == null)
            {
                SeenMPCID.Add(InAreaMPC[i].Number);
            }
        }
        
        results.Dispose();
    }

    




    void ToRemoveParralel(List<V7> InAraeMPCList, List<int> InAreaList, Vector3 MOVE, float MINX, float MAXX, float MINZ, float MAXZ, out List<int> ToBeRomeved)
    {
        // moving lists to native arrays
        // NativeBitArray ToBeRomevedParallelBits = new NativeBitArray(MPCList.Count, Allocator.TempJob);
        // may not work as I expect
        NativeArray<int> ToBeRomevedParallel = new NativeArray<int>(InAraeMPCList.Count, Allocator.TempJob);
        NativeArray<float3> MPCListParallel = new NativeArray<float3>(InAreaList.Count, Allocator.TempJob);
        //NativeArray<int> InAreaListParallel = new NativeArray<int>(InAreaList.Count, Allocator.TempJob);

        for (int i = 0; i < InAreaList.Count; i++)
        {
            ToBeRomevedParallel[i] = 0;
            MPCListParallel[i] = InAraeMPCList[i].CoordNorm.Coordinates;
            //InAreaListParallel[i] = InAreaList[i];
        }


        // create job instance
        ToBeRemovedParallelExecution toBeRemovedParallelExecution = new ToBeRemovedParallelExecution
        {
            //InIndexArray = InAreaListParallel,
            InMPCArray = MPCListParallel,
            Output = ToBeRomevedParallel,
            min_x = MINX,
            max_x = MAXX,
            min_z = MINZ,
            max_z = MAXZ,
            move = MOVE
        };

        // job scheduling
        JobHandle jobHandle = toBeRemovedParallelExecution.Schedule(InAreaList.Count, 20);
        jobHandle.Complete();// tell job to complete

        ToBeRomeved = new List<int>();
        for (int i = 0; i < ToBeRomevedParallel.Length; i++)
        {
            if (ToBeRomevedParallel[i] == 1)
            {
                ToBeRomeved.Add(InAreaList[i]);
            }
        }

        ToBeRomevedParallel.Dispose();
        MPCListParallel.Dispose();
    }
        
    [BurstCompile]
    public struct ToBeRemovedParallelExecution : IJobParallelFor
    {
        // parallel run performed only above this array
        // [ReadOnly] public NativeArray<int> InIndexArray;
        // hz
        [ReadOnly] public NativeArray<float3> InMPCArray;
        // this is an array where boolean yes are written
        [WriteOnly] public NativeArray<int> Output;

        [ReadOnly] public float min_x, max_x, min_z, max_z;
        [ReadOnly] public float3 move;

        public void Execute(int index)
        {
            if (move.x >= 0)
            {
                if (move.z >= 0)
                {
                    if ((InMPCArray[index].x < min_x) || (InMPCArray[index].z < min_z))
                    {
                        Output[index] = 1;
                    }
                }
                else
                {
                    if ((InMPCArray[index].x < min_x) || (InMPCArray[index].z > max_z))
                    {
                        Output[index] = 1;
                    }
                }
            }
            else
            {
                if (move.z >= 0)
                {
                    if ((InMPCArray[index].x > max_x) || (InMPCArray[index].z < min_z))
                    {
                        Output[index] = 1;
                    }
                }
                else
                {
                    if ((InMPCArray[index].x > max_x) || (InMPCArray[index].z > max_z))
                    {
                        Output[index] = 1;
                    }
                }
            }
        }
    }

    */


































    void ToAdd(List<V7> XSort, List<V7> ZSort, Vector3 move, float min_x, int XMIN_index, float max_x, int XMAX_index, float min_z, int ZMIN_index, float max_z, int ZMAX_index, out List<int> ToBeAdd)
    {
        ToBeAdd = new List<int>();
        if (move.x >= 0)
        {
            if (move.z >= 0)
            {
                if (XMAX_index != XSort.Count - 1)
                {
                    int kx = 1;
                    while (XMAX_index + kx < XSort.Count && XSort[XMAX_index + kx].CoordNorm.Coordinates.x < max_x)
                    {
                        if ((XSort[XMAX_index + kx].CoordNorm.Coordinates.z > min_z) && (XSort[XMAX_index + kx].CoordNorm.Coordinates.z < max_z))
                        { ToBeAdd.Add(XSort[XMAX_index + kx].Number); }
                        kx += 1;
                    }
                }
                if (ZMAX_index != ZSort.Count - 1)
                {
                    int kz = 1;
                    while (ZMAX_index + kz < ZSort.Count && ZSort[ZMAX_index + kz].CoordNorm.Coordinates.z < max_z)
                    {
                        if ((ZSort[ZMAX_index + kz].CoordNorm.Coordinates.x > min_x) && (ZSort[ZMAX_index + kz].CoordNorm.Coordinates.x < XSort[XMAX_index].CoordNorm.Coordinates.x))
                        { ToBeAdd.Add(ZSort[ZMAX_index + kz].Number); }
                        kz += 1;
                    }
                }
            }
            else
            {
                if (XMAX_index != XSort.Count - 1)
                {
                    int kx = 1;
                    while (XMAX_index + kx < XSort.Count && XSort[XMAX_index + kx].CoordNorm.Coordinates.x < max_x)
                    {
                        if ((XSort[XMAX_index + kx].CoordNorm.Coordinates.z > min_z) && (XSort[XMAX_index + kx].CoordNorm.Coordinates.z < max_z))
                        { ToBeAdd.Add(XSort[XMAX_index + kx].Number); }
                        kx += 1;
                    }
                }
                if (ZMIN_index != 0)
                {
                    int kz = 1;
                    while (ZMIN_index - kz > -1 && ZSort[ZMIN_index - kz].CoordNorm.Coordinates.z > min_z)
                    {
                        if ((ZSort[ZMIN_index - kz].CoordNorm.Coordinates.x > min_x) && (ZSort[ZMIN_index - kz].CoordNorm.Coordinates.x < XSort[XMAX_index].CoordNorm.Coordinates.x))
                        { ToBeAdd.Add(ZSort[ZMIN_index - kz].Number); }
                        kz += 1;
                    }
                }
            }
        }
        else
        {
            if (move.z >= 0)
            {
                if (XMIN_index != 0)
                {
                    int kx = 1;
                    while (XMIN_index - kx > -1 && XSort[XMIN_index - kx].CoordNorm.Coordinates.x > min_x)
                    {
                        if ((XSort[XMIN_index - kx].CoordNorm.Coordinates.z > min_z) && (XSort[XMIN_index - kx].CoordNorm.Coordinates.z < max_z))
                        { ToBeAdd.Add(XSort[XMIN_index - kx].Number); }
                        kx += 1;
                    }
                }
                if (ZMAX_index != ZSort.Count - 1)
                {
                    int kz = 1;
                    while (ZMAX_index + kz < ZSort.Count && ZSort[ZMAX_index + kz].CoordNorm.Coordinates.z < max_z)
                    {
                        if ((ZSort[ZMAX_index + kz].CoordNorm.Coordinates.x > XSort[XMIN_index].CoordNorm.Coordinates.x) && (ZSort[ZMAX_index + kz].CoordNorm.Coordinates.x < max_x))
                        { ToBeAdd.Add(ZSort[ZMAX_index + kz].Number); }
                        kz += 1;
                    }
                }
            }
            else
            {
                if (XMIN_index != 0)
                {
                    int kx = 1;
                    while (XMIN_index - kx > -1 && XSort[XMIN_index - kx].CoordNorm.Coordinates.x > min_x)
                    {
                        if ((XSort[XMIN_index - kx].CoordNorm.Coordinates.z > min_z) && (XSort[XMIN_index - kx].CoordNorm.Coordinates.z < max_z))
                        { ToBeAdd.Add(XSort[XMIN_index - kx].Number); }
                        kx += 1;
                    }
                }
                if (ZMIN_index != 0)
                {
                    int kz = 1;
                    while (ZMIN_index - kz > -1 && ZSort[ZMIN_index - kz].CoordNorm.Coordinates.z > min_z)
                    {
                        if ((ZSort[ZMIN_index - kz].CoordNorm.Coordinates.x > XSort[XMIN_index].CoordNorm.Coordinates.x) && (ZSort[ZMIN_index - kz].CoordNorm.Coordinates.x < max_x))
                        { ToBeAdd.Add(ZSort[ZMIN_index - kz].Number); }
                        kz += 1;
                    }
                }
            }
        }
    }




    void ToRemove(List<V7> MPCList, Vector3 move, float min_x, float max_x, float min_z, float max_z, out List<int> ToBeRomeved)
    {
        ToBeRomeved = new List<int>();
        if (move.x >= 0)
        {
            if (move.z >= 0)
            {
                for (int i = 0; i < MPCList.Count; i++)
                {
                    if ((MPCList[i].CoordNorm.Coordinates.x < min_x) || (MPCList[i].CoordNorm.Coordinates.z < min_z))
                    {
                        ToBeRomeved.Add(MPCList[i].Number);
                    }
                }
            }
            else
            {
                for (int i = 0; i < MPCList.Count; i++)
                {
                    if ((MPCList[i].CoordNorm.Coordinates.x < min_x) || (MPCList[i].CoordNorm.Coordinates.z > max_z))
                    {
                        ToBeRomeved.Add(MPCList[i].Number);
                    }
                }
            }
        }
        else
        {
            if (move.z >= 0)
            {
                for (int i = 0; i < MPCList.Count; i++)
                {
                    if ((MPCList[i].CoordNorm.Coordinates.x > max_x) || (MPCList[i].CoordNorm.Coordinates.z < min_z))
                    {
                        ToBeRomeved.Add(MPCList[i].Number);
                    }
                }
            }
            else
            {
                for (int i = 0; i < MPCList.Count; i++)
                {
                    if ((MPCList[i].CoordNorm.Coordinates.x > max_x) || (MPCList[i].CoordNorm.Coordinates.z > max_z))
                    {
                        ToBeRomeved.Add(MPCList[i].Number);
                    }
                }
            }
        }
    }


    void DrawArea(float minx, float maxx, float minz, float maxz)
    {
        Vector3 v1 = new Vector3(minx, 0, minz);
        Vector3 v2 = new Vector3(minx, 0, maxz);
        Vector3 v3 = new Vector3(maxx, 0, maxz);
        Vector3 v4 = new Vector3(maxx, 0, minz);
        Debug.DrawLine(v1, v2, Color.cyan);
        Debug.DrawLine(v2, v3, Color.cyan);
        Debug.DrawLine(v3, v4, Color.cyan);
        Debug.DrawLine(v4, v1, Color.cyan);
    }
}
