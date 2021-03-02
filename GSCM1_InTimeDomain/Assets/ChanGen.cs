using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class ChanGen : MonoBehaviour
{
    

    List<List<GeoComp>> GlobeCom2;
    List<List<GeoComp>> GlobeCom3;


    // extracting Rx data
    public GameObject Rx;
    Transceiver_Channel Rx_Seen_MPC_Script;
    List<int> Rx_MPC1;
    List<int> Rx_MPC2;
    List<int> Rx_MPC3;
    // extrecting Tx data
    public GameObject Tx;
    Transceiver_Channel Tx_Seen_MPC_Script;
    List<int> Tx_MPC1;
    List<int> Tx_MPC2;
    List<int> Tx_MPC3;

    public bool LOS_Tracer;
    public bool MPC1_Tracer;
    public bool MPC2_Tracer;
    public bool MPC3_Tracer;

    //int update_flag = 0;

    List<V6> MPC1;
    List<V6> MPC2;
    List<V6> MPC3;

    // Start is called before the first frame update
    void Start()
    {
        GameObject LookUpT = GameObject.Find("LookUpTables");
        LookUpTableGen LUT_Script = LookUpT.GetComponent<LookUpTableGen>();
        GlobeCom2 = LUT_Script.GC2; // coordinates, normals, distances, departing and arriving angles
        GlobeCom3 = LUT_Script.GC3; // coordinates, normals, distances, departing and arriving angles

        GameObject MPC_Spawner = GameObject.Find("Direct_Solution");
        Correcting_polygons MPC_Script = MPC_Spawner.GetComponent<Correcting_polygons>();
        MPC1 = MPC_Script.SeenV6_MPC1;
        MPC2 = MPC_Script.SeenV6_MPC2;
        MPC3 = MPC_Script.SeenV6_MPC3;

        // assigning sripts names to the Tx and Rx
        Tx_Seen_MPC_Script = Tx.GetComponent<Transceiver_Channel>();
        Rx_Seen_MPC_Script = Rx.GetComponent<Transceiver_Channel>();
    }

    /*
    // Update is called once per frame
    void Update()
    {
        update_flag = 1;

        Rx_MPC1 = Rx_Seen_MPC_Script.seen_MPC1;
        Rx_MPC2 = Rx_Seen_MPC_Script.seen_MPC2;
        Rx_MPC3 = Rx_Seen_MPC_Script.seen_MPC3;

        Tx_MPC1 = Tx_Seen_MPC_Script.seen_MPC1;
        Tx_MPC2 = Tx_Seen_MPC_Script.seen_MPC2;
        Tx_MPC3 = Tx_Seen_MPC_Script.seen_MPC3;


    }
    */

    private void FixedUpdate()
    {
        Rx_MPC1 = Rx_Seen_MPC_Script.seen_MPC1;
        Rx_MPC2 = Rx_Seen_MPC_Script.seen_MPC2;
        Rx_MPC3 = Rx_Seen_MPC_Script.seen_MPC3;

        Tx_MPC1 = Tx_Seen_MPC_Script.seen_MPC1;
        Tx_MPC2 = Tx_Seen_MPC_Script.seen_MPC2;
        Tx_MPC3 = Tx_Seen_MPC_Script.seen_MPC3;



        ///////////////////////////////////////////////////////////////////////////////////
        /// LOS
        ///////////////////////////////////////////////////////////////////////////////////
        // float startTime = Time.realtimeSinceStartup;
        if (!Physics.Linecast(Tx.transform.position, Rx.transform.position))
        {
            float LOS_distance = (Tx.transform.position - Rx.transform.position).magnitude;
            if (LOS_Tracer)
            {
                Debug.DrawLine(Tx.transform.position, Rx.transform.position, Color.magenta);
            }
        }
        // Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000000f) + "microsec");
        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC1
        ///////////////////////////////////////////////////////////////////////////////////
        List<int> common_mpc1 = new List<int>();

        CommonMPC1(Rx_MPC1, Tx_MPC1, out common_mpc1);

        List<int> active_MPC1 = new List<int>();
        for (int i = 0; i < common_mpc1.Count; i++)

        {
            // defining peprendicular to the considered normal that is parallel to the ground. 
            Vector3 n_perp = new Vector3(-MPC1[common_mpc1[i]].Normal.z, 0, MPC1[common_mpc1[i]].Normal.x);
            float rx_distance = (Rx.transform.position - MPC1[common_mpc1[i]].Coordinates).magnitude;
            float tx_distance = (Rx.transform.position - MPC1[common_mpc1[i]].Coordinates).magnitude;
            Vector3 rx_direction = (Rx.transform.position - MPC1[common_mpc1[i]].Coordinates).normalized;
            Vector3 tx_direction = (Tx.transform.position - MPC1[common_mpc1[i]].Coordinates).normalized;

            if ((Vector3.Dot(n_perp, rx_direction) * Vector3.Dot(n_perp, tx_direction)) < 0)
            {
                active_MPC1.Add(common_mpc1[i]);
                if (MPC1_Tracer)
                {
                    Debug.DrawLine(Rx.transform.position, MPC1[common_mpc1[i]].Coordinates, Color.red);
                    Debug.DrawLine(Tx.transform.position, MPC1[common_mpc1[i]].Coordinates, Color.blue);
                }
            }
        }




        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC2
        ///////////////////////////////////////////////////////////////////////////////////
        List<Path2> second_order_paths = new List<Path2>();

        int brute_force = 1;
        for (int i = 0; i < Rx_MPC2.Count; i++)
        {
            List<GeoComp> temp_list = new List<GeoComp>();
            if (brute_force == 1)
            {
                temp_list = GlobeCom2[Rx_MPC2[i]];
            }
            else
            {
                for (int j = 0; j < MPC2.Count; j++)
                {
                    float dist_xxx = (MPC2[j].Coordinates - MPC2[Rx_MPC2[i]].Coordinates).magnitude;
                    if (dist_xxx < 70)
                    {
                        if (!Physics.Linecast(MPC2[j].Coordinates, MPC2[Rx_MPC2[i]].Coordinates))
                        {
                            float aod = Mathf.Acos(Vector3.Dot((MPC2[j].Coordinates - MPC2[Rx_MPC2[i]].Coordinates).normalized, MPC2[j].Normal));
                            float aoa = Mathf.Acos(-Vector3.Dot((MPC2[j].Coordinates - MPC2[Rx_MPC2[i]].Coordinates).normalized, MPC2[Rx_MPC2[i]].Normal));
                            GeoComp asd = new GeoComp(j, dist_xxx, aod, aoa);
                            temp_list.Add(asd);
                        }
                    }
                }
            }

            for (int ii = 0; ii < Tx_MPC2.Count; ii++)
            {
                if (temp_list.Any(geocom => geocom.MPCIndex == Tx_MPC2[ii]))
                {
                    int temp_index = temp_list.FindIndex(geocom => geocom.MPCIndex == Tx_MPC2[ii]);
                    // defining peprendicular to the considered normal that is parallel to the ground. 
                    V6 temp_Rx_MPC2 = MPC2[Rx_MPC2[i]];
                    V6 temp_Tx_MPC2 = MPC2[Tx_MPC2[ii]];

                    Vector3 n_perp1 = new Vector3(-temp_Rx_MPC2.Normal.z, 0, temp_Rx_MPC2.Normal.x);
                    Vector3 Rx_dir1 = (Rx.transform.position - temp_Rx_MPC2.Coordinates).normalized;
                    Vector3 RT = (temp_Tx_MPC2.Coordinates - temp_Rx_MPC2.Coordinates).normalized;

                    Vector3 n_perp2 = new Vector3(-temp_Tx_MPC2.Normal.z, 0, temp_Tx_MPC2.Normal.x);
                    Vector3 Tx_dir2 = (Tx.transform.position - temp_Tx_MPC2.Coordinates).normalized;
                    Vector3 TR = (temp_Rx_MPC2.Coordinates - temp_Tx_MPC2.Coordinates).normalized;

                    if ((Vector3.Dot(n_perp1, Rx_dir1) * Vector3.Dot(n_perp1, RT) < 0) && (Vector3.Dot(n_perp2, Tx_dir2) * Vector3.Dot(n_perp2, TR) < 0))
                    {
                        float rx_distance2MPC2 = (Rx.transform.position - MPC2[Rx_MPC2[i]].Coordinates).magnitude;
                        float tx_distance2MPC2 = (Tx.transform.position - MPC2[Tx_MPC2[ii]].Coordinates).magnitude;
                        float MPC2_distance = temp_list[temp_index].Distance;
                        float total_distance = rx_distance2MPC2 + MPC2_distance + tx_distance2MPC2;

                        Path2 temp_path2 = new Path2(Rx.transform.position, MPC2[Rx_MPC2[i]].Coordinates, MPC2[Tx_MPC2[ii]].Coordinates, Tx.transform.position, total_distance, 0.0f);
                        second_order_paths.Add(temp_path2);
                        if (MPC2_Tracer)
                        {
                            Debug.DrawLine(temp_path2.Rx_Point, temp_path2.MPC2_1, Color.green);
                            Debug.DrawLine(temp_path2.MPC2_1, temp_path2.MPC2_2, Color.yellow);
                            Debug.DrawLine(temp_path2.MPC2_2, temp_path2.Tx_Point, Color.cyan);
                        }
                    }
                }
            }

        }



    }

    void CommonMPC1(List<int> List1, List<int> List2, out List<int> CommonList)
    {
        CommonList = List1.Intersect(List2).ToList();
    }
}

public struct Path1
{
    public Vector3 Rx_Point;
    public Vector3 MPC1;
    public Vector3 Tx_Point;
    public float Distance;
    public float AngularGain;

    public Path1(Vector3 a, Vector3 b, Vector3 c, float distance, float angulargain)
    {
        Rx_Point = a;
        MPC1 = b;
        Tx_Point = c;
        Distance = distance;
        AngularGain = angulargain;
    }
}

public struct Path2
{
    public Vector3 Rx_Point;
    public Vector3 MPC2_1;
    public Vector3 MPC2_2;
    public Vector3 Tx_Point;
    public float Distance;
    public float AngularGain;

    public Path2(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float distance, float angulargain)
    {
        Rx_Point = a;
        MPC2_1 = b;
        MPC2_2 = c;
        Tx_Point = d;
        Distance = distance;
        AngularGain = angulargain;
    }
}

public struct Path3
{
    public Vector3 Rx_Point;
    public Vector3 MPC3_1;
    public Vector3 MPC3_2;
    public Vector3 MPC3_3;
    public Vector3 Tx_Point;
    public float Distance;
    public float AngularGain;

    public Path3(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, float distance, float angulargain)
    {
        Rx_Point = a;
        MPC3_1 = b;
        MPC3_2 = c;
        MPC3_3 = d;
        Tx_Point = e;
        Distance = distance;
        AngularGain = angulargain;
    }

}

 public struct Path3Half
{
    public Vector3 Point;
    public Vector3 MPC3_1;
    public Vector3 MPC3_2;
    public int MPC3_2ID;
    public float Distance;
    public float AngularGain;

    public Path3Half(Vector3 a, Vector3 b, Vector3 c, int d, float distance, float angulargain)
    {
        Point = a;
        MPC3_1 = b;
        MPC3_2 = c;
        MPC3_2ID = d;
        Distance = distance;
        AngularGain = angulargain;
    }
}