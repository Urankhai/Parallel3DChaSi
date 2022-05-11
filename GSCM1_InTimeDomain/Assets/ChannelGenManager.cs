using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.IO;
using System;
using UnityEditor;

public partial class ChannelGenManager : MonoBehaviour
{
    int FrameCounter;
    int NeigbouringCount;
    
    [Space]
    [Header("PHYSICAL PARAMETERS")]
    [Space]
    public float PowerInMilliWatts;
    public float SpeedofLight = 299792458.0f; // m/s
    public float CarrierFrequency = (float)(5.9 * Mathf.Pow(10, 9)); // GHz
    public float fsubcarriers = (float)200000; // kHz

    public bool OmniAntenna = false;
    //float EdgeEffectCoeff = -100.0f;

    [Space]
    [Header("RESULTS FILE")]
    [Space]

    public string filename;

    /// <summary>
    ///  Data for Fourier transform
    /// </summary>

    double[] Y_output;
    double[] H_output;
    //double[] Y_noise_output;
    //double[] H_noise_output;
    double[] X_inputValues;
    [Space]
    [Header("CHARTS FOR DRAWING")]
    [Space]

    public Transform tfTime;
    public Transform tfFreq;

    // for saving data
    public List<List<string>> H_save = new List<List<string>>(); // for saving data into a csv file
    public List<List<string>> h_save = new List<List<string>>(); // for saving data into a csv file

    // control parameters
    public bool DrawingOverlaps = false;
    public bool DrawingPath1 = false;
    public bool DrawingPath2 = false;
    public bool DrawingPath3 = false;

    // MPCs Data
    //NativeArray<V6> DMC_Native;
    //NativeArray<Vector3> DMC_perp;
    //NativeArray<float> DMC_attenuation;
    //NativeArray<V6> MPC1_Native;
    //NativeArray<Vector3> MPC1_perp;
    //NativeArray<float> MPC1_attenuation;
    //NativeArray<V6> MPC2_Native;
    //NativeArray<Vector3> MPC2_perp;
    //NativeArray<float> MPC2_attenuation;
    //NativeArray<V6> MPC3_Native;
    //NativeArray<Vector3> MPC3_perp;
    //NativeArray<float> MPC3_attenuation;
    int DMC_num;
    int MPC1_num;
    int MPC2_num;
    int MPC3_num;

    NativeArray<Vector2> Pattern;

    int MPC_num;
    NativeArray<V6> MPC_Native;
    NativeArray<Vector3> MPC_perp;
    NativeArray<float> MPC_attenuation;

    // LookUp Table Data
    float maxdistance;
    //int maxNumberSeenMPC2;
    //int maxNumberSeenMPC3;
    NativeArray<SeenPath2> LookUpTableMPC2; // this array shows all properties of the path2s
    NativeArray<Vector2Int> MPC2LUTID; // this array shows how many paths come from each MPC2
    NativeArray<SeenPath3> LookUpTableMPC3;
    NativeArray<Vector2Int> MPC3SeenID;

    // Coordinates of antennas
    NativeArray<Vector3> OldCoordinates;
    NativeArray<Vector3> CarCoordinates;
    NativeArray<Vector3> CarForwardVect;
    NativeArray<Vector3> CarsSpeed;

    // Creating nativearrays in this script that should be destroyed
    NativeArray<Vector2Int> Links;
    NativeArray<Overlap> Overlaps1;
    NativeArray<Overlap> TxOverlaps2;
    NativeArray<Overlap> RxOverlaps2;
    NativeArray<Overlap> TxOverlaps3;
    NativeArray<Overlap> RxOverlaps3;
    int link_num;
    int car_num;

    //LoS
    NativeArray<RaycastCommand> commandsLoS;
    NativeArray<RaycastHit> resultsLoS;
    NativeArray<Vector3> CornersNormalsPerpendiculars;
    //DMC
    /*
    NativeArray<float> SoA0;
    NativeArray<int> Seen0; // either 0 or 1
    NativeArray<RaycastCommand> commands0; // for DMCs
    NativeArray<RaycastHit> results0; // for DMCs
    NativeArray<float> SoA1;
    NativeArray<int> Seen1; // either 0 or 1
    NativeArray<RaycastCommand> commands1;  // for MPC1s
    NativeArray<RaycastHit> results1;  // for MPC1s
    NativeArray<float> SoA2;
    NativeArray<int> Seen2; // either 0 or 1
    NativeArray<RaycastCommand> commands2;  // for MPC2s
    NativeArray<RaycastHit> results2;  // for MPC2s
    NativeArray<float> SoA3;
    NativeArray<int> Seen3; // either 0 or 1
    NativeArray<RaycastCommand> commands3;  // for MPC3s
    NativeArray<RaycastHit> results3;  // for MPC3s
    */

    NativeArray<float> SoA;
    NativeArray<int> Seen; // either 0 or 1
    NativeArray<RaycastCommand> commands; // for DMCs
    NativeArray<RaycastHit> results; // for DMCs

    static readonly int FFTNum = 1024;
    public System.Numerics.Complex[] H = new System.Numerics.Complex[FFTNum]; // Half of LTE BandWidth, instead of 2048 subcarriers

    

    NativeArray<float> Subcarriers;
    NativeArray<float> InverseWavelengths;
    NativeArray<System.Numerics.Complex> H_LoS;
    //NativeArray<System.Numerics.Complex> H0;
    //NativeArray<System.Numerics.Complex> H1;
    //NativeArray<System.Numerics.Complex> H2;
    //NativeArray<System.Numerics.Complex> H3;
    NativeArray<System.Numerics.Complex> H_NLoS;

    public float SubframePower;
    private void OnEnable()
    {
        SubframePower = PowerInMilliWatts / FFTNum;
        //Debug.Log("Power per subframe = " + SubframePower);

        Subcarriers = new NativeArray<float>(FFTNum, Allocator.Persistent);
        InverseWavelengths = new NativeArray<float>(FFTNum, Allocator.Persistent);
        
        for (int i = 0; i < FFTNum; i++)
        {
            Subcarriers[i] = CarrierFrequency + fsubcarriers * i;
            InverseWavelengths[i] = Subcarriers[i] / SpeedofLight;
        }

        Debug.Log("File name" + filename);

        List<float> listA = new List<float>();
        List<float> listB = new List<float>();

        using (var reader = new StreamReader(@"C:\Users\Administrator\Desktop\Aleksei\Parallel3DChaSi\GSCM1_InTimeDomain\Assets\EADF\HelixV2VEADF.csv"))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                //(float)Convert.ToDouble("41.00027357629127");
                listA.Add((float)Convert.ToDouble(values[0]));
                listB.Add((float)Convert.ToDouble(values[1]));
            }
            //Debug.Log(listA.Count);
        }

        Pattern = new NativeArray<Vector2>(listA.Count, Allocator.Persistent); //new Vector2[17];
        
        for (int i=0; i < listA.Count; i++)
        {
            Pattern[i] = new Vector2(listA[i], listB[i]);
        }

        //Pattern[0] = new Vector2(0.0068f, -0.0004f);
        //Pattern[1] = new Vector2(-0.0024f, 0.0024f);
        //Pattern[2] = new Vector2(-0.0751f, 0.0071f);
        //Pattern[3] = new Vector2(0.0051f, -0.0603f);

        //Pattern[4] = new Vector2(0.2522f, 0.0103f);
        //Pattern[5] = new Vector2(-0.0464f, 0.3858f);
        //Pattern[6] = new Vector2(-0.8632f, -0.0495f);
        //Pattern[7] = new Vector2(0.0175f, -1.2042f);

        //Pattern[8] = new Vector2(1.6836f, 0.0000f);
        //Pattern[9] = new Vector2(0.0175f, 1.2042f);
        //Pattern[10] = new Vector2(-0.8632f, 0.0495f);
        //Pattern[11] = new Vector2(-0.0464f, 0.3858f);

        //Pattern[12] = new Vector2(0.2522f, -0.0103f);
        //Pattern[13] = new Vector2(0.0051f, 0.0603f);
        //Pattern[14] = new Vector2(-0.0751f, 0.0071f);
        //Pattern[15] = new Vector2(-0.0024f, -0.0024f);
        //Pattern[16] = new Vector2(0.0068f, 0.0004f);
    }

    private void OnDestroy()
    {
        Pattern.Dispose();
        Links.Dispose();
        Overlaps1.Dispose();
        TxOverlaps2.Dispose();
        RxOverlaps2.Dispose();
        TxOverlaps3.Dispose();
        RxOverlaps3.Dispose();

        commandsLoS.Dispose();
        resultsLoS.Dispose();
        /*
        SoA0.Dispose();
        Seen0.Dispose();
        commands0.Dispose();
        results0.Dispose();

        SoA1.Dispose();
        Seen1.Dispose();
        commands1.Dispose();
        results1.Dispose();

        SoA2.Dispose();
        Seen2.Dispose();
        commands2.Dispose();
        results2.Dispose();

        SoA3.Dispose();
        Seen3.Dispose();
        commands3.Dispose();
        results3.Dispose();
        */
        SoA.Dispose();
        Seen.Dispose();
        commands.Dispose();
        results.Dispose();

        Subcarriers.Dispose();
        InverseWavelengths.Dispose();
        H_LoS.Dispose();
        /*
        H0.Dispose();
        H1.Dispose();
        H2.Dispose();
        H3.Dispose();
        */
        H_NLoS.Dispose();
    }

    /*
    private void OnDisable()
    {
        string path = @"C:\Users\Administrator\Desktop\Aleksei\Parallel3DChaSi\GSCM1_InTimeDomain\Assets\";
        string path1 = path + "h_time1.csv";//Application.persistentDataPath + "/h_time1.csv";

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

        string path2 = path + filename;// Application.persistentDataPath + "/H_freq1.csv";

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

        Debug.Log("The lenght of the list<list> structure " + h_save.Count);
    }
    */

    void Start()
    {
        FrameCounter = 0;
        NeigbouringCount = 0;
        /// for Fourier transform
        X_inputValues = new double[H.Length];
        for (int i = 0; i < H.Length; i++)
        { X_inputValues[i] = i; }

        #region Reading Info from Scripts
        #region MPCs
        // MPCs Data
        GameObject MPC_Spawner = GameObject.Find("CorrectPolygons");
        Correcting_polygons_Native MPC_Native_Script = MPC_Spawner.GetComponent<Correcting_polygons_Native>();

        DMC_num  = MPC_Native_Script.ActiveV6_DMC_NativeList.Length;
        MPC1_num = MPC_Native_Script.ActiveV6_MPC1_NativeList.Length;
        MPC2_num = MPC_Native_Script.ActiveV6_MPC2_NativeList.Length;
        MPC3_num = MPC_Native_Script.ActiveV6_MPC3_NativeList.Length;


        CornersNormalsPerpendiculars = MPC_Native_Script.Active_CornersNormalsPerpendiculars;
        //DMC_Native = MPC_Native_Script.ActiveV6_DMC_NativeList;
        //MPC1_Native = MPC_Native_Script.ActiveV6_MPC1_NativeList;
        //MPC2_Native = MPC_Native_Script.ActiveV6_MPC2_NativeList;
        //MPC3_Native = MPC_Native_Script.ActiveV6_MPC3_NativeList;

        //DMC_attenuation = MPC_Native_Script.ActiveV6_DMC_Power;
        //MPC1_attenuation = MPC_Native_Script.ActiveV6_MPC1_Power;
        //MPC2_attenuation = MPC_Native_Script.ActiveV6_MPC2_Power;
        //MPC3_attenuation = MPC_Native_Script.ActiveV6_MPC3_Power;

        //DMC_perp = MPC_Native_Script.Active_DMC_Perpendiculars;
        //MPC1_perp = MPC_Native_Script.Active_MPC1_Perpendiculars;
        //MPC2_perp = MPC_Native_Script.Active_MPC2_Perpendiculars;
        //MPC3_perp = MPC_Native_Script.Active_MPC3_Perpendiculars;
        
        // for all MPCs
        MPC_num = MPC_Native_Script.ActiveV6_MPC_NativeList.Length;
        MPC_Native = MPC_Native_Script.ActiveV6_MPC_NativeList;
        MPC_attenuation = MPC_Native_Script.ActiveV6_MPC_Power;
        MPC_perp = MPC_Native_Script.Active_MPC_Perpendiculars;

        #endregion

        #region LookUpTable
        // LookUp Table Data
        GameObject LookUpTable = GameObject.Find("LookUpTablesUpd");
        LookUpTableGenUpd LUT_Script = LookUpTable.GetComponent<LookUpTableGenUpd>();
        maxdistance = LUT_Script.MaxSeenDistance;
        //maxNumberSeenMPC2 = LUT_Script.maxlengthMPC2;
        //maxNumberSeenMPC3 = LUT_Script.maxlengthMPC3;

        LookUpTableMPC2 = LUT_Script.LookUpTableMPC2;
        MPC2LUTID = LUT_Script.MPC2LUTID;
        LookUpTableMPC3 = LUT_Script.LookUpTableMPC3;
        MPC3SeenID = LUT_Script.MPC3SeenID;
        #endregion

        #region Positions
        // Coordinates of antennas
        GameObject VehiclesData = GameObject.Find("MovementManager");
        AllVehiclesControl ControlScript= VehiclesData.GetComponent<AllVehiclesControl>();
        OldCoordinates = ControlScript.OldCoordinates;
        CarCoordinates = ControlScript.CarCoordinates;
        CarForwardVect = ControlScript.CarForwardVect;
        CarsSpeed = ControlScript.CarsSpeed;
        #endregion

        #endregion

        car_num = CarCoordinates.Length;

        link_num = car_num * (car_num - 1) / 2;
        
        Links = new NativeArray<Vector2Int>(link_num, Allocator.Persistent);
        Overlaps1 = new NativeArray<Overlap>(link_num, Allocator.Persistent);
        TxOverlaps2 = new NativeArray<Overlap>(link_num, Allocator.Persistent);
        RxOverlaps2 = new NativeArray<Overlap>(link_num, Allocator.Persistent);
        TxOverlaps3 = new NativeArray<Overlap>(link_num, Allocator.Persistent);
        RxOverlaps3 = new NativeArray<Overlap>(link_num, Allocator.Persistent);

        // LoS
        commandsLoS = new NativeArray<RaycastCommand>(2*link_num, Allocator.Persistent);
        resultsLoS = new NativeArray<RaycastHit>(2*link_num, Allocator.Persistent);
        
        /*
        // DMC
        SoA0 = new NativeArray<float>(DMC_num * car_num, Allocator.Persistent);
        Seen0 = new NativeArray<int>(DMC_num * car_num, Allocator.Persistent);
        commands0 = new NativeArray<RaycastCommand>(DMC_num * car_num, Allocator.Persistent);
        results0 = new NativeArray<RaycastHit>(DMC_num * car_num, Allocator.Persistent);
        // MPC1
        SoA1 = new NativeArray<float>(MPC1_num * car_num, Allocator.Persistent);
        Seen1 = new NativeArray<int>(MPC1_num * car_num, Allocator.Persistent);
        commands1 = new NativeArray<RaycastCommand>(MPC1_num * car_num, Allocator.Persistent);
        results1 = new NativeArray<RaycastHit>(MPC1_num * car_num, Allocator.Persistent);
        // MPC2
        SoA2 = new NativeArray<float>(MPC2_num * car_num, Allocator.Persistent);
        Seen2 = new NativeArray<int>(MPC2_num * car_num, Allocator.Persistent);
        commands2 = new NativeArray<RaycastCommand>(MPC2_num * car_num, Allocator.Persistent);
        results2 = new NativeArray<RaycastHit>(MPC2_num * car_num, Allocator.Persistent);
        // MPC3
        SoA3 = new NativeArray<float>(MPC3_num * car_num, Allocator.Persistent);
        Seen3 = new NativeArray<int>(MPC3_num * car_num, Allocator.Persistent);
        commands3 = new NativeArray<RaycastCommand>(MPC3_num * car_num, Allocator.Persistent);
        results3 = new NativeArray<RaycastHit>(MPC3_num * car_num, Allocator.Persistent);
        */

        // MPCs
        SoA = new NativeArray<float>( (DMC_num + MPC1_num + MPC2_num + MPC3_num) * car_num, Allocator.Persistent);
        Seen = new NativeArray<int>((DMC_num + MPC1_num + MPC2_num + MPC3_num) * car_num, Allocator.Persistent);
        commands = new NativeArray<RaycastCommand>((DMC_num + MPC1_num + MPC2_num + MPC3_num) * car_num, Allocator.Persistent);
        results = new NativeArray<RaycastHit>((DMC_num + MPC1_num + MPC2_num + MPC3_num) * car_num, Allocator.Persistent);

        int link_count = 0;
        for (int i = 0; i < car_num; i++)
        {
            for (int j = i + 1; j < car_num; j++)
            {
                Links[link_count] = new Vector2Int(i, j);
                link_count += 1;
            }
        }

        // Channels for all links
        H_LoS = new NativeArray<System.Numerics.Complex>(FFTNum * link_num, Allocator.Persistent);
        //H0 = new NativeArray<System.Numerics.Complex>(FFTNum * link_num, Allocator.Persistent);
        //H1 = new NativeArray<System.Numerics.Complex>(FFTNum * link_num, Allocator.Persistent);
        //H2 = new NativeArray<System.Numerics.Complex>(FFTNum * link_num, Allocator.Persistent);
        //H3 = new NativeArray<System.Numerics.Complex>(FFTNum * link_num, Allocator.Persistent);
        H_NLoS = new NativeArray<System.Numerics.Complex>(FFTNum * link_num, Allocator.Persistent);


        //EdgeEffectCoeff = 10.0f;
    }

    private void FixedUpdate()
    //void Update()
    {
        FrameCounter++;
        
        

        if (Mathf.Abs(-18.0f - CarCoordinates[1].z) < 1.0f)
        {
            Debug.Log("Frame number = " + FrameCounter);
            NeigbouringCount++;
            if (NeigbouringCount == 1)
            {
                Debug.Log("Frame number = " + FrameCounter);
                
                string path = @"C:\Users\Administrator\Desktop\Aleksei\Parallel3DChaSi\GSCM1_InTimeDomain\Assets\";
                string path2 = path + filename;// Application.persistentDataPath + "/H_freq1.csv";

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

                Debug.Log("The lenght of the list<list> structure " + H_save.Count);
                //Application.Quit();

                EditorApplication.isPlaying = false;
            }
        }

        //Debug.Log("Car 4 coordinates " + CarCoordinates[1]);

        #region Defining edges of seen areas for all links (The duration is about 20 micro seconds)
        AreaOverlaps areaOverlaps = new AreaOverlaps
        {
            MaxDist = maxdistance,
            Coordinates = CarCoordinates,
            Links = Links,

            AreaArray1 = Overlaps1,
            TxAreaArray2 = TxOverlaps2,
            RxAreaArray2 = RxOverlaps2,
            TxAreaArray3 = TxOverlaps3,
            RxAreaArray3 = RxOverlaps3,
        };
        JobHandle findoverlaps = areaOverlaps.Schedule(link_num, 1);
        findoverlaps.Complete();
        
        if (DrawingOverlaps)
        {
            DrawOverlaps(Overlaps1);
            DrawOverlaps(TxOverlaps2);
            DrawOverlaps(TxOverlaps3);
        }
        #endregion


        #region DrawOverlaps Function
        void DrawOverlaps(NativeArray<Overlap> Array)
        {
            for (int i = 0; i < Array.Length; i++)
            {
                if (Array[i].InfInf != new Vector2(0, 0))
                {
                    Vector3 II = new Vector3(Array[i].InfInf.x, 0, Array[i].InfInf.y);
                    Vector3 IS = new Vector3(Array[i].SupSup.x, 0, Array[i].InfInf.y);
                    Vector3 SI = new Vector3(Array[i].InfInf.x, 0, Array[i].SupSup.y);
                    Vector3 SS = new Vector3(Array[i].SupSup.x, 0, Array[i].SupSup.y);

                    if (i == 0)
                    {
                        Debug.DrawLine(II, IS, Color.cyan);
                        Debug.DrawLine(IS, SS, Color.cyan);
                        Debug.DrawLine(SS, SI, Color.cyan);
                        Debug.DrawLine(SI, II, Color.cyan);
                    }
                    else if (i == 1)
                    {
                        Debug.DrawLine(II + new Vector3(0, 1, 0), IS + new Vector3(0, 1, 0), Color.red);
                        Debug.DrawLine(IS + new Vector3(0, 1, 0), SS + new Vector3(0, 1, 0), Color.red);
                        Debug.DrawLine(SS + new Vector3(0, 1, 0), SI + new Vector3(0, 1, 0), Color.red);
                        Debug.DrawLine(SI + new Vector3(0, 1, 0), II + new Vector3(0, 1, 0), Color.red);
                    }
                    else
                    {
                        Debug.DrawLine(II + new Vector3(0, 2, 0), IS + new Vector3(0, 2, 0), Color.blue);
                        Debug.DrawLine(IS + new Vector3(0, 2, 0), SS + new Vector3(0, 2, 0), Color.blue);
                        Debug.DrawLine(SS + new Vector3(0, 2, 0), SI + new Vector3(0, 2, 0), Color.blue);
                        Debug.DrawLine(SI + new Vector3(0, 2, 0), II + new Vector3(0, 2, 0), Color.blue);
                    }
                }
            }
        }
        #endregion


        #region LoS Channel Calculation
        ParallelLoSDetection LoSDetection = new ParallelLoSDetection
        {
            CarsPositions = CarCoordinates,
            Links = Links,
            commands = commandsLoS,
        };
        JobHandle LoSDetectionHandle = LoSDetection.Schedule(2*link_num, 1);
        LoSDetectionHandle.Complete();
        // parallel raycasting
        JobHandle rayCastJobLoS = RaycastCommand.ScheduleBatch(commandsLoS, resultsLoS, 1, default);
        rayCastJobLoS.Complete();

        /*if (FrameCounter >= 200)
        {
            float buildingthinkness = (resultsLoS[0].point - resultsLoS[3].point).magnitude;
            Debug.Log("Car 1 = [" + CarCoordinates[0] + "]; Car 2 = [" + CarCoordinates[1] + "]");
            Debug.Log("The ray goes through buildings and covers = " + buildingthinkness + "m");
        }*/

        

        ParallelLoSChannel LoSChannel = new ParallelLoSChannel
        {
            OmniAntennaFlag = OmniAntenna,
            FFTSize = FFTNum,
            CarsPositions = CarCoordinates,
            CarsFwd = CarForwardVect,
            Links = Links,
            raycastresults = resultsLoS,
            inverseLambdas = InverseWavelengths,
            Pattern = Pattern,
            Corners = CornersNormalsPerpendiculars,

            HLoS = H_LoS,
        };
        JobHandle LoSChannelHandle = LoSChannel.Schedule(H_LoS.Length, 64);
        LoSChannelHandle.Complete();
        #endregion

        //Debug.Log("Car 1 hight " + CarCoordinates[0].y + "; Car 2 hight " + CarCoordinates[1].y);
        //if (H_LoS[0] != 0)
        //{

        //}

        Vector3 car1 = CarCoordinates[0];
        Vector3 corner1 = new Vector3(CornersNormalsPerpendiculars[3].x, car1.y, CornersNormalsPerpendiculars[3].z);
        Vector3 normal1 = CornersNormalsPerpendiculars[4]; // since we consider only two cars

        Vector3 car2 = CarCoordinates[1];
        Vector3 corner2 = new Vector3(CornersNormalsPerpendiculars[0].x, car2.y, CornersNormalsPerpendiculars[0].z); // we elevate the corner to the hight of the car
        Vector3 normal2 = CornersNormalsPerpendiculars[1];

        //
        // Knife edge effect calculating function (from Carl's paper with slight modification in angle calculation)
        Vector3 virtual_corner = (corner1 + corner2) / 2.0f;
        Vector3 virtual_normal = (normal1 + normal2) / 2.0f;
        Vector3 car1_corner = virtual_corner - car1;
        Vector3 car2_corner = virtual_corner - car2;

        //Vector3 heruistic_corner = new Vector3(53.9f, car1.y, -26.468f);
        // (53.64f, 0.0f, -26.89f)
        Vector3 heruistic_corner = new Vector3(CornersNormalsPerpendiculars[6].x, car1.y, CornersNormalsPerpendiculars[6].z);

        int corner_indicator = 1;
        if (corner_indicator == 1)
        {
            car1_corner = heruistic_corner - car1;
            car2_corner = heruistic_corner - car2;
        }
        

        float dist1 = (car1_corner).magnitude;
        float dist2 = (car2_corner).magnitude;

        Vector3 direction1 = car1_corner.normalized;
        Vector3 direction2 = car2_corner.normalized;

        // angles from Carl's matlab code
        // float phi = Mathf.Acos(Vector3.Dot(direction1, -direction2));
        float alpha1 = Mathf.PI / 2.0f - Mathf.Acos(Vector3.Dot(direction1, virtual_normal));
        float alpha2 = Mathf.PI / 2.0f - Mathf.Acos(Vector3.Dot(direction2, virtual_normal));
        float phi = alpha1 + alpha2;

        //float theta = Mathf.Acos(Vector3.Dot(direction1, -direction2));

        float nu = phi * Mathf.Sqrt(2.0f * (InverseWavelengths[1023]) / (1.0f / dist1 + 1.0f / dist2));

        float diff_K = 2.2131f; // it's 10^(6.9/20)
        float DiffractionCoefficient = 1.0f;
        if (nu > -0.78)
        {
            DiffractionCoefficient =  diff_K * (nu - 0.1f + Mathf.Sqrt((nu - 0.1f) * (nu - 0.1f) + 1.0f)); // the main formula for diffraction
        }
        //
        Debug.Log("phi = " + phi + "; nu = " + nu + "; Diffraction coefficient = " + 1 / DiffractionCoefficient);
        //Debug.Log("Diffraction coefficient = " + 1/DiffractionCoefficient + "; Ny = " + nu); 


        #region DMC and MPC1 Channel Parameters
        /*
        float t_upd = Time.realtimeSinceStartup;
        // Raycasting DMC
        ParallelRayCastingDataCars RayCastingData0 = new ParallelRayCastingDataCars
        {
            CastingDistance = maxdistance,
            Cars_Positions = CarCoordinates,
            MPC_Array = DMC_Native,
            MPC_Perpendiculars = DMC_perp,

            SoA = SoA0,
            SeenIndicator = Seen0,
            commands = commands0,
        };
        JobHandle jobHandle_RayCastingData0 = RayCastingData0.Schedule(DMC_num * car_num, 16);
        //jobHandle_RayCastingData0.Complete();

        // parallel raycasting
        JobHandle rayCastJob0 = RaycastCommand.ScheduleBatch(commands0, results0, 16, jobHandle_RayCastingData0);
        //rayCastJob0.Complete();


        // Channel parameters for DMCs
        NativeArray<Path> DMC_Paths = new NativeArray<Path>(link_num*DMC_num, Allocator.TempJob);
        NativeArray<Vector3Int> DMC_test = new NativeArray<Vector3Int>(link_num * DMC_num, Allocator.TempJob);
        ParallelChannelParameters1 dmc_channel_parameters = new ParallelChannelParameters1
        {
            MPC_attenuation = DMC_attenuation,
            MPC_array = DMC_Native,
            Links = Links,
            CarsPositions = CarCoordinates,
            CarsFwd = CarForwardVect,

            commands = commands0,
            raycastresults = results0,
            VisibilityIndicator = Seen0,

            TestArray = DMC_test,
            OutArray = DMC_Paths,
        };
        JobHandle dmc_channel_parameters_handle = dmc_channel_parameters.Schedule(DMC_Paths.Length, 5, rayCastJob0);
        //dmc_channel_parameters_handle.Complete();

        
        

        // Channel parameters for MPC1s (DMC and MPC1 parameters calculated similarly)
        // Raycasting DMC
        ParallelRayCastingDataCars RayCastingData1 = new ParallelRayCastingDataCars
        {
            CastingDistance = maxdistance,
            Cars_Positions = CarCoordinates,
            MPC_Array = MPC1_Native,
            MPC_Perpendiculars = MPC1_perp,

            SoA = SoA1,
            SeenIndicator = Seen1,
            commands = commands1,
        };
        JobHandle jobHandle_RayCastingData1 = RayCastingData1.Schedule(MPC1_num * car_num, 16, dmc_channel_parameters_handle);
        //jobHandle_RayCastingData1.Complete();

        // parallel raycasting
        JobHandle rayCastJob1 = RaycastCommand.ScheduleBatch(commands1, results1, 16, jobHandle_RayCastingData1);
        //rayCastJob1.Complete();

        NativeArray<Path> MPC1_Paths = new NativeArray<Path>(link_num * MPC1_num, Allocator.TempJob);
        NativeArray<Vector3Int> MPC1_test = new NativeArray<Vector3Int>(link_num * MPC1_num, Allocator.TempJob);
        ParallelChannelParameters1 mpc1_channel_parameters = new ParallelChannelParameters1
        {
            // general information for all
            Links = Links,
            CarsPositions = CarCoordinates,
            CarsFwd = CarForwardVect,

            // MPC1 related information
            MPC_attenuation = MPC1_attenuation,
            MPC_array = MPC1_Native,
            commands = commands1,
            raycastresults = results1,
            VisibilityIndicator = Seen1,

            // Output
            TestArray = MPC1_test,
            OutArray = MPC1_Paths,
        };
        JobHandle mpc1_channel_parameters_handle = mpc1_channel_parameters.Schedule(MPC1_Paths.Length, 5, rayCastJob1);
        mpc1_channel_parameters_handle.Complete();
        
        Debug.Log("Time spent for Raycasting: " + ((Time.realtimeSinceStartup - t_upd) * 1000f) + " ms");
        */

        #endregion





        float t_all = Time.realtimeSinceStartup;

        ParallelRayCastingDataCars RayCastingData = new ParallelRayCastingDataCars
        {
            CastingDistance = maxdistance,
            Cars_Positions = CarCoordinates,
            MPC_Array = MPC_Native,
            MPC_Perpendiculars = MPC_perp,

            SoA = SoA,
            SeenIndicator = Seen,
            commands = commands,
        };
        JobHandle jobHandle_RayCastingData = RayCastingData.Schedule(MPC_num * car_num, 16);
        //jobHandle_RayCastingData0.Complete();

        // parallel raycasting
        JobHandle rayCastJob = RaycastCommand.ScheduleBatch(commands, results, 16, jobHandle_RayCastingData);
        rayCastJob.Complete();

        var map = new NativeMultiHashMap<int, Path_and_IDs>(50000, Allocator.TempJob);
        var idarray = new NativeArray<int>(MPC_num*link_num, Allocator.TempJob);
        ChannelParametersAll channelParameters = new ChannelParametersAll
        {
            OmniAntennaFlag = OmniAntenna,
            MPC_Attenuation = MPC_attenuation,
            MPC_Array = MPC_Native,
            MPC_Perp = MPC_perp,
            DMCNum = DMC_num,
            MPC1Num = MPC1_num,
            MPC2Num = MPC2_num,
            MPC3Num = MPC3_num,
            MPCNum = MPC_num,

            LookUpTableMPC2 = LookUpTableMPC2,
            LUTIndexRangeMPC2 = MPC2LUTID,
            LookUpTableMPC3 = LookUpTableMPC3,
            LUTIndexRangeMPC3 = MPC3SeenID,

            ChannelLinks = Links,
            CarsCoordinates = CarCoordinates,
            CarsForwardsDir = CarForwardVect,

            Commands = commands,
            Results = results,
            SignOfArrival = SoA,
            SeenIndicator = Seen,
            
            Pattern = Pattern,

            IDArray = idarray,
            HashMap = map.AsParallelWriter(),
        };
        JobHandle channelParametersJob = channelParameters.Schedule(MPC_num * link_num, 4);
        channelParametersJob.Complete();

        #region Drwaing possible paths
        if (DrawingPath1 || DrawingPath2 || DrawingPath3)
        {
            for (int i = 0; i < MPC_num; i++)
            {
                if (map.TryGetFirstValue(i, out Path_and_IDs path, out NativeMultiHashMapIterator<int> nativeMultiHashMapIterator))
                {
                    do
                    {
                        if (path.PathOrder == 1 && DrawingPath1)
                        {
                            Vector3 car1_coor = CarCoordinates[path.ChainIDs.Car1];
                            Vector3 car2_coor = CarCoordinates[path.ChainIDs.Car2];
                            Vector3 MPC_coor1 = MPC_Native[path.ChainIDs.ID1].Coordinates;
                            Debug.DrawLine(car1_coor, MPC_coor1, Color.white);
                            Debug.DrawLine(MPC_coor1, car2_coor, Color.grey);
                        }
                        else if (path.PathOrder == 2 && DrawingPath2)
                        {
                            Vector3 car1_coor = CarCoordinates[path.ChainIDs.Car1];
                            Vector3 car2_coor = CarCoordinates[path.ChainIDs.Car2];
                            Vector3 MPC_coor1 = MPC_Native[path.ChainIDs.ID1].Coordinates;
                            Vector3 MPC_coor2 = MPC_Native[path.ChainIDs.ID2].Coordinates;
                            Debug.DrawLine(car1_coor, MPC_coor1, Color.white);
                            Debug.DrawLine(MPC_coor1, MPC_coor2, Color.white);
                            Debug.DrawLine(MPC_coor2, car2_coor, Color.white);
                        }
                        else if (path.PathOrder == 3 && DrawingPath3)
                        {
                            Vector3 car1_coor = CarCoordinates[path.ChainIDs.Car1];
                            Vector3 car2_coor = CarCoordinates[path.ChainIDs.Car2];
                            Vector3 MPC_coor1 = MPC_Native[path.ChainIDs.ID1].Coordinates;
                            Vector3 MPC_coor2 = MPC_Native[path.ChainIDs.ID2].Coordinates;
                            Vector3 MPC_coor3 = MPC_Native[path.ChainIDs.ID3].Coordinates;
                            Debug.DrawLine(car1_coor, MPC_coor1, Color.green);
                            Debug.DrawLine(MPC_coor1, MPC_coor2, Color.green);
                            Debug.DrawLine(MPC_coor2, MPC_coor3, Color.green);
                            Debug.DrawLine(MPC_coor3, car2_coor, Color.green);
                            //Debug.Log(20 * Mathf.Log10(path3.AngularGain));
                        }
                    }
                    while (map.TryGetNextValue(out path, ref nativeMultiHashMapIterator));
                }
            }
        }
        #endregion

        //float t_filt = Time.realtimeSinceStartup;
        NativeList<int> nonzero_indexes = new NativeList<int>(Allocator.TempJob);
        IndexNonZeroFilter nzindexes = new IndexNonZeroFilter
        {
            Array = idarray,
        };
        JobHandle jobHandleIndexNonZeroFilter = nzindexes.ScheduleAppend(nonzero_indexes, idarray.Length, 64);
        jobHandleIndexNonZeroFilter.Complete();

        NativeArray<Vector2Int> link_ids = new NativeArray<Vector2Int>(link_num, Allocator.TempJob);
        LinkIndexes linkIndexes = new LinkIndexes
        {
            MPCNum = MPC_num,
            NonZeroIndexes = nonzero_indexes,

            LinkIDs = link_ids,
        };
        JobHandle jobHandlelinkIndexes = linkIndexes.Schedule(link_num, 1, jobHandleIndexNonZeroFilter);
        //jobHandlelinkIndexes.Complete();
        //Debug.Log("Time spent for filtering: " + ((Time.realtimeSinceStartup - t_filt) * 1000f) + " ms");

        //float t_chan = Time.realtimeSinceStartup;
        ParallelChannel parallelChannel = new ParallelChannel
        {
            FFTSize = FFTNum,
            CarsPositions = CarCoordinates,
            CarsFwd = CarForwardVect,
            InverseLambdas = InverseWavelengths,
            HashMap = map,
            MPCNum = MPC_num,
            LinkNum = link_num,
            NonZeroIndexes = nonzero_indexes,
            LinkIDs = link_ids,

            H_NLoS = H_NLoS,
        };
        JobHandle parallelChannelJob = parallelChannel.Schedule(FFTNum * link_num, 1, jobHandlelinkIndexes);
        parallelChannelJob.Complete();
        //Debug.Log("Time spent for channel calculation: " + ((Time.realtimeSinceStartup - t_chan) * 1000f) + " ms");

        //Debug.Log("Time spent for Raycasting all at once: " + ((Time.realtimeSinceStartup - t_all) * 1000f) + " ms");

        #region FFT operation
        //float t_fft = Time.realtimeSinceStartup;
        System.Numerics.Complex[] outputSignal_Freq = FastFourierTransform.FFT(H, false);
        double RSS = 0;
        
        for (int i = 0; i < H.Length; i++)
        { H[i] = H_LoS[i] / DiffractionCoefficient + H_NLoS[i]; }

        Y_output = new double[H.Length];
        H_output = new double[H.Length];
        
        List<string> h_snapshot = new List<string>();
        List<string> H_snapshot = new List<string>();

        for (int i = 0; i < H.Length; i++)
        {
            
            Y_output[i] = 10 * Mathf.Log10( Mathf.Pow( (float)System.Numerics.Complex.Abs(outputSignal_Freq[i]), 2 ) + 0.0000000000001f);
            H_output[i] = 10 * Mathf.Log10( Mathf.Pow( (float)System.Numerics.Complex.Abs(H[i]), 2 ) + 0.0000000000001f);


            RSS += Mathf.Pow((float)System.Numerics.Complex.Abs(H_LoS[i]), 2);// * SubframePower;
            

            // procedure to write to a file
            string h_string = Mathf.Pow((float)System.Numerics.Complex.Abs(outputSignal_Freq[i]), 2).ToString();
            h_snapshot.Add(h_string); // channel in time domain

            // apparently, we need to convert a complex number to a string using such a weird method QUICK FIX
            
            string H_string;
            if (H[i].Imaginary > 0)
            {
                H_string = H[i].Real.ToString() + "+" + H[i].Imaginary.ToString() + "i";
                //double H_real = H[i].Real;
                //double H_imag = H[i].Imaginary;
            }
            else // in case of negative imaginary part
            {
                H_string = H[i].Real.ToString() + H[i].Imaginary.ToString() + "i";
            }
            H_snapshot.Add(H_string); // channel in frequence domain
            
        }

        /*
        if (RSS != 0)
        {
            //Debug.Log("RSS = " + 10 * Mathf.Log10((float)RSS));

            float test_distance = (CarCoordinates[0] - CarCoordinates[1]).magnitude;
            float test_gain = 10 * Mathf.Log10(Mathf.Pow( 1/(InverseWavelengths[0] * 4 * Mathf.PI * test_distance) , 2));
            Debug.Log("RSS = " + 10 * Mathf.Log10((float)RSS) + " dB; distance " + test_distance + "; path gain = " + test_gain);
        }
        */

        h_save.Add(h_snapshot);
        H_save.Add(H_snapshot);

        Drawing.drawChart(tfTime, X_inputValues, Y_output, "time");
        Drawing.drawChart(tfFreq, X_inputValues, H_output, "frequency");
        //Debug.Log("Time spent for FFT: " + ((Time.realtimeSinceStartup - t_fft) * 1000f) + " ms");
        #endregion

        link_ids.Dispose();
        nonzero_indexes.Dispose();
        map.Dispose();
        idarray.Dispose();

        //DMC_Paths.Dispose();
        //DMC_test.Dispose();
        //MPC1_Paths.Dispose();
        //MPC1_test.Dispose();
    }
    
   

    
}

[BurstCompile]
public struct IndexNonZeroFilter : IJobParallelForFilter
{
    public NativeArray<int> Array;
    public bool Execute(int index)
    {
        return Array[index] == 1;
    }
}

[BurstCompile]
public struct LinkIndexes : IJobParallelFor
{
    [ReadOnly] public int MPCNum;
    [ReadOnly] public NativeArray<int> NonZeroIndexes;

    [WriteOnly] public NativeArray<Vector2Int> LinkIDs;
    public void Execute(int link_id)
    {
        int min_i = NonZeroIndexes.Length;
        int max_i = 0;
        for (int i = 0; i < NonZeroIndexes.Length; i++)
        {
            if (NonZeroIndexes[i] >= link_id * MPCNum && i < min_i)
            { min_i = i; }
            if (NonZeroIndexes[i] <= (link_id + 1) * MPCNum && i > max_i)
            { max_i = i; }
        }
        LinkIDs[link_id] = new Vector2Int(min_i, max_i);
    }
}

[BurstCompile]
public struct ParallelChannel : IJobParallelFor
{
    [ReadOnly] public int FFTSize;
    [ReadOnly] public NativeArray<Vector3> CarsPositions;
    [ReadOnly] public NativeArray<Vector3> CarsFwd;
    //[ReadOnly] public NativeArray<Vector2Int> Links;
    [ReadOnly] public NativeArray<float> InverseLambdas;
    [ReadOnly] public NativeMultiHashMap<int, Path_and_IDs> HashMap;
    [ReadOnly] public int MPCNum;
    [ReadOnly] public int LinkNum;
    [ReadOnly] public NativeArray<int> NonZeroIndexes;
    [ReadOnly] public NativeArray<Vector2Int> LinkIDs;

    [WriteOnly] public NativeArray<System.Numerics.Complex> H_NLoS;
    public void Execute(int index)
    {
        int i_link = Mathf.FloorToInt(index / FFTSize);
        int i_sub = index - i_link * FFTSize; // calculating index within FFT array

        // defining zero temp value
        System.Numerics.Complex temp_HNLoS = new System.Numerics.Complex(0, 0);


        
        //for (int i = i_link * MPCNum; i < (i_link + 1) * MPCNum; i++)
        for (int i = LinkIDs[i_link].x; i <= LinkIDs[i_link].y; i++)
        {
            
            if (HashMap.TryGetFirstValue(NonZeroIndexes[i], out Path_and_IDs path, out NativeMultiHashMapIterator<int> nativeMultiHashMapIterator))
            {
                // if there are many paths, then sum them up
                do
                {
                    float HNLoS_dist = path.PathParameters.Distance;

                    //float frequence = 299792458.0f * InverseLambdas[i_sub];
                    //float inverse_lambda = InverseLambdas[i_sub];
                    //float test_pi = Mathf.PI;
                    

                    float HNLoS_dist_gain = 1.0f / (InverseLambdas[i_sub] * 4 * Mathf.PI * HNLoS_dist); // Free space loss
                    float HNLoS_attnuation = path.PathParameters.Attenuation;

                    //float test_arg = 2.0f * Mathf.PI * InverseLambdas[i_sub] * HNLoS_dist;

                    double ReExpNLoS = Mathf.Cos(2.0f * Mathf.PI * InverseLambdas[i_sub] * HNLoS_dist);
                    double ImExpNLoS = Mathf.Sin(2.0f * Mathf.PI * InverseLambdas[i_sub] * HNLoS_dist);
                    // defining exponent
                    System.Numerics.Complex ExpNLoS = new System.Numerics.Complex(ReExpNLoS, ImExpNLoS);

                    temp_HNLoS += HNLoS_attnuation * HNLoS_dist_gain * ExpNLoS;                    

                }
                while (HashMap.TryGetNextValue(out path, ref nativeMultiHashMapIterator));
            }
        }
        H_NLoS[index] = temp_HNLoS;        
    }
}

public struct Path_and_IDs
{
    public PathChain ChainIDs;
    public Path PathParameters;
    public int PathOrder;

    public Path_and_IDs(PathChain pathChain, Path pathParameters, int order)
    {
        ChainIDs = pathChain;
        PathParameters = pathParameters;
        PathOrder = order;
    }
}

public struct PathChain
{
    // for tracking the all paths inclusions
    public int Car1;
    public int ID1;
    public int ID2;
    public int ID3;
    public int Car2;
    public PathChain(int car1, int i1, int i2, int i3, int car2)
    {
        Car1 = car1;
        ID1 = i1;
        ID2 = i2;
        ID3 = i3;
        Car2 = car2;
    }
}

public struct Path
{
    public Vector2Int Car_IDs;
    public float Distance;
    public float Attenuation;
    public Path(Vector2Int link, float d, float att)
    {
        Car_IDs = link;
        Distance = d;
        Attenuation = att;
    }
}





[BurstCompile]
public struct ParallelRayCastingDataCars : IJobParallelFor
{
    [ReadOnly] public float CastingDistance;
    [ReadOnly] public NativeArray<Vector3> Cars_Positions;
    [ReadOnly] public NativeArray<V6> MPC_Array;
    [ReadOnly] public NativeArray<Vector3> MPC_Perpendiculars;

    [WriteOnly] public NativeArray<float> SoA;
    [WriteOnly] public NativeArray<int> SeenIndicator;
    [WriteOnly] public NativeArray<RaycastCommand> commands;
    public void Execute(int index)
    {
        int i_car = Mathf.FloorToInt(index / MPC_Array.Length);
        int i_mpc = index - i_car * MPC_Array.Length;
        Vector3 temp_direction = MPC_Array[i_mpc].Coordinates - Cars_Positions[i_car];

        float cosA = Vector3.Dot(MPC_Array[i_mpc].Normal, -temp_direction.normalized); // NOTE: the sign is negative (upd 24.02.2022: the sign of temp_direction)
        if (cosA > (float)0.1 && temp_direction.magnitude < CastingDistance)
        {
            //SoA[index] = Mathf.Sign(Vector3.Dot(MPC_Perpendiculars[i_mpc], -temp_direction.normalized));
            SoA[index] = Vector3.Dot(MPC_Perpendiculars[i_mpc], -temp_direction.normalized);
            SeenIndicator[index] = 1;
            commands[index] = new RaycastCommand(Cars_Positions[i_car], temp_direction.normalized, temp_direction.magnitude);
        }
        else
        {
            SoA[index] = 0;
            SeenIndicator[index] = 0;
            commands[index] = new RaycastCommand(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0);
        }

    }
}