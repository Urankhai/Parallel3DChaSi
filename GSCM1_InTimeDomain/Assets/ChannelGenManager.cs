using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.IO;
using System;
using UnityEditor;
using Vector3 = UnityEngine.Vector3;

public partial class ChannelGenManager : MonoBehaviour
{
    int FrameCounter;
    int NeigbouringCount;
    private float Timer;
    private float dTimter;
    
    [Space]
    [Header("PHYSICAL PARAMETERS")]
    [Space]
    public float PowerInMilliWatts;
    public float SpeedofLight = 299792458.0f; // m/s
    public float CarrierFrequency = (float)(5.9 * Mathf.Pow(10, 9)); // GHz
    public float fsubcarriers = (float)0.001E9; // kHz

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
    NativeArray<float> CarsSpeed;

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
    

    NativeArray<float> SoA;
    NativeArray<int> Seen; // either 0 or 1
    NativeArray<RaycastCommand> commands; // for DMCs
    NativeArray<RaycastHit> results; // for DMCs

    static readonly int FFTNum = 311;
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
        
        //using (var reader = new StreamReader(@"C:\Users\Administrator\Desktop\Aleksei\Parallel3DChaSi\GSCM1_InTimeDomain\Assets\EADF\HelixV2VEADF.csv"))
        using (var reader = new StreamReader(@"Assets/EADF/HelixV2VEADF.csv"))
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
        
        SoA.Dispose();
        Seen.Dispose();
        commands.Dispose();
        results.Dispose();

        Subcarriers.Dispose();
        InverseWavelengths.Dispose();
        H_LoS.Dispose();
        
        H_NLoS.Dispose();
    }

    

    void Start()
    {
        FrameCounter = 0;
        NeigbouringCount = 0;
        Timer = 0f;
        // for Fourier transform
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
        H_NLoS = new NativeArray<System.Numerics.Complex>(FFTNum * link_num, Allocator.Persistent);


        //EdgeEffectCoeff = 10.0f;
    }

    private void FixedUpdate()
    //void Update()
    {
        FrameCounter++;
        dTimter = Time.realtimeSinceStartup - Timer;
        Timer = Time.realtimeSinceStartup;
        
        

        if (Mathf.Abs(122.0f - CarCoordinates[1].x) < 1.0f)
        {
            Debug.Log("Frame number = " + FrameCounter);
            NeigbouringCount++;
            if (NeigbouringCount == 1)
            {
                Debug.Log("Frame number = " + FrameCounter);
                
                string path = @"Assets/Measurements/";
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


        if (resultsLoS[0].distance == 0)
        {
            Debug.DrawLine(CarCoordinates[0], CarCoordinates[1], Color.yellow);
        }

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

        var map = new NativeMultiHashMap<int, Path_and_IDs>(1000000, Allocator.TempJob);
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
                        if (path.PathOrder == 3 && DrawingPath3)
                        {
                            Vector3 car1_coor = CarCoordinates[path.ChainIDs.Car1];
                            Vector3 car2_coor = CarCoordinates[path.ChainIDs.Car2];
                            Vector3 MPC_coor1 = MPC_Native[path.ChainIDs.ID1].Coordinates;
                            Vector3 MPC_coor2 = MPC_Native[path.ChainIDs.ID2].Coordinates;
                            Vector3 MPC_coor3 = MPC_Native[path.ChainIDs.ID3].Coordinates;
                            Debug.DrawLine(car1_coor, MPC_coor1, new Color(0.0f, 0.76f, 0.0f,0.3f));
                            Debug.DrawLine(MPC_coor1, MPC_coor2, new Color(0.0f, 0.76f, 0.0f,0.3f));
                            Debug.DrawLine(MPC_coor2, MPC_coor3, new Color(0.0f, 0.76f, 0.0f,0.3f));
                            Debug.DrawLine(MPC_coor3, car2_coor, new Color(0.0f, 0.76f, 0.0f,0.3f));
                            //Debug.Log(20 * Mathf.Log10(path3.AngularGain));
                        }
                        else if (path.PathOrder == 2 && DrawingPath2)
                        {
                            Vector3 car1_coor = CarCoordinates[path.ChainIDs.Car1];
                            Vector3 car2_coor = CarCoordinates[path.ChainIDs.Car2];
                            Vector3 MPC_coor1 = MPC_Native[path.ChainIDs.ID1].Coordinates;
                            Vector3 MPC_coor2 = MPC_Native[path.ChainIDs.ID2].Coordinates;
                            Debug.DrawLine(car1_coor, MPC_coor1, new Color(0.0f, 1.0f, 1.0f, 0.3f));
                            Debug.DrawLine(MPC_coor1, MPC_coor2, new Color(0.0f, 1.0f, 1.0f, 0.3f));
                            Debug.DrawLine(MPC_coor2, car2_coor, new Color(0.0f, 1.0f, 1.0f, 0.3f));
                        }
                        else if (path.PathOrder == 1 && DrawingPath1)
                        {
                            Vector3 car1_coor = CarCoordinates[path.ChainIDs.Car1];
                            Vector3 car2_coor = CarCoordinates[path.ChainIDs.Car2];
                            Vector3 MPC_coor1 = MPC_Native[path.ChainIDs.ID1].Coordinates;
                            Debug.DrawLine(car1_coor, MPC_coor1, new Color(1.0f, 0.76f, 0.0f));
                            Debug.DrawLine(MPC_coor1, car2_coor, new Color(1.0f, 0.76f, 0.0f));
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
        System.Numerics.Complex[] outputSignal_Freq = FastFourierTransform.FFT(H, true);
        double RSS = 0;
        
        for (int i = 0; i < H.Length; i++)
        { H[i] = H_LoS[i] + H_NLoS[i]; }

        Y_output = new double[H.Length];
        H_output = new double[H.Length];
        
        List<string> h_snapshot = new List<string>();
        List<string> H_snapshot = new List<string>();

        string Tx_x = CarCoordinates[0].x.ToString();
        string Tx_y = CarCoordinates[0].z.ToString();
        string Rx_x = CarCoordinates[1].x.ToString();
        string Rx_y = CarCoordinates[1].z.ToString();
        H_snapshot.Add(Tx_x);
        H_snapshot.Add(Tx_y);
        H_snapshot.Add(Rx_x);
        H_snapshot.Add(Rx_y);

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
            if (H[i].Imaginary >= 0)
            {
                H_string = H[i].Real.ToString() + "+" + H[i].Imaginary.ToString() + "i";
                //H_string = H_LoS[i].Real.ToString() + "+" + H_LoS[i].Imaginary.ToString() + "i";
                //double H_real = H[i].Real;
                //double H_imag = H[i].Imaginary;
            }
            else // in case of negative imaginary part
            {
                H_string = H[i].Real.ToString() + H[i].Imaginary.ToString() + "i";
                //H_string = H_LoS[i].Real.ToString() + H_LoS[i].Imaginary.ToString() + "i";
            }
            H_snapshot.Add(H_string); // channel in frequence domain
            
        }
        
        //H_snapshot.Insert(0, Timer.ToString());

        /*
        if (RSS != 0)
        {
            //Debug.Log("RSS = " + 10 * Mathf.Log10((float)RSS));

            float test_distance = (CarCoordinates[0] - CarCoordinates[1]).magnitude;
            float test_gain = 10 * Mathf.Log10(Mathf.Pow( 1/(InverseWavelengths[0] * 4 * Mathf.PI * test_distance) , 2));
            Debug.Log("RSS = " + 10 * Mathf.Log10((float)RSS) + " dB; distance " + test_distance + "; path gain = " + test_gain);
        }
        */
        if (FrameCounter > 1)
        {
            h_save.Add(h_snapshot);
            H_save.Add(H_snapshot);
        }

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

                    Vector3 vectorBetweenCars = new Vector3(CarsPositions[0].x - CarsPositions[1].x, CarsPositions[0].y - CarsPositions[1].y, CarsPositions[0].z - CarsPositions[1].z);
                    float distanceBetweenCars = vectorBetweenCars.magnitude;

                    

                    //float frequence = 299792458.0f * InverseLambdas[i_sub];
                    //float inverse_lambda = InverseLambdas[i_sub];
                    //float test_pi = Mathf.PI;
                    

                    //float HNLoS_dist_gain = 1.0f / (InverseLambdas[i_sub] * 4 * Mathf.PI * HNLoS_dist); // Free space loss
                    float HNLoS_dist_gain = 1.0f / (HNLoS_dist); // Removed the wavelength similar to Carl's Matlab script
                    // powWi = repmat(env.g02Wi,1,length(T)) - 20*log10(d_Wi);
                    float HNLoS_attnuation = path.PathParameters.Attenuation;

                    //float test_arg = 2.0f * Mathf.PI * InverseLambdas[i_sub] * HNLoS_dist;

                    double ReExpNLoS = Mathf.Cos(2.0f * Mathf.PI * InverseLambdas[i_sub] * HNLoS_dist);
                    double ImExpNLoS = -Mathf.Sin(2.0f * Mathf.PI * InverseLambdas[i_sub] * HNLoS_dist);
                    // defining exponent
                    System.Numerics.Complex ExpNLoS = new System.Numerics.Complex(ReExpNLoS, ImExpNLoS);

                    temp_HNLoS += HNLoS_attnuation * HNLoS_dist_gain * ExpNLoS;

                    //float test_stop = 1.0f;

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