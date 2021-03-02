using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

public class Transceiver_Channel : MonoBehaviour
{
    public int ReachableDistance;
    // defining moved distance
    Vector3 d1; // for antenna #1
    Vector3 fwd; // vector represents the forward direction
    Vector3 up; // vector represents the up direction

    // defining initial position of antennas and the area of search
    Vector3 old_position1; // for antenna #1

    // for MPC1 and DMCs
    float ant1_min_x1; 
    float ant1_max_x1;
    float ant1_min_z1;
    float ant1_max_z1;
    // for MPC2
    float ant1_min_x2;
    float ant1_max_x2;
    float ant1_min_z2;
    float ant1_max_z2;
    // for MPC3
    float ant1_min_x3;
    float ant1_max_x3;
    float ant1_min_z3;
    float ant1_max_z3;

    private int XMIN_index0;
    private int XMAX_index0;
    private int ZMIN_index0;
    private int ZMAX_index0;

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

    public List<int> seen_DMC = new List<int>();
    public List<float> seen_DMC_att = new List<float>();
    public List<int> seen_MPC1 = new List<int>();
    public List<float> seen_MPC1_att = new List<float>();
    public List<int> seen_MPC2 = new List<int>();
    public List<float> seen_MPC2_att = new List<float>();
    public List<int> seen_MPC3 = new List<int>();
    public List<float> seen_MPC3_att = new List<float>();

    //public bool Activate_MPCs;
    public bool Activate_MPC1;
    public bool Activate_MPC2;
    public bool Activate_MPC3;

    public bool Antenna_pattern;

    ///////////////////////////////////////////////////////////////////////////////////
    /// Start reading data from other scripts
    ///////////////////////////////////////////////////////////////////////////////////

    List<V6> DMC;
    List<V7> V7DMC;
    List<int> inarea_DMC = new List<int>();

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

    // sorted lists by x and z: DMC
    List<V7> byX_V7DMC;
    List<V7> byZ_V7DMC;
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
    List<V7> temp_V7_list0 = new List<V7>(); // this list will be used for sorting
    List<V7> temp_V7_list1 = new List<V7>(); // this list will be used for sorting
    List<V7> temp_V7_list2 = new List<V7>(); // this list will be used for sorting
    List<V7> temp_V7_list3 = new List<V7>(); // this list will be used for sorting

    // Fixed update count
    int fixed_update_count = 0;

    // Start is called before the first frame update
    void Start()
    {
        
        old_position1 = transform.position; // antenna #1 position in the beginning
        // in x axis
        // for MPC1 and DMC
        ant1_min_x1 = old_position1.x - ReachableDistance;
        ant1_max_x1 = old_position1.x + ReachableDistance;
        // for MPC2
        ant1_min_x2 = old_position1.x - ReachableDistance / 2;
        ant1_max_x2 = old_position1.x + ReachableDistance / 2;
        // for MPC3
        ant1_min_x3 = old_position1.x - ReachableDistance / 3;
        ant1_max_x3 = old_position1.x + ReachableDistance / 3;

        // in z axis
        // for MPC1 and DMC
        ant1_min_z1 = old_position1.z - ReachableDistance;
        ant1_max_z1 = old_position1.z + ReachableDistance;
        // for MPC2
        ant1_min_z2 = old_position1.z - ReachableDistance / 2;
        ant1_max_z2 = old_position1.z + ReachableDistance / 2;
        // for MPC3
        ant1_min_z3 = old_position1.z - ReachableDistance / 3;
        ant1_max_z3 = old_position1.z + ReachableDistance / 3;

        ///////////////////////////////////////////////////////////////////////////////////
        /// Start reading data from other scripts
        ///////////////////////////////////////////////////////////////////////////////////
        // reading generated MPCs from the mpc spawner script
        GameObject MPC_Spawner = GameObject.Find("Direct_Solution");
        Correcting_polygons MPC_Script = MPC_Spawner.GetComponent<Correcting_polygons>();
        DMC = MPC_Script.SeenV6_DMC;
        V7DMC = MPC_Script.SeenV7_DMC;
        MPC1 = MPC_Script.SeenV6_MPC1;
        V7MPC1 = MPC_Script.SeenV7_MPC1;
        MPC2 = MPC_Script.SeenV6_MPC2;
        V7MPC2 = MPC_Script.SeenV7_MPC2;
        MPC3 = MPC_Script.SeenV6_MPC3;
        V7MPC3 = MPC_Script.SeenV7_MPC3;


        // sorted lists by x and z: DMC
        byX_V7DMC = V7DMC.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        byZ_V7DMC = V7DMC.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();
        // sorted lists by x and z: MPC1
        byX_V7MPC1 = V7MPC1.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        byZ_V7MPC1 = V7MPC1.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();
        // sorted lists by x and z: MPC1
        byX_V7MPC2 = V7MPC2.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        byZ_V7MPC2 = V7MPC2.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();
        // sorted lists by x and z: MPC1
        //float startTime_org = Time.realtimeSinceStartup;
        byX_V7MPC3 = V7MPC3.OrderBy(ASD => ASD.CoordNorm.Coordinates.x).ToList();
        //Debug.Log("Check Time Org Sort: " + ((Time.realtimeSinceStartup - startTime_org) * 1000000f) + " microsec");
        byZ_V7MPC3 = V7MPC3.OrderBy(QWE => QWE.CoordNorm.Coordinates.z).ToList();


        /*
        byX_V7MPC3 = V7MPC3;//.OrderBy(ASD => ASD.CoordNorm.Coordinates.x).ToList();
        SortV7byX sortV7ByX = new SortV7byX();
        byX_V7MPC3.Sort(sortV7ByX);
        
        float startTime_upd = Time.realtimeSinceStartup;
        byZ_V7MPC3 = V7MPC3;//.OrderBy(QWE => QWE.CoordNorm.Coordinates.z).ToList();
        SortV7byZ sortV7ByZ = new SortV7byZ();
        byZ_V7MPC3.Sort(sortV7ByZ);
        Debug.Log("Check Time Upd Sort: " + ((Time.realtimeSinceStartup - startTime_upd) * 1000000f) + " microsec");
        */

        ///////////////////////////////////////////////////////////////////////////////////
        /// Finish reading data from other scripts
        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////
        // search for seen mpcs within the search area
        ///////////////////////////////////////////////////////////////////////////////////

        ///////////////////////////////////////////////////////////////////////////////////
        /// DMC

        ///////////////////////////////////////////////////////////////////////////////////
        //Debug.Log("Check0");

        for (int i = 0; i < DMC.Count; i++)
        {
            if ((DMC[i].Coordinates.x > ant1_min_x1) && (DMC[i].Coordinates.x < ant1_max_x1) && (DMC[i].Coordinates.z > ant1_min_z1) && (DMC[i].Coordinates.z < ant1_max_z1))
            {
                //inarea_MPC1.Add(i);
                V7 temp_V7 = new V7(DMC[i], i);
                temp_V7_list0.Add(temp_V7);
                inarea_DMC.Add(i);
            }
        }

        // finding edges of inarea_MPC1 in x and z directions
        List<V7> XsortV7_0 = temp_V7_list0.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
        List<V7> ZsortV7_0 = temp_V7_list0.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

        // finding elements indexes in the sorted lists
        // for MPC1
        XMIN_index0 = byX_V7DMC.FindIndex(V7elm => V7elm.Number == XsortV7_0[0].Number);
        XMAX_index0 = byX_V7DMC.FindIndex(V7elm => V7elm.Number == XsortV7_0[XsortV7_0.Count - 1].Number);
        ZMIN_index0 = byZ_V7DMC.FindIndex(V7elm => V7elm.Number == ZsortV7_0[0].Number);
        ZMAX_index0 = byZ_V7DMC.FindIndex(V7elm => V7elm.Number == ZsortV7_0[XsortV7_0.Count - 1].Number);


        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC1

        ///////////////////////////////////////////////////////////////////////////////////
        //Debug.Log("Check0");

        for (int i = 0; i < MPC1.Count; i++)
        {
            if ((MPC1[i].Coordinates.x > ant1_min_x1) && (MPC1[i].Coordinates.x < ant1_max_x1) && (MPC1[i].Coordinates.z > ant1_min_z1) && (MPC1[i].Coordinates.z < ant1_max_z1))
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
            if ((MPC2[i].Coordinates.x > ant1_min_x2) && (MPC2[i].Coordinates.x < ant1_max_x2) && (MPC2[i].Coordinates.z > ant1_min_z2) && (MPC2[i].Coordinates.z < ant1_max_z2))
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
            if ((MPC3[i].Coordinates.x > ant1_min_x3) && (MPC3[i].Coordinates.x < ant1_max_x3) && (MPC3[i].Coordinates.z > ant1_min_z3) && (MPC3[i].Coordinates.z < ant1_max_z3))
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
    private void FixedUpdate()
    {
        fixed_update_count += 1;
        d1 = transform.position - old_position1;

        fwd = transform.forward;
        up = transform.up;

        //Debug.DrawRay(transform.position, fwd, Color.red);
        //Debug.DrawRay(transform.position, up, Color.yellow);

        // update the old position. Now, current position become old for the next position.
        old_position1 = transform.position;
        // in x axis
        // for MPC1
        ant1_min_x1 += d1.x;
        ant1_max_x1 += d1.x;
        // for MPC2
        ant1_min_x2 += d1.x;
        ant1_max_x2 += d1.x;
        // for MPC3
        ant1_min_x3 += d1.x;
        ant1_max_x3 += d1.x;
        
        // in z axis
        // for MPC1
        ant1_min_z1 += d1.z;
        ant1_max_z1 += d1.z;
        // for MPC2
        ant1_min_z2 += d1.z;
        ant1_max_z2 += d1.z;
        // for MPC3
        ant1_min_z3 += d1.z;
        ant1_max_z3 += d1.z;

        if (Activate_MPC1 || Activate_MPC2 || Activate_MPC3)
        {
            // drawing the area around the moving car
            DrawArea(ant1_min_x1, ant1_max_x1, ant1_min_z1, ant1_max_z1);
        }

        ToRemove(temp_V7_list0, d1, ant1_min_x1, ant1_max_x1, ant1_min_z1, ant1_max_z1, out List<int> ToBeRemoved0);

        ToRemove(temp_V7_list1, d1, ant1_min_x1, ant1_max_x1, ant1_min_z1, ant1_max_z1, out List<int> ToBeRemoved1);
        ToRemove(temp_V7_list2, d1, ant1_min_x2, ant1_max_x2, ant1_min_z2, ant1_max_z2, out List<int> ToBeRemoved2);
        ToRemove(temp_V7_list3, d1, ant1_min_x3, ant1_max_x3, ant1_min_z3, ant1_max_z3, out List<int> ToBeRemoved3);

        if (ToBeRemoved0.Count > 0) // DMC
        {
            // remove the elements listed in ToBeRemoved list
            inarea_DMC = inarea_DMC.Except(ToBeRemoved0).ToList();

            // update the seen MPCs list
            for (int i = 0; i < ToBeRemoved0.Count; i++)
            {
                temp_V7_list0.RemoveAll(elm => elm.Number == ToBeRemoved0[i]);
            }
            
        }

        if (ToBeRemoved1.Count > 0)
        {
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

        ToAdd(byX_V7DMC,  byZ_V7DMC,  d1, ant1_min_x1, XMIN_index0, ant1_max_x1, XMAX_index0, ant1_min_z1, ZMIN_index0, ant1_max_z1, ZMAX_index0, out List<int> ToBeAdd0);
        
        ToAdd(byX_V7MPC1, byZ_V7MPC1, d1, ant1_min_x1, XMIN_index1, ant1_max_x1, XMAX_index1, ant1_min_z1, ZMIN_index1, ant1_max_z1, ZMAX_index1, out List<int> ToBeAdd1);
        ToAdd(byX_V7MPC2, byZ_V7MPC2, d1, ant1_min_x2, XMIN_index2, ant1_max_x2, XMAX_index2, ant1_min_z2, ZMIN_index2, ant1_max_z2, ZMAX_index2, out List<int> ToBeAdd2);
        ToAdd(byX_V7MPC3, byZ_V7MPC3, d1, ant1_min_x3, XMIN_index3, ant1_max_x3, XMAX_index3, ant1_min_z3, ZMIN_index3, ant1_max_z3, ZMAX_index3, out List<int> ToBeAdd3);


        if (ToBeAdd0.Count > 0)
        {
            // add the elements listed in ToBeAdd list
            var twolists = new List<int>(inarea_DMC.Count + ToBeAdd0.Count);
            twolists.AddRange(inarea_DMC);
            twolists.AddRange(ToBeAdd0);

            inarea_DMC = twolists;

            for (int i = 0; i < ToBeAdd0.Count; i++)
            {
                V7 temp_V7 = new V7(DMC[ToBeAdd0[i]], ToBeAdd0[i]);
                temp_V7_list0.Add(temp_V7);
            }

            // finding edges of inarea_MPC1 in x and z directions
            List<V7> XsortV7_0 = temp_V7_list0.OrderBy(Elm => Elm.CoordNorm.Coordinates.x).ToList();
            List<V7> ZsortV7_0 = temp_V7_list0.OrderBy(Elm => Elm.CoordNorm.Coordinates.z).ToList();

            // finding elements indexes in the sorted lists
            XMIN_index0 = byX_V7DMC.FindIndex(V7elm => V7elm.Number == XsortV7_0[0].Number);
            XMAX_index0 = byX_V7DMC.FindIndex(V7elm => V7elm.Number == XsortV7_0[XsortV7_0.Count - 1].Number);

            ZMIN_index0 = byZ_V7DMC.FindIndex(V7elm => V7elm.Number == ZsortV7_0[0].Number);
            ZMAX_index0 = byZ_V7DMC.FindIndex(V7elm => V7elm.Number == ZsortV7_0[XsortV7_0.Count - 1].Number);

        }

        if (ToBeAdd1.Count > 0)
        {
            // add the elements listed in ToBeAdd list
            var twolists = new List<int>(inarea_MPC1.Count + ToBeAdd1.Count);
            twolists.AddRange(inarea_MPC1);
            twolists.AddRange(ToBeAdd1);

            inarea_MPC1 = twolists;

            for (int i = 0; i < ToBeAdd1.Count; i++)
            {
                V7 temp_V7 = new V7(MPC1[ToBeAdd1[i]], ToBeAdd1[i]);
                temp_V7_list1.Add(temp_V7);
            }
            
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
                //Debug.Log("Fixed update step = " + fixed_update_count + "; ToBeAdd3[" + i + "] = " + ToBeAdd3[i] + "; byX_V7MPC3 count = " + byX_V7MPC3.Count + "; byZ_V7MPC3 = " + byX_V7MPC3.Count);
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

        //float startTime_org = Time.realtimeSinceStartup;
        seen_MPC1 = new List<int>();
        for (int i = 0; i < inarea_MPC1.Count; i++)
        {

            Vector3 temp_direction = MPC1[inarea_MPC1[i]].Coordinates - transform.position;
            if (Vector3.Dot(MPC1[inarea_MPC1[i]].Normal, temp_direction) < 0)
            {
                if (!Physics.Linecast(transform.position, MPC1[inarea_MPC1[i]].Coordinates))
                {
                    seen_MPC1.Add(inarea_MPC1[i]);

                    // Adding antenna radiation pattern
                    if (Antenna_pattern)
                    {
                        float cos_az = Vector3.Dot(temp_direction.normalized, fwd);
                        float cos_el = Vector3.Dot(temp_direction.normalized, up);
                        
                        
                        float horizont_att = 0.5f - 0.5f * cos_az;

                        float vertical_att;
                        if (cos_el <= 0) { vertical_att = cos_el + 1; }
                        else { vertical_att = 1 - cos_el; }

                        //Debug.Log(vertical_att * horizont_att);
                        seen_MPC1_att.Add(vertical_att * horizont_att);
                    }
                    else
                    { seen_MPC1_att.Add(1f); }

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
                    if (Antenna_pattern)
                    {
                        float cos_az = Vector3.Dot(temp_direction.normalized, fwd);
                        float cos_el = Vector3.Dot(temp_direction.normalized, up);


                        float horizont_att = 0.5f - 0.5f * cos_az;

                        float vertical_att;
                        if (cos_el <= 0) { vertical_att = cos_el + 1; }
                        else { vertical_att = 1 - cos_el; }

                        //Debug.Log(vertical_att * horizont_att);
                        seen_MPC2_att.Add(vertical_att * horizont_att);
                    }
                    else
                    { seen_MPC2_att.Add(1f); }

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

                    if (Antenna_pattern)
                    {
                        float cos_az = Vector3.Dot(temp_direction.normalized, fwd);
                        float cos_el = Vector3.Dot(temp_direction.normalized, up);


                        float horizont_att = 0.5f - 0.5f * cos_az;

                        float vertical_att;
                        if (cos_el <= 0) { vertical_att = cos_el + 1; }
                        else { vertical_att = 1 - cos_el; }

                        //Debug.Log(vertical_att * horizont_att);
                        seen_MPC3_att.Add(vertical_att * horizont_att);
                    }
                    else
                    { seen_MPC3_att.Add(1f); }

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

        //float startTime_org = Time.realtimeSinceStartup;
        seen_DMC = new List<int>();
        for (int i = 0; i < inarea_DMC.Count; i++)
        {

            Vector3 temp_direction = DMC[inarea_DMC[i]].Coordinates - transform.position;
            if (Vector3.Dot(DMC[inarea_DMC[i]].Normal, temp_direction) < 0)
            {
                if (!Physics.Linecast(transform.position, DMC[inarea_DMC[i]].Coordinates))
                {
                    seen_DMC.Add(inarea_DMC[i]);

                    // Adding antenna radiation pattern
                    if (Antenna_pattern)
                    {
                        float cos_az = Vector3.Dot(temp_direction.normalized, fwd);
                        float cos_el = Vector3.Dot(temp_direction.normalized, up);


                        float horizont_att = 0.5f - 0.5f * cos_az;

                        float vertical_att;
                        if (cos_el <= 0) { vertical_att = cos_el + 1; }
                        else { vertical_att = 1 - cos_el; }

                        //Debug.Log(vertical_att * horizont_att);
                        seen_DMC_att.Add(vertical_att * horizont_att);
                    }
                    else
                    { seen_DMC_att.Add(1f); }

                }
            }
        }

        //Debug.Log("Check Time Org: " + ((Time.realtimeSinceStartup - startTime_org) * 1000000f) + " microsec");

        /*
        float startTime_upd = Time.realtimeSinceStartup;
        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(inarea_DMC.Count, Allocator.TempJob);//(inarea_MPC1.Count + inarea_MPC2.Count + inarea_MPC3.Count, Allocator.TempJob);
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(inarea_DMC.Count, Allocator.TempJob);//(inarea_MPC1.Count + inarea_MPC2.Count + inarea_MPC3.Count, Allocator.TempJob);
        var seen_DMC_upd = new List<int>();
        //var seen_MPC1_upd = new List<int>();
        //var seen_MPC2_upd = new List<int>();
        //var seen_MPC3_upd = new List<int>();
        
        for (int i = 0; i < inarea_DMC.Count; i++)
        {
            Vector3 temp_direction = DMC[inarea_DMC[i]].Coordinates - transform.position;
            commands[i] = new RaycastCommand(transform.position, temp_direction.normalized, temp_direction.magnitude);
        }
        
        JobHandle RayCast_handle = RaycastCommand.ScheduleBatch(commands, results, 5, default);
        RayCast_handle.Complete();
        for (int i = 0; i < inarea_DMC.Count; i++)
        {
            if (results[i].collider == null)
            {
                seen_DMC_upd.Add(inarea_DMC[i]);
            }
        }
        ////// comment here
        for (int i = 0; i < inarea_MPC1.Count; i++)
        {
            Vector3 temp_direction = MPC1[inarea_MPC1[i]].Coordinates - transform.position;
            commands[i] = new RaycastCommand(transform.position, temp_direction.normalized, temp_direction.magnitude);
        }
        for (int i = 0; i < inarea_MPC2.Count; i++)
        {
            Vector3 temp_direction = MPC2[inarea_MPC2[i]].Coordinates - transform.position;
            commands[inarea_MPC1.Count + i] = new RaycastCommand(transform.position, temp_direction.normalized, temp_direction.magnitude);
        }
        for (int i = 0; i < inarea_MPC3.Count; i++)
        {
            Vector3 temp_direction = MPC3[inarea_MPC3[i]].Coordinates - transform.position;
            commands[inarea_MPC1.Count + inarea_MPC2.Count + i] = new RaycastCommand(transform.position, temp_direction.normalized, temp_direction.magnitude);
        }
        
        for (int i = 0; i < inarea_MPC1.Count; i++)
        {
            if (results[i].collider == null)
            {
                seen_MPC1_upd.Add(inarea_MPC1[i]);
            }
        }
        for (int i = inarea_MPC1.Count; i < inarea_MPC1.Count + inarea_MPC2.Count; i++)
        {
            if (results[i].collider == null)
            {
                seen_MPC2_upd.Add(inarea_MPC2[i - inarea_MPC1.Count]);
            }
        }
        for (int i = inarea_MPC1.Count + inarea_MPC2.Count; i < results.Length; i++)
        {
            if (results[i].collider == null)
            {
                seen_MPC3_upd.Add(inarea_MPC3[i - inarea_MPC1.Count - inarea_MPC2.Count]);
            }
        }
        ////// comment here ends

        // Dispose the buffers
        results.Dispose();
        commands.Dispose();
        
        Debug.Log("Check Time Upd: " + ((Time.realtimeSinceStartup - startTime_upd) * 1000000f) + " microsec");
        */

    }





































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
        if (ToBeAdd.Count > 0)
        {
            if (ToBeAdd[0] > XSort.Count)
            {
                Debug.Log("Check here!");
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
