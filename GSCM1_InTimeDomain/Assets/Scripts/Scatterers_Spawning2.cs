using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Linq;
using Unity.Burst;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

public class Scatterers_Spawning2 : MonoBehaviour
{
    // setting the area of the scenario
    public Vector3 bottom_left_corner;
    public float area_height;
    public float area_width;
    private readonly int IfVisualize = 1;

    

    // setting the scatterers spawning parameters
    public float chiW1; // = 0.044; %W1 is first order
    public float chiW2; // = 1.5*0.044; %W2 is second order
    public float chiW3; // = 2*0.044; %W3 is third order
    public float chiD;

    public bool DrawMPCs;
    
    public GameObject MPC1_Prefab;
    public GameObject MPC2_Prefab;
    public GameObject MPC3_Prefab;
    public GameObject DMC_Prefab;
    

    // setting materials
    // public Material ObsPoint_Material;

    // subset of the seen MPCs
    [HideInInspector] public List<Vector3> MPC1_possiblepositionList = new List<Vector3>();
    [HideInInspector] public List<Vector3> MPC2_possiblepositionList = new List<Vector3>();
    [HideInInspector] public List<Vector3> MPC3_possiblepositionList = new List<Vector3>();
    [HideInInspector] public List<Vector3> DMC_possiblepositionList = new List<Vector3>();

    [HideInInspector] public NativeList<V4> MPC1_possiblepositionNativeList;
    [HideInInspector] public NativeList<V4> MPC2_possiblepositionNativeList;
    [HideInInspector] public NativeList<V4> MPC3_possiblepositionNativeList;
    [HideInInspector] public NativeList<V4> DMC_possiblepositionNativeList;
    [HideInInspector] public NativeList<V5> MPC_possiblepositionNativeList;

    [HideInInspector] public NativeArray<Vector3> Observation_Points_NativeArray;

    NativeArray<Unity.Mathematics.Random> _rngs; // this is used in random numbers generation
    private void OnEnable()
    {
        MPC1_possiblepositionNativeList = new NativeList<V4>(Allocator.Persistent);
        MPC2_possiblepositionNativeList = new NativeList<V4>(Allocator.Persistent);
        MPC3_possiblepositionNativeList = new NativeList<V4>(Allocator.Persistent);
        DMC_possiblepositionNativeList = new NativeList<V4>(Allocator.Persistent);

        MPC_possiblepositionNativeList = new NativeList<V5>(Allocator.Persistent);

        Observation_Points = GameObject.FindGameObjectsWithTag("Observation_Points");
        Observation_Points_NativeArray = new NativeArray<Vector3>(Observation_Points.Length, Allocator.Persistent);

        // this solution comes from https://forum.unity.com/threads/mathematics-random-with-in-ijobprocesscomponentdata.598192/#post-4009273
        _rngs = new NativeArray<Unity.Mathematics.Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        { _rngs[i] = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue)); }
    }
    private void OnDestroy()
    {
        MPC1_possiblepositionNativeList.Dispose();
        MPC2_possiblepositionNativeList.Dispose();
        MPC3_possiblepositionNativeList.Dispose();
        DMC_possiblepositionNativeList.Dispose();

        MPC_possiblepositionNativeList.Dispose();

        Observation_Points_NativeArray.Dispose();
        
        _rngs.Dispose();
    }



    //private GameObject[] Buildings;
    private GameObject[] Observation_Points;

    void Start()
    {
        float t_spawning = Time.realtimeSinceStartup;
        // Declaring observation points
        
        int num_OBS = Observation_Points.Length;
        
        for (int i = 0; i < num_OBS; i++)
        {
            Observation_Points_NativeArray[i] = Observation_Points[i].transform.position;
        }

        

        float Size_Whole_Area = area_height * area_width;
        Debug.Log("Size of the Area = " + Size_Whole_Area + " square meters");

        Vector3 z_dir = new Vector3(0, 0, 1);
        Vector3 x_dir = new Vector3(1, 0, 0);
        Vector3 corner1 = bottom_left_corner;
        Vector3 corner2 = corner1 + area_height * z_dir;
        Vector3 corner3 = corner1 + area_height * z_dir + area_width * x_dir;
        Vector3 corner4 = corner1 + area_width * x_dir;

        if (IfVisualize == 1)
        {
            Debug.DrawLine(corner1, corner2, Color.white, 5.0f);
            Debug.DrawLine(corner2, corner3, Color.white, 5.0f);
            Debug.DrawLine(corner3, corner4, Color.white, 5.0f);
            Debug.DrawLine(corner4, corner1, Color.white, 5.0f);
        }


        int num_MPC1 = Mathf.FloorToInt(Size_Whole_Area * chiW1);
        int num_MPC2 = Mathf.FloorToInt(Size_Whole_Area * chiW2);
        int num_MPC3 = Mathf.FloorToInt(Size_Whole_Area * chiW3);
        int num_DMC  = Mathf.FloorToInt(Size_Whole_Area * chiD);

        // All in one
        int all_MPC = num_MPC1 + num_MPC2 + num_MPC3 + num_DMC;
        NativeArray<V5> MPC_possiblepositionNativeArray = new NativeArray<V5>(all_MPC, Allocator.TempJob);

        NativeList<JobHandle> list_of_jobs_all = new NativeList<JobHandle>(Allocator.Temp);
        NativeArray<RaycastCommand> CommandsNativeArray_MPC = new NativeArray<RaycastCommand>(all_MPC * num_OBS, Allocator.TempJob);
        NativeArray<RaycastHit> ResultsNativeArray_MPC = new NativeArray<RaycastHit>(all_MPC * num_OBS, Allocator.TempJob);
        
        // End all in one

        NativeArray<V4> MPC1_possiblepositionNativeArray = new NativeArray<V4>(num_MPC1, Allocator.TempJob);
        NativeArray<V4> MPC2_possiblepositionNativeArray = new NativeArray<V4>(num_MPC2, Allocator.TempJob);
        NativeArray<V4> MPC3_possiblepositionNativeArray = new NativeArray<V4>(num_MPC3, Allocator.TempJob);
        NativeArray<V4> DMC_possiblepositionNativeArray = new NativeArray<V4>(num_DMC, Allocator.TempJob);
        
        NativeList<JobHandle> list_of_jobs = new NativeList<JobHandle>(Allocator.Temp);
        NativeList<JobHandle> list_of_jobs_RayCastData = new NativeList<JobHandle>(Allocator.Temp);
        NativeList<JobHandle> list_of_jobs_RayCast = new NativeList<JobHandle>(Allocator.Temp);

        // data for parallel raycasting
        // MPC1
        NativeArray<RaycastCommand> CommandsNativeArray_MPC1 = new NativeArray<RaycastCommand>(num_MPC1 * num_OBS, Allocator.TempJob);
        NativeArray<RaycastHit> ResultsNativeArray_MPC1 = new NativeArray<RaycastHit>(num_MPC1 * num_OBS, Allocator.TempJob);
        // MPC2
        NativeArray<RaycastCommand> CommandsNativeArray_MPC2 = new NativeArray<RaycastCommand>(num_MPC2 * num_OBS, Allocator.TempJob);
        NativeArray<RaycastHit> ResultsNativeArray_MPC2 = new NativeArray<RaycastHit>(num_MPC2 * num_OBS, Allocator.TempJob);
        // MPC3
        NativeArray<RaycastCommand> CommandsNativeArray_MPC3 = new NativeArray<RaycastCommand>(num_MPC3 * num_OBS, Allocator.TempJob);
        NativeArray<RaycastHit> ResultsNativeArray_MPC3 = new NativeArray<RaycastHit>(num_MPC3 * num_OBS, Allocator.TempJob);
        // DMC
        NativeArray<RaycastCommand> CommandsNativeArray_DMC = new NativeArray<RaycastCommand>(num_DMC * num_OBS, Allocator.TempJob);
        NativeArray<RaycastHit> ResultsNativeArray_DMC = new NativeArray<RaycastHit>(num_DMC * num_OBS, Allocator.TempJob);

        //float startTime_upd_all = Time.realtimeSinceStartup;
        /*
        ParallelMPCSpawnerAll parallelMPC1SpawnerAll = new ParallelMPCSpawnerAll
        {
            Corner_bottom_left = bottom_left_corner,
            Width = area_width,
            Height = area_height,
            
            MPC1_quantity = num_MPC1,
            MPC2_quantity = num_MPC2,
            MPC3_quantity = num_MPC3,
            DMC_quantity = num_DMC,
            // taken from the website look above
            rngs = _rngs,
            Array = MPC_possiblepositionNativeArray,
        };

        JobHandle jobHandle_MPCSpawner = parallelMPC1SpawnerAll.Schedule(all_MPC, 64);
        list_of_jobs_all.Add(jobHandle_MPCSpawner);

        ParallelRayCastingV5 parallelRayCastingV5_MPC = new ParallelRayCastingV5
        {
            MPC_Array = MPC_possiblepositionNativeArray,
            OBS_Array = Observation_Points_NativeArray,

            commands = CommandsNativeArray_MPC,
        };
        JobHandle jobHandle_RayCastingV5_MPC = parallelRayCastingV5_MPC.Schedule(all_MPC * Observation_Points_NativeArray.Length, 64, jobHandle_MPCSpawner);
        list_of_jobs_all.Add(jobHandle_RayCastingV5_MPC);
        JobHandle.CompleteAll(list_of_jobs_all);

        JobHandle RayCastJobMPC = RaycastCommand.ScheduleBatch(CommandsNativeArray_MPC, ResultsNativeArray_MPC, 5, default);
        RayCastJobMPC.Complete();


        
        int mpc1count = 0;
        int mpc2count = 0;
        int mpc3count = 0;
        int dmccount = 0;
        
        for (int i = 0; i < all_MPC; i++)
        {
            int temp_inclusion = 0;
            for (int j = 0; j < num_OBS; j++)
            {
                if (ResultsNativeArray_MPC[j * all_MPC + i].collider == null)
                { temp_inclusion = 1; }
            }
            if (temp_inclusion == 1)
            {
                MPC_possiblepositionNativeList.Add(MPC_possiblepositionNativeArray[i]);
                
                if (MPC_possiblepositionNativeArray[i].MPC_order == 1)
                { GameObject MPC11_clone = Instantiate(MPC1_Prefab, MPC_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); mpc1count += 1; }
                else if (MPC_possiblepositionNativeArray[i].MPC_order == 2)
                { GameObject MPC22_clone = Instantiate(MPC2_Prefab, MPC_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); mpc2count += 1; }
                else if (MPC_possiblepositionNativeArray[i].MPC_order == 3)
                { GameObject MPC33_clone = Instantiate(MPC3_Prefab, MPC_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); mpc3count += 1; }
                else
                { GameObject DMC_clone = Instantiate(DMC_Prefab, MPC_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); dmccount += 1; }
                
            }
        }
        //Debug.Log("MPC1 " + mpc1count + "; MPC2 " + mpc2count + "; MPC3 " + mpc3count + "; DMC " + dmccount);

        //Debug.Log("Check Time Upd All: " + ((Time.realtimeSinceStartup - startTime_upd_all) * 1000f) + " ms");
        */


        //float startTime_upd = Time.realtimeSinceStartup;
        // MPC1
        ParallelMPCSpawner parallelMPC1Spawner = new ParallelMPCSpawner
        {
            Corner_bottom_left = bottom_left_corner,
            Width = area_width,
            Height = area_height,
            
            // taken from the website look above
            rngs = _rngs,
            Array = MPC1_possiblepositionNativeArray,
        };

        JobHandle jobHandle_MPC1Spawner = parallelMPC1Spawner.Schedule(num_MPC1, 1);
        //jobHandle_MPC1Spawner.Complete();
        list_of_jobs.Add(jobHandle_MPC1Spawner);



        // MPC2
        ParallelMPCSpawner parallelMPC2Spawner = new ParallelMPCSpawner
        {
            Corner_bottom_left = bottom_left_corner,
            Width = area_width,
            Height = area_height,
            
            // taken from the website look above
            rngs = _rngs,
            Array = MPC2_possiblepositionNativeArray,
        };

        JobHandle jobHandle_MPC2Spawner = parallelMPC2Spawner.Schedule(num_MPC2, 64, jobHandle_MPC1Spawner);
        //jobHandle_MPC2Spawner.Complete();
        list_of_jobs.Add(jobHandle_MPC2Spawner);

        // MPC3
        ParallelMPCSpawner parallelMPC3Spawner = new ParallelMPCSpawner
        {
            Corner_bottom_left = bottom_left_corner,
            Width = area_width,
            Height = area_height,

            // taken from the website look above
            rngs = _rngs,
            Array = MPC3_possiblepositionNativeArray,
        };

        JobHandle jobHandle_MPC3Spawner = parallelMPC3Spawner.Schedule(num_MPC3, 64, jobHandle_MPC2Spawner);
        //jobHandle_MPC3Spawner.Complete();
        list_of_jobs.Add(jobHandle_MPC3Spawner);

        // DMC
        ParallelMPCSpawner parallelDMCSpawner = new ParallelMPCSpawner
        {
            Corner_bottom_left = bottom_left_corner,
            Width = area_width,
            Height = area_height,

            // taken from the website look above
            rngs = _rngs,
            Array = DMC_possiblepositionNativeArray,
        };

        JobHandle jobHandle_DMCSpawner = parallelDMCSpawner.Schedule(num_DMC, 64, jobHandle_MPC3Spawner);
        //jobHandle_DMCSpawner.Complete();
        list_of_jobs.Add(jobHandle_DMCSpawner);
        JobHandle.CompleteAll(list_of_jobs);


        // Preparing data
        // MPC1
        ParallelRayCastingData parallelRayCastingData_MPC1 = new ParallelRayCastingData
        {
            MPC_Array = MPC1_possiblepositionNativeArray,
            OBS_Array = Observation_Points_NativeArray,

            commands = CommandsNativeArray_MPC1,
        };
        JobHandle jobHandle_RayCastingData_MPC1 = parallelRayCastingData_MPC1.Schedule(num_MPC1 * Observation_Points_NativeArray.Length, 64, jobHandle_DMCSpawner);
        // MPC2
        ParallelRayCastingData parallelRayCastingData_MPC2 = new ParallelRayCastingData
        {
            MPC_Array = MPC2_possiblepositionNativeArray,
            OBS_Array = Observation_Points_NativeArray,

            commands = CommandsNativeArray_MPC2,
        };
        JobHandle jobHandle_RayCastingData_MPC2 = parallelRayCastingData_MPC2.Schedule(num_MPC2 * Observation_Points_NativeArray.Length, 64, jobHandle_DMCSpawner);
        // MPC3
        ParallelRayCastingData parallelRayCastingData_MPC3 = new ParallelRayCastingData
        {
            MPC_Array = MPC3_possiblepositionNativeArray,
            OBS_Array = Observation_Points_NativeArray,

            commands = CommandsNativeArray_MPC3,
        };
        JobHandle jobHandle_RayCastingData_MPC3 = parallelRayCastingData_MPC3.Schedule(num_MPC3 * Observation_Points_NativeArray.Length, 64, jobHandle_DMCSpawner);
        // DMC
        ParallelRayCastingData parallelRayCastingData_DMC = new ParallelRayCastingData
        {
            MPC_Array = DMC_possiblepositionNativeArray,
            OBS_Array = Observation_Points_NativeArray,

            commands = CommandsNativeArray_DMC,
        };
        JobHandle jobHandle_RayCastingData_DMC = parallelRayCastingData_DMC.Schedule(num_DMC * Observation_Points_NativeArray.Length, 64, jobHandle_DMCSpawner);
        
        // preparing list of jobs
        list_of_jobs_RayCastData.Add(jobHandle_RayCastingData_MPC1);
        list_of_jobs_RayCastData.Add(jobHandle_RayCastingData_MPC2);
        list_of_jobs_RayCastData.Add(jobHandle_RayCastingData_MPC3);
        list_of_jobs_RayCastData.Add(jobHandle_RayCastingData_DMC);
        JobHandle.CompleteAll(list_of_jobs_RayCastData);

        JobHandle RayCastJobMPC1 = RaycastCommand.ScheduleBatch(CommandsNativeArray_MPC1, ResultsNativeArray_MPC1, 5, default);
        JobHandle RayCastJobMPC2 = RaycastCommand.ScheduleBatch(CommandsNativeArray_MPC2, ResultsNativeArray_MPC2, 5, default);
        JobHandle RayCastJobMPC3 = RaycastCommand.ScheduleBatch(CommandsNativeArray_MPC3, ResultsNativeArray_MPC3, 5, default);
        JobHandle RayCastJobDMC = RaycastCommand.ScheduleBatch(CommandsNativeArray_DMC, ResultsNativeArray_DMC, 5, default);
        list_of_jobs_RayCast.Add(RayCastJobMPC1);
        list_of_jobs_RayCast.Add(RayCastJobMPC2);
        list_of_jobs_RayCast.Add(RayCastJobMPC3);
        list_of_jobs_RayCast.Add(RayCastJobDMC);
        JobHandle.CompleteAll(list_of_jobs_RayCast);

        // generating MPC1 native_list
        for (int i = 0; i < num_MPC1; i++)
        {
            int temp_inclusion = 0;
            for (int j = 0; j < num_OBS; j++)
            {
                if (ResultsNativeArray_MPC1[ j*num_MPC1 + i ].collider == null)
                { temp_inclusion = 1; }
            }
            if (temp_inclusion == 1)
            { 
                MPC1_possiblepositionNativeList.Add(MPC1_possiblepositionNativeArray[i]);
                MPC1_possiblepositionList.Add(MPC1_possiblepositionNativeArray[i].Coordinates);
                if (DrawMPCs == true)
                { Instantiate(MPC1_Prefab, MPC1_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); }
            }
        }
        // generating MPC2 native_list
        for (int i = 0; i < num_MPC2; i++)
        {
            int temp_inclusion = 0;
            for (int j = 0; j < num_OBS; j++)
            {
                if (ResultsNativeArray_MPC2[j * num_MPC2 + i].collider == null)
                { temp_inclusion = 1; }
            }
            if (temp_inclusion == 1)
            { 
                MPC2_possiblepositionNativeList.Add(MPC2_possiblepositionNativeArray[i]);
                MPC2_possiblepositionList.Add(MPC2_possiblepositionNativeArray[i].Coordinates);
                if (DrawMPCs == true)
                { Instantiate(MPC2_Prefab, MPC2_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); }
            }
        }
        // generating MPC3 native_list
        for (int i = 0; i < num_MPC3; i++)
        {
            int temp_inclusion = 0;
            for (int j = 0; j < num_OBS; j++)
            {
                if (ResultsNativeArray_MPC3[j * num_MPC3 + i].collider == null)
                { temp_inclusion = 1; }
            }
            if (temp_inclusion == 1)
            { 
                MPC3_possiblepositionNativeList.Add(MPC3_possiblepositionNativeArray[i]);
                MPC3_possiblepositionList.Add(MPC3_possiblepositionNativeArray[i].Coordinates);
                if (DrawMPCs == true)
                { Instantiate(MPC3_Prefab, MPC3_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); }
            }
        }
        // generating DMC native_list
        for (int i = 0; i < num_DMC; i++)
        {
            int temp_inclusion = 0;
            for (int j = 0; j < num_OBS; j++)
            {
                if (ResultsNativeArray_DMC[j * num_DMC + i].collider == null)
                { temp_inclusion = 1; }
            }
            if (temp_inclusion == 1)
            { 
                DMC_possiblepositionNativeList.Add(DMC_possiblepositionNativeArray[i]);
                DMC_possiblepositionList.Add(DMC_possiblepositionNativeArray[i].Coordinates);
                if (DrawMPCs == true)
                { Instantiate(DMC_Prefab, DMC_possiblepositionNativeArray[i].Coordinates, Quaternion.identity); }
            }
        }

        //Debug.Log("Check Time Upd: " + ((Time.realtimeSinceStartup - startTime_upd) * 1000f) + " ms");


        Debug.Log("On the roads: MPC1s " + MPC1_possiblepositionNativeList.Length + "; MPC2s " + MPC2_possiblepositionNativeList.Length + "; MPC3s " + MPC3_possiblepositionNativeList.Length + "; DMCs " + DMC_possiblepositionNativeList.Length);

        MPC1_possiblepositionNativeArray.Dispose();
        MPC2_possiblepositionNativeArray.Dispose();
        MPC3_possiblepositionNativeArray.Dispose();
        DMC_possiblepositionNativeArray.Dispose();
        list_of_jobs.Dispose();

        MPC_possiblepositionNativeArray.Dispose();
        list_of_jobs_all.Dispose();
        CommandsNativeArray_MPC.Dispose();
        ResultsNativeArray_MPC.Dispose();

        //Observation_Points_NativeArray.Dispose();

        CommandsNativeArray_MPC1.Dispose();
        CommandsNativeArray_MPC2.Dispose();
        CommandsNativeArray_MPC3.Dispose();
        CommandsNativeArray_DMC.Dispose();
        ResultsNativeArray_MPC1.Dispose();
        ResultsNativeArray_MPC2.Dispose();
        ResultsNativeArray_MPC3.Dispose();
        ResultsNativeArray_DMC.Dispose();
        list_of_jobs_RayCastData.Dispose();
        list_of_jobs_RayCast.Dispose();

        Debug.Log("Time spent for spawning: " + ((Time.realtimeSinceStartup - t_spawning) * 1000f) + " ms");
    }
}

public struct V4
{
    public Vector3 Coordinates;
    public int Inclusion;

    public V4(Vector3 vec, int inorout)
    {
        Coordinates = vec;
        Inclusion = inorout;
    }
}

public struct V5
{
    public Vector3 Coordinates;
    public int MPC_order;
    public int Inclusion;

    public V5(Vector3 vec, int order, int inorout)
    {
        Coordinates = vec;
        MPC_order = order;
        Inclusion = inorout;
    }
}

[BurstCompile]
public struct ParallelRayCastingV5 : IJobParallelFor
{
    [ReadOnly] public NativeArray<V5> MPC_Array;
    [ReadOnly] public NativeArray<Vector3> OBS_Array;

    [WriteOnly] public NativeArray<RaycastCommand> commands;
    public void Execute(int index)
    {
        int i_obs = Mathf.FloorToInt(index / MPC_Array.Length);
        int i_mpc = index - i_obs * MPC_Array.Length;

        Vector3 temp_direction = MPC_Array[i_mpc].Coordinates - OBS_Array[i_obs];
        commands[index] = new RaycastCommand(OBS_Array[i_obs], temp_direction.normalized, temp_direction.magnitude);
    }
}

[BurstCompile]
public struct RayCastHitProcessing : IJobParallelForFilter
{
    public NativeArray<RaycastHit> HitArray;
    public int RepetitionNumber;
    public int MPCNumber;
    public bool Execute(int index)
    {
        int inclusion = 0;
        for (int i = 0; i < RepetitionNumber; i++)
        {
            if (HitArray[i * MPCNumber + index].distance == 0)
            { inclusion = 1; break; }
        }
        return (inclusion == 1);
    }
}
