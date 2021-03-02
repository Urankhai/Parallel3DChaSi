using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
//using Unity.Mathematics;
using System;
using System.IO;
//using System.Numerics;

public class ChenGenParallel_TD : MonoBehaviour
{
    public bool If_we_need_MPC3;
    //[SerializeField] private bool useJobs;
    public float SpeedofLight = 299792458.0f; // m/s
    public float CarrierFrequency = 5.9f * (float)Math.Pow(10, 9); // GHz
    public float fsubcarriers = 200000; // kHz



    List<GeoComp> LinearGlobeCom2;
    List<Vector2Int> IndexesGlobeCom2;
    public int MaxLengthOfSeenMPC2Lists;

    public NativeArray<V6> SeenMPC1Table;

    public NativeArray<V6> SeenMPC2Table;
    public NativeArray<GeoComp> LookUpTable2;
    public NativeArray<Vector2Int> LookUpTable2ID;
    
    List<GeoComp> LinearGlobeCom3;
    List<Vector2Int> IndexesGlobeCom3;
    public int MaxLengthOfSeenMPC3Lists;
    
    public NativeArray<V6> SeenMPC3Table;
    public NativeArray<GeoComp> LookUpTable3;
    public NativeArray<Vector2Int> LookUpTable3ID;
    private void OnDisable()
    {
        if (SeenMPC1Table.IsCreated)
        { SeenMPC1Table.Dispose(); }
        if (SeenMPC2Table.IsCreated)
        { SeenMPC2Table.Dispose(); }
        if (LookUpTable2.IsCreated)
        { LookUpTable2.Dispose(); }
        if (LookUpTable2ID.IsCreated)
        { LookUpTable2ID.Dispose(); }
        if (SeenMPC3Table.IsCreated)
        { SeenMPC3Table.Dispose(); }
        if (LookUpTable3.IsCreated)
        { LookUpTable3.Dispose(); }
        if (LookUpTable3ID.IsCreated)
        { LookUpTable3ID.Dispose(); }
        //Debug.Log("All nativearrays are cleaned");
        //Debug.Log("The lenght of the list<list> structure " + h_save.Count);

        /*
        string path1 = Application.persistentDataPath + "/h_time1.csv";

        using (var file = File.CreateText(path1))
        {
            foreach (var arr in h_save)
            {
                //if (String.IsNullOrEmpty(arr)) continue;
                file.Write(arr[0]);
                for (int i = 1; i < arr.Count; i++)
                {
                    file.Write(',');
                    file.Write(arr[i]);
                }
                file.WriteLine();
            }
        }

        string path2 = Application.persistentDataPath + "/H_freq1.csv";

        using (var file = File.CreateText(path2))
        {
            foreach (var arr in H_save)
            {
                //if (String.IsNullOrEmpty(arr)) continue;
                file.Write(arr[0]);
                for (int i = 1; i < arr.Count; i++)
                {
                    file.Write(',');
                    file.Write(arr[i]);
                }
                file.WriteLine();
            }
        }
        */
        Debug.Log(Application.persistentDataPath);
    }

    // extracting Rx data
    public GameObject Rx;
    Transceiver_Channel Rx_Seen_MPC_Script;
    bool Rx_Antenna_pattern;
    List<int> Rx_MPC1;
    List<float> Rx_MPC1_att;
    List<int> Rx_MPC2;
    List<float> Rx_MPC2_att;
    List<int> Rx_MPC3;
    List<float> Rx_MPC3_att;

    // extrecting Tx data
    public GameObject Tx;
    Transceiver_Channel Tx_Seen_MPC_Script;
    bool Tx_Antenna_pattern;
    List<int> Tx_MPC1;
    List<float> Tx_MPC1_att;
    List<int> Tx_MPC2;
    List<float> Tx_MPC2_att;
    List<int> Tx_MPC3;
    List<float> Tx_MPC3_att;

    public bool LOS_Tracer;
    public bool MPC1_Tracer;
    public bool MPC2_Tracer;
    public bool MPC3_Tracer;

    

    /// <summary>
    ///  Data for Fourier transform
    /// </summary>
    double[] Y_output;
    double[] H_output;
    double[] Y_noise_output;
    double[] H_noise_output;
    double[] X_inputValues;
    [Space(10)]
    [Header("CHARTS FOR DRAWING")]
    [Space]
    public Transform tfTime;
    public Transform tfFreq;

    //int update_flag = 0;

    List<V6> MPC1;
    List<V6> MPC2;
    List<V6> MPC3;

    // defining empty elements
    public Path3Half empty_path3Half = new Path3Half(new Vector3(0,0,0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0, 0f, 0f);
    public Path3 empty_path3 = new Path3(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f, 0f);
    public Path2 empty_path2 = new Path2(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f, 0f);
    public Path1 empty_path = new Path1(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f, 0f);

    public System.Random rand = new System.Random(); //reuse this if you are generating many

    // for simulating the edge effect
    [HideInInspector] public int flag_LoS;
    [HideInInspector] public int FixUpdateCount = 0;
    [HideInInspector] public int LoS_Start = 0;
    [HideInInspector] public int LoS_End = 0;
    [HideInInspector] public float EdgeEffect_LoS = 0;

    [HideInInspector] public GameObject textobject;
    public float RelativePermitivity = 5.0f;
    [HideInInspector] public float Z;
    [HideInInspector] public int Nfft = 1024;

    // Start is called before the first frame update
    void Start()
    {

        /// for Fourier transform
        
        X_inputValues = new double[Nfft];
        for (int i = 0; i < Nfft; i++)
        { X_inputValues[i] = i; }

        // ground reflection
        Z = (float)Math.Sqrt(RelativePermitivity - 1);

        // finding the script
        GameObject LookUpT = GameObject.Find("LookUpTables");
        LookUpTableGen LUT_Script = LookUpT.GetComponent<LookUpTableGen>();


        // reading info about MPC2
        LinearGlobeCom2 = LUT_Script.Linear_GC2;
        IndexesGlobeCom2 = LUT_Script.Indexes_GC2;
        MaxLengthOfSeenMPC2Lists = LUT_Script.MaxLengthOfSeenMPC2Lists;

        flag_LoS = 0; // flag will be needed for the edge effect

        // test allocation nativearrays
        LookUpTable2 = new NativeArray<GeoComp>(LinearGlobeCom2.Count, Allocator.Persistent);
        LookUpTable2ID = new NativeArray<Vector2Int>(IndexesGlobeCom2.Count, Allocator.Persistent);

        for (int i = 0; i < LinearGlobeCom2.Count; i++)
        { LookUpTable2[i] = LinearGlobeCom2[i]; }
        for (int i = 0; i < IndexesGlobeCom2.Count; i++)
        { LookUpTable2ID[i] = IndexesGlobeCom2[i]; }

        // reading info about MPC3
        LinearGlobeCom3 = LUT_Script.Linear_GC3;
        IndexesGlobeCom3 = LUT_Script.Indexes_GC3;
        MaxLengthOfSeenMPC3Lists = LUT_Script.MaxLengthOfSeenMPC3Lists;

        LookUpTable3 = new NativeArray<GeoComp>(LinearGlobeCom3.Count, Allocator.Persistent);
        LookUpTable3ID = new NativeArray<Vector2Int>(IndexesGlobeCom3.Count, Allocator.Persistent);

        for (int i = 0; i < LinearGlobeCom3.Count; i++)
        { LookUpTable3[i] = LinearGlobeCom3[i]; }
        for (int i = 0; i < IndexesGlobeCom3.Count; i++)
        { LookUpTable3ID[i] = IndexesGlobeCom3[i]; }
        // test allocation nativearrays

        
        

        GameObject MPC_Spawner = GameObject.Find("Direct_Solution");
        Correcting_polygons MPC_Script = MPC_Spawner.GetComponent<Correcting_polygons>();
        MPC1 = MPC_Script.SeenV6_MPC1;
        MPC2 = MPC_Script.SeenV6_MPC2;
        MPC3 = MPC_Script.SeenV6_MPC3;

        SeenMPC1Table = new NativeArray<V6>(MPC1.Count, Allocator.Persistent);
        for (int i = 0; i < MPC1.Count; i++)
        { SeenMPC1Table[i] = MPC1[i]; }

        SeenMPC2Table = new NativeArray<V6>(MPC2.Count, Allocator.Persistent);
        for (int i = 0; i < MPC2.Count; i++)
        { SeenMPC2Table[i] = MPC2[i]; }

        SeenMPC3Table = new NativeArray<V6>(MPC3.Count, Allocator.Persistent);
        for (int i = 0; i < MPC3.Count; i++)
        { SeenMPC3Table[i] = MPC3[i]; }

        // assigning sripts names to the Tx and Rx
        Tx_Seen_MPC_Script = Tx.GetComponent<Transceiver_Channel>();
        Tx_Antenna_pattern = Tx_Seen_MPC_Script.Antenna_pattern;
        Rx_Seen_MPC_Script = Rx.GetComponent<Transceiver_Channel>();
        Rx_Antenna_pattern = Rx_Seen_MPC_Script.Antenna_pattern;
    }

   

    private void FixedUpdate()
    {
        FixUpdateCount += 1;
        Rx_MPC1 = Rx_Seen_MPC_Script.seen_MPC1;
        Rx_MPC1_att = Rx_Seen_MPC_Script.seen_MPC1_att;

        Rx_MPC2 = Rx_Seen_MPC_Script.seen_MPC2;
        Rx_MPC2_att = Rx_Seen_MPC_Script.seen_MPC2_att;

        Rx_MPC3 = Rx_Seen_MPC_Script.seen_MPC3;
        Rx_MPC3_att = Rx_Seen_MPC_Script.seen_MPC3_att;

        Tx_MPC1 = Tx_Seen_MPC_Script.seen_MPC1;
        Tx_MPC1_att = Tx_Seen_MPC_Script.seen_MPC1_att;

        Tx_MPC2 = Tx_Seen_MPC_Script.seen_MPC2;
        Tx_MPC2_att = Tx_Seen_MPC_Script.seen_MPC2_att;

        Tx_MPC3 = Tx_Seen_MPC_Script.seen_MPC3;
        Tx_MPC3_att = Tx_Seen_MPC_Script.seen_MPC3_att;


        ///////////////////////////////////////////////////////////////////////////////////
        /// LOS
        ///////////////////////////////////////////////////////////////////////////////////

        float dtLoS = 0;
        float PathGainLoS = 0;
        float LOS_distance = 0;

        if (!Physics.Linecast(Tx.transform.position, Rx.transform.position))
        {
            Vector3 LoS_dir = (Tx.transform.position - Rx.transform.position).normalized;
            Vector3 Tx_fwd = Tx.transform.forward;
            Vector3 Rx_fwd = Rx.transform.forward;
            float cos_Tx = -1;
            float cos_Rx = -1;
            float K_antanne_pattern;

            if (Tx_Antenna_pattern)
            {
                cos_Tx = Vector3.Dot(-LoS_dir, Tx_fwd);
            }
            if (Rx_Antenna_pattern)
            { 
                cos_Rx = Vector3.Dot(LoS_dir, Rx_fwd);
            }
            K_antanne_pattern = 0.25f * (1 - cos_Rx - cos_Tx + cos_Rx * cos_Tx);

            flag_LoS = 1;
            if (LoS_Start == 0) { LoS_Start = FixUpdateCount; Debug.Log("LoS Start " + LoS_Start); } // finding the start time of LoS

            if (LOS_Tracer)
            {
                Debug.DrawLine(Tx.transform.position, Rx.transform.position, Color.magenta);
            }

            LOS_distance = (Tx.transform.position - Rx.transform.position).magnitude;
            //Debug.Log("Distance = " + LOS_distance);
            //LOS_distance = 200;
            dtLoS = LOS_distance / SpeedofLight;// + 1000/SpeedofLight;
            PathGainLoS = (1 / (LOS_distance)) * K_antanne_pattern;

            float hbyd = 3.4f / LOS_distance; // 2h/d; h = 1.7meters
            float Rparallel = (RelativePermitivity * hbyd - Z) / (RelativePermitivity * hbyd + Z);
            float Rperpendicular = (hbyd - Z) / (hbyd + Z);
            float Rcoef = (float)Math.Sqrt(0.5f * (Rparallel * Rparallel + Rperpendicular * Rperpendicular));

            //PathGainLoS -= PathGainLoS * Rcoef * Rcoef / 2;
            //float h2byd = 5.78f / LOS_distance; // 2 h^2/2

            EdgeEffect(FixUpdateCount - LoS_Start, out EdgeEffect_LoS); 

            // the follwing can be done due to manual calculation of the LoS End
            if (FixUpdateCount > 100 && FixUpdateCount < 126)
            {
                EdgeEffect(124 - FixUpdateCount, out EdgeEffect_LoS);
            }
            
        }
        /*else
        {
            if (flag_LoS == 1)
            { 
                flag_LoS = 2; 
                LoS_End = FixUpdateCount; 
                Debug.Log("LoS End " + LoS_End);
                // remember the last LoS channel
                for (int i = 0; i < H_LoS.Length; i++)
                {
                    H_old[i] = H_LoS[i];
                }
            } // finding the end time of LoS
        }*/

        //Debug.Log("Edge effect coefficient = " + EdgeEffect_LoS);



        var dtLoSParallel = new NativeArray<float>(1, Allocator.TempJob);
        var distanceLoSParallel = new NativeArray<float>(1, Allocator.TempJob);
        var PathGainLoSParallel = new NativeArray<float>(1, Allocator.TempJob);
        for (int i = 0; i < dtLoSParallel.Length; i++)
        {
            dtLoSParallel[i] = dtLoS;
            distanceLoSParallel[i] = LOS_distance;
            PathGainLoSParallel[i] = PathGainLoS;
        }

        dtLoSParallel.Dispose();
        distanceLoSParallel.Dispose();
        PathGainLoSParallel.Dispose();
        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC1
        ///////////////////////////////////////////////////////////////////////////////////

        var RxArray1 = new NativeArray<int>(Rx_MPC1.Count, Allocator.TempJob);
        var RxArray1_att = new NativeArray<float>(Rx_MPC1.Count, Allocator.TempJob);
        var TxArray1 = new NativeArray<int>(Tx_MPC1.Count, Allocator.TempJob);
        var TxArray1_att = new NativeArray<float>(Tx_MPC1.Count, Allocator.TempJob);

        var possiblePath1 = new NativeArray<Path1>(Tx_MPC1.Count, Allocator.TempJob);
        var dtMPC1Array = new NativeArray<float>(Tx_MPC1.Count, Allocator.TempJob);
        var PathGainMPC1 = new NativeArray<float>(Tx_MPC1.Count, Allocator.TempJob);
        for (int i = 0; i < Rx_MPC1.Count; i++)
        { 
            RxArray1[i] = Rx_MPC1[i]; 
            RxArray1_att[i] = Rx_MPC1_att[i]; 
        }
        
        for (int i = 0; i < Tx_MPC1.Count; i++)
        {
            TxArray1[i] = Tx_MPC1[i];
            TxArray1_att[i] = Tx_MPC1_att[i];
            possiblePath1[i] = empty_path;
        }
        CommonMPC1Parallel commonMPC1Parallel = new CommonMPC1Parallel
        {
            Speed_of_Light = SpeedofLight,
            MPC1 = SeenMPC1Table,
            Array1 = RxArray1,
            Array1_att = RxArray1_att,
            Array2 = TxArray1,
            Array2_att = TxArray1_att,
            Rx_Point = Rx.transform.position,
            Tx_Point = Tx.transform.position,

            Output = possiblePath1,
            OutputDelays = dtMPC1Array,
            OutputAmplitudes = PathGainMPC1,
        };
        JobHandle jobHandleMPC1 = commonMPC1Parallel.Schedule(TxArray1.Length, 2);
        jobHandleMPC1.Complete();

        // transition from NativeArrays to Lists
        List<Path1> first_order_paths_full_parallel = new List<Path1>();
        if (MPC1_Tracer)
        {
            for (int i = 0; i < possiblePath1.Length; i++)
            {
                if (possiblePath1[i].Distance > 0)
                {
                    first_order_paths_full_parallel.Add(possiblePath1[i]);
                    Debug.DrawLine(possiblePath1[i].Rx_Point, possiblePath1[i].MPC1, Color.cyan);
                    Debug.DrawLine(possiblePath1[i].Tx_Point, possiblePath1[i].MPC1, Color.cyan);
                }
            }
        }



        RxArray1.Dispose();
        RxArray1_att.Dispose();
        TxArray1.Dispose();
        TxArray1_att.Dispose();
        possiblePath1.Dispose();
        dtMPC1Array.Dispose();
        PathGainMPC1.Dispose();
        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC2 Parallel
        ///////////////////////////////////////////////////////////////////////////////////

        var level2MPC2 = new NativeArray<int>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var level2MPC2_att = new NativeArray<float>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var possiblePath2 = new NativeArray<Path2>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var dtArrayMPC2 = new NativeArray<float>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var PathGainMPC2 = new NativeArray<float>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);

        for (int l = 0; l < Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists; l++)
        {
            level2MPC2[l] = Rx_MPC2[Mathf.FloorToInt(l / MaxLengthOfSeenMPC2Lists)];
            level2MPC2_att[l] = Rx_MPC2_att[Mathf.FloorToInt(l / MaxLengthOfSeenMPC2Lists)];
            possiblePath2[l] = empty_path2;
        }

        var TxMPC2Array = new NativeArray<int>(Tx_MPC2.Count, Allocator.TempJob);
        var TxMPC2Array_att = new NativeArray<float>(Tx_MPC2.Count, Allocator.TempJob);
        for (int l = 0; l < Tx_MPC2.Count; l++)
        { 
            TxMPC2Array[l] = Tx_MPC2[l];
            TxMPC2Array_att[l] = Tx_MPC2_att[l];
        }

        Path2ParallelSearch path2ParallelSearch = new Path2ParallelSearch
        {
            // common data
            SeenMPC2Table = SeenMPC2Table,
            LookUpTable2 = LookUpTable2,
            LookUpTable2ID = LookUpTable2ID,
            // must be disposed
            Rx_MPC2Array = level2MPC2,
            Rx_MPC2Array_att = level2MPC2_att,
            Tx_MPC2 = TxMPC2Array,
            Tx_MPC2_att = TxMPC2Array_att,

            // other data
            Rx_Position = Rx.transform.position,
            Tx_Position = Tx.transform.position,
            MaxListsLength = MaxLengthOfSeenMPC2Lists,
            Speed_of_Light = SpeedofLight,

            SecondOrderPaths = possiblePath2,
            OutputDelays = dtArrayMPC2,
            OutputAmplitudes = PathGainMPC2,
        };
        // create a job handle list
        JobHandle jobHandleMPC2 = path2ParallelSearch.Schedule(level2MPC2.Length, MaxLengthOfSeenMPC2Lists, jobHandleMPC1);
        
        jobHandleMPC2.Complete();



        /// MPC2
        List<Path2> second_order_paths_full_parallel = new List<Path2>();

        if (MPC2_Tracer)
        {
            for (int l = 0; l < possiblePath2.Length; l++)
            {
                if (possiblePath2[l].Distance > 0)
                {
                    second_order_paths_full_parallel.Add(possiblePath2[l]);
                    Debug.DrawLine(possiblePath2[l].Rx_Point, possiblePath2[l].MPC2_1, Color.white);
                    Debug.DrawLine(possiblePath2[l].MPC2_1, possiblePath2[l].MPC2_2, Color.white);
                    Debug.DrawLine(possiblePath2[l].MPC2_2, possiblePath2[l].Tx_Point, Color.white);
                }
            }
        }

        level2MPC2.Dispose();
        level2MPC2_att.Dispose();
        possiblePath2.Dispose();
        TxMPC2Array.Dispose();
        TxMPC2Array_att.Dispose();
        dtArrayMPC2.Dispose();
        PathGainMPC2.Dispose();

        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC3
        ///////////////////////////////////////////////////////////////////////////////////

        if (If_we_need_MPC3 == true)
        {
            // define how many elements should be processed in a single core
            int innerloopBatchCount = MaxLengthOfSeenMPC3Lists;

            Vector3 Rx_Point = Rx.transform.position;
            Vector3 Tx_Point = Tx.transform.position;

            NativeArray<int> Rx_Seen_MPC3 = new NativeArray<int>(Rx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            NativeArray<float> Rx_Seen_MPC3_att = new NativeArray<float>(Rx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            NativeArray<Path3Half> RxReachableHalfPath3Array = new NativeArray<Path3Half>(Rx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            for (int l = 0; l < Rx_MPC3.Count * MaxLengthOfSeenMPC3Lists; l++)
            {
                Rx_Seen_MPC3[l] = Rx_MPC3[Mathf.FloorToInt(l / MaxLengthOfSeenMPC3Lists)];
                Rx_Seen_MPC3_att[l] = Rx_MPC3_att[Mathf.FloorToInt(l / MaxLengthOfSeenMPC3Lists)];
                RxReachableHalfPath3Array[l] = empty_path3Half;
            }

            NativeArray<int> Tx_Seen_MPC3 = new NativeArray<int>(Tx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            NativeArray<float> Tx_Seen_MPC3_att = new NativeArray<float>(Tx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            NativeArray<Path3Half> TxReachableHalfPath3Array = new NativeArray<Path3Half>(Tx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            for (int l = 0; l < Tx_MPC3.Count * MaxLengthOfSeenMPC3Lists; l++)
            {
                Tx_Seen_MPC3[l] = Tx_MPC3[Mathf.FloorToInt(l / MaxLengthOfSeenMPC3Lists)];
                Tx_Seen_MPC3_att[l] = Tx_MPC3_att[Mathf.FloorToInt(l / MaxLengthOfSeenMPC3Lists)];
                TxReachableHalfPath3Array[l] = empty_path3Half;
            }

            HalfPath3Set RxhalfPath3Set = new HalfPath3Set
            {
                // common data
                SeenMPC3Table = SeenMPC3Table,
                LookUpTable3 = LookUpTable3,
                LookUpTable3ID = LookUpTable3ID,
                MaxListsLength = MaxLengthOfSeenMPC3Lists,

                // Car specific data
                Point = Rx_Point,
                // must be disposed
                Seen_MPC3 = Rx_Seen_MPC3,
                Seen_MPC3_att = Rx_Seen_MPC3_att,
                ReachableHalfPath3 = RxReachableHalfPath3Array,
            };
            // create a job handle list
            NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

            JobHandle RxjobHandleMPC3 = RxhalfPath3Set.Schedule(Rx_Seen_MPC3.Length, innerloopBatchCount, jobHandleMPC2);
            jobHandleList.Add(RxjobHandleMPC3);

            HalfPath3Set TxhalfPath3Set = new HalfPath3Set
            {
                // common data
                SeenMPC3Table = SeenMPC3Table,
                LookUpTable3 = LookUpTable3,
                LookUpTable3ID = LookUpTable3ID,
                MaxListsLength = MaxLengthOfSeenMPC3Lists,

                // Car specific data
                Point = Tx_Point,
                // must be disposed
                Seen_MPC3 = Tx_Seen_MPC3,
                Seen_MPC3_att = Tx_Seen_MPC3_att,
                ReachableHalfPath3 = TxReachableHalfPath3Array,

            };

            JobHandle TxjobHandleMPC3 = TxhalfPath3Set.Schedule(Tx_Seen_MPC3.Length, innerloopBatchCount, jobHandleMPC2);
            jobHandleList.Add(TxjobHandleMPC3);

            JobHandle.CompleteAll(jobHandleList);




            // storing nonempty path3s
            List<Path3Half> TxHalfPath3 = new List<Path3Half>();
            List<Path3Half> RxHalfPath3 = new List<Path3Half>();

            // introducing a little bit of randomness to the third order of paths selection
            int MPC3PathStep = 3; // otherwise, the sets of possible third order of paths become too big
            for (int l = 0; l < TxReachableHalfPath3Array.Length; l += MPC3PathStep)
            {
                if (TxReachableHalfPath3Array[l].Distance > 0)
                {
                    TxHalfPath3.Add(TxReachableHalfPath3Array[l]);
                }
            }
            for (int l = 0; l < RxReachableHalfPath3Array.Length; l += MPC3PathStep)
            {
                if (RxReachableHalfPath3Array[l].Distance > 0)
                {
                    RxHalfPath3.Add(RxReachableHalfPath3Array[l]);
                }
            }




            //float startTime2 = Time.realtimeSinceStartup;

            NativeArray<Path3Half> RxNativeArray = new NativeArray<Path3Half>(RxHalfPath3.Count, Allocator.TempJob);
            for (int i = 0; i < RxNativeArray.Length; i++)
            { RxNativeArray[i] = RxHalfPath3[i]; }

            NativeArray<Path3Half> TxNativeArray = new NativeArray<Path3Half>(TxHalfPath3.Count, Allocator.TempJob);
            for (int i = 0; i < TxNativeArray.Length; i++)
            { TxNativeArray[i] = TxHalfPath3[i]; }

            NativeArray<Path3> activepath3 = new NativeArray<Path3>(RxHalfPath3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            NativeArray<float> dtArrayMPC3 = new NativeArray<float>(RxHalfPath3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
            NativeArray<float> PathGainMPC3 = new NativeArray<float>(RxHalfPath3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);

            Path3ActiveSet Rx_path3ActiveSet = new Path3ActiveSet
            {
                SeenMPC3Table = SeenMPC3Table,
                InputArray = RxNativeArray,
                CompareArray = TxNativeArray,
                MaxListsLength = MaxLengthOfSeenMPC3Lists,
                EmptyElement = empty_path3,
                Speed_of_Light = SpeedofLight,

                Output = activepath3,
                OutputDelays = dtArrayMPC3,
                OutputAmplitudes = PathGainMPC3

            };
            JobHandle Jobforpath3ActiveSet = Rx_path3ActiveSet.Schedule(activepath3.Length, MaxLengthOfSeenMPC3Lists, TxjobHandleMPC3);

            Jobforpath3ActiveSet.Complete();


            List<Path3> third_order_paths_full_parallel = new List<Path3>();

            if (MPC3_Tracer)
            {
                int trace_count = 0;
                for (int l = 0; l < activepath3.Length; l++)
                //for (int l = 0; l < 10; l++)
                {
                    if (activepath3[l].Distance > 0)
                    {
                        trace_count += 1;
                        third_order_paths_full_parallel.Add(activepath3[l]);
                        Debug.DrawLine(activepath3[l].Rx_Point, activepath3[l].MPC3_1, Color.green);
                        Debug.DrawLine(activepath3[l].MPC3_1, activepath3[l].MPC3_2, Color.blue);
                        Debug.DrawLine(activepath3[l].MPC3_2, activepath3[l].MPC3_3, Color.yellow);
                        Debug.DrawLine(activepath3[l].MPC3_3, activepath3[l].Tx_Point, Color.red);
                        //if (trace_count == 10)
                        //{ break; }
                    }
                }
            }
            //Debug.Log("Number of 3rd order paths = " + third_order_paths_full_parallel.Count);
            TxNativeArray.Dispose();
            RxNativeArray.Dispose();
            activepath3.Dispose();
            dtArrayMPC3.Dispose();
            PathGainMPC3.Dispose();

            //Debug.Log("Check 2: " + ((Time.realtimeSinceStartup - startTime2) * 1000000f) + " microsec");






            Rx_Seen_MPC3.Dispose();
            Rx_Seen_MPC3_att.Dispose();
            RxReachableHalfPath3Array.Dispose();
            Tx_Seen_MPC3.Dispose();
            Tx_Seen_MPC3_att.Dispose();
            TxReachableHalfPath3Array.Dispose();
        }
        ///////////////////////////////////////////////////////////////////////////////////
        /// complete the works
        ///////////////////////////////////////////////////////////////////////////////////
        

        Y_output = new double[Nfft];
        H_output = new double[Nfft];
        Y_noise_output = new double[Nfft];
        H_noise_output = new double[Nfft];
        

        Drawing.drawChart(tfTime, X_inputValues, Y_output, "time");
        Drawing.drawChart(tfFreq, X_inputValues, H_output, "frequency");

        

        //Debug.Log("RSS = " + 10* Math.Log10( RSS ) );
        

        
        

        


        
    }



    void EdgeEffect(int Index, out float EdgeCoefficient)
    {
        float factor = (float)Math.PI / 20;

        if (Index < 0)
        { EdgeCoefficient = 0; }
        else if (Index < 20)
        {
            EdgeCoefficient = ((float)Math.Sin(-Math.PI / 2 + Index * factor) + 1)*0.5f;
        }
        else
        { EdgeCoefficient = 1; }
    }
}


[BurstCompile]
public struct ChannelParallel : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> TimeDelayArray;
    [ReadOnly] public NativeArray<float> PathsGainArray;

    [ReadOnly] public NativeArray<float> FrequencyArray;

    [WriteOnly] public NativeArray<System.Numerics.Complex> HH;

    public void Execute(int index)
    {
        System.Numerics.Complex temp_channel = new System.Numerics.Complex(0, 0);
        if (TimeDelayArray.Length == 1)
        {
            double cosine = Math.Cos(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[0]);
            double sine = Math.Sin(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[0]);
            // defining exponent
            System.Numerics.Complex exponent = new System.Numerics.Complex(cosine, sine);

            temp_channel = PathsGainArray[0] * exponent * 0.447 / 32; // quick fix of the transmitter's power, this comes from sqrt(0.2/1024), where 0.2 W = 200 mW
        }
        else
        {
            for (int i = 0; i < TimeDelayArray.Length; i++)
            {
                if (TimeDelayArray[i] > 0)
                {
                    double cosine = Math.Cos(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[i]);
                    double sine = Math.Sin(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[i]);
                    // defining exponent
                    System.Numerics.Complex exponent = new System.Numerics.Complex(cosine, sine);

                    temp_channel += PathsGainArray[i] * exponent * 0.447 / 32; // quick fix of the transmitter's power, this comes from sqrt(0.2/1024), where 0.2 W = 200 mW
                }
            }
        }
        HH[index] = temp_channel;
    }
}



[BurstCompile]
public struct ChannelParallelStable : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> DistancesArray;
    [ReadOnly] public NativeArray<float> PathsGainArray;

    [ReadOnly] public NativeArray<float> InverseWavelengthArray;

    [WriteOnly] public NativeArray<System.Numerics.Complex> HH;

    public void Execute(int index)
    {
        System.Numerics.Complex temp_channel = new System.Numerics.Complex(0, 0);
        if (DistancesArray.Length == 1)
        {
            double cosine = Math.Cos(2 * Math.PI * InverseWavelengthArray[index] * DistancesArray[0]);
            double sine = Math.Sin(2 * Math.PI * InverseWavelengthArray[index] * DistancesArray[0]);
            // defining exponent
            System.Numerics.Complex exponent = new System.Numerics.Complex(cosine, sine);

            temp_channel = PathsGainArray[0] * exponent * 100;
        }
        else
        {
            for (int i = 0; i < DistancesArray.Length; i++)
            {
                if (DistancesArray[i] > 0)
                {
                    double cosine = Math.Cos(2 * Math.PI * InverseWavelengthArray[index] * DistancesArray[i]);
                    double sine = Math.Sin(2 * Math.PI * InverseWavelengthArray[index] * DistancesArray[i]);
                    // defining exponent
                    System.Numerics.Complex exponent = new System.Numerics.Complex(cosine, sine);

                    temp_channel += PathsGainArray[i] * exponent * 100;
                }
            }
        }
        HH[index] = temp_channel;
    }
}

[BurstCompile]
public struct HalfPath3Set : IJobParallelFor
{
    [ReadOnly] public NativeArray<V6> SeenMPC3Table;
    [ReadOnly] public NativeArray<GeoComp> LookUpTable3;
    [ReadOnly] public NativeArray<Vector2Int> LookUpTable3ID;
    [ReadOnly] public Vector3 Point;
    [ReadOnly] public int MaxListsLength;

    [ReadOnly] public NativeArray<int> Seen_MPC3;
    [ReadOnly] public NativeArray<float> Seen_MPC3_att;

    [WriteOnly] public NativeArray<Path3Half> ReachableHalfPath3;
    public void Execute(int index)
    {
        V6 level1 = SeenMPC3Table[Seen_MPC3[index]];
        Vector3 point_to_level1 = (level1.Coordinates - Point).normalized;
        float distance1 = (level1.Coordinates - Point).magnitude;

        int temp_index = Mathf.FloorToInt(index / MaxListsLength);
        int temp_i = index - temp_index * MaxListsLength;
        if (temp_i <= LookUpTable3ID[Seen_MPC3[index]].y - LookUpTable3ID[Seen_MPC3[index]].x)
        {
            
            int temp_l = LookUpTable3ID[Seen_MPC3[index]].x + temp_i;
            // defining the second level of points
            V6 level2 = SeenMPC3Table[LookUpTable3[temp_l].MPCIndex];
            Vector3 level2_to_level1 = (level1.Coordinates - level2.Coordinates).normalized;

            // we define a perpendicular direction to the normal of the level1
            Vector3 normal1 = level1.Normal;
            Vector3 n_perp1 = new Vector3(-normal1.z, 0, normal1.x);

            if ( (Vector3.Dot(n_perp1, point_to_level1) * Vector3.Dot(n_perp1, level2_to_level1)) < 0)
            {
                double theta1 = Math.Acos(Vector3.Dot(normal1, -point_to_level1));
                double theta2 = Math.Acos(Vector3.Dot(normal1, -level2_to_level1));
                int I1, I2, I3;
                if (theta1 + theta2 < 0.35)
                { I1 = 0; }
                else { I1 = 1; }
                if (theta1 < 1.22)
                { I2 = 0; }
                else { I2 = 1; }
                if (theta2 < 1.22)
                { I3 = 0; }
                else { I3 = 1; }

                float angular_gain = Seen_MPC3_att[index]*(float)Math.Exp(-12 * ((theta1 + theta2 - 0.35) * I1 + (theta1 - 1.22) * I2 + (theta2 - 1.22) * I3));

                float distance2 = (level1.Coordinates - level2.Coordinates).magnitude;
                float distance = distance1 + distance2;
                Path3Half temp_Path3Half = new Path3Half(Point, level1.Coordinates, SeenMPC3Table[LookUpTable3[temp_l].MPCIndex].Coordinates, LookUpTable3[temp_l].MPCIndex, distance, angular_gain);
                ReachableHalfPath3[index] = temp_Path3Half;
            }

            
        }

    }
}



[BurstCompile]
public struct CommonMPC1Parallel : IJobParallelFor
{
    // parallel run performed only above this array
    [ReadOnly] public NativeArray<int> Array1;
    [ReadOnly] public NativeArray<float> Array1_att;
    // this is a standard array
    [ReadOnly] public NativeArray<int> Array2;
    [ReadOnly] public NativeArray<float> Array2_att;
    [ReadOnly] public NativeArray<V6> MPC1;

    [ReadOnly] public Vector3 Rx_Point, Tx_Point;
    [ReadOnly] public float Speed_of_Light;
    // this is an array where boolean yes are written
    [WriteOnly] public NativeArray<Path1> Output;
    [WriteOnly] public NativeArray<float> OutputDelays;
    [WriteOnly] public NativeArray<float> OutputAmplitudes;

    public void Execute(int index)
    {
        OutputDelays[index] = 0;
        OutputAmplitudes[index] = 0;
        for (int i = 0; i < Array1.Length; i++)
        {
            if (Array2[index] == Array1[i])
            {
                Vector3 temp_normal = MPC1[Array2[index]].Normal;
                Vector3 n_perp = new Vector3(-temp_normal.z, 0, temp_normal.x);
                Vector3 rx_direction = (Rx_Point - MPC1[Array2[index]].Coordinates).normalized;
                Vector3 tx_direction = (Tx_Point - MPC1[Array2[index]].Coordinates).normalized;

                if ((Vector3.Dot(n_perp, rx_direction) * Vector3.Dot(n_perp, tx_direction)) < 0)
                {
                    // we mimic that we calculate the angles
                    double theta1 = Math.Acos(Vector3.Dot(temp_normal, rx_direction));
                    double theta2 = Math.Acos(Vector3.Dot(temp_normal, tx_direction));
                    
                    int I1, I2, I3;
                    if (theta1 + theta2 < 0.35)
                    { I1 = 0; } 
                    else  { I1 = 1; }
                    if (theta1 < 1.22)
                    { I2 = 0; }
                    else { I2 = 1; }
                    if (theta2 < 1.22)
                    { I3 = 0; }
                    else { I3 = 1; }

                    float angular_gain = (float)Math.Exp(  -12*( (theta1 + theta2 - 0.35)*I1 + (theta1 - 1.22)*I2 + (theta2 - 1.22)*I3 )  );


                    float rx_distance = (Rx_Point - MPC1[Array2[index]].Coordinates).magnitude;
                    float tx_distance = (Tx_Point - MPC1[Array2[index]].Coordinates).magnitude;
                    float distance = rx_distance + tx_distance;
                    Path1 temp_Path1 = new Path1(Rx_Point, MPC1[Array2[index]].Coordinates, Tx_Point, distance, angular_gain);
                    Output[index] = temp_Path1;
                    OutputDelays[index] = distance;// / Speed_of_Light;
                    OutputAmplitudes[index] = Array2_att[index] * Array1_att[i] * angular_gain * (1 / distance);
                }
                break;
            }
        }
    }
}