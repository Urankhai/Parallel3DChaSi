using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using System.Linq;
using Unity.Entities;

public class LookUpTableGenUpd : MonoBehaviour
{
    //public int MaxSeenMPC2 = 20;
    //public int MaxSeenMPC3 = 20;
    public float MaxSeenDistance = 300;
    public int maxlengthMPC2 = 0;
    public int maxlengthMPC3 = 0;
    [HideInInspector] public NativeArray<SeenPath2> LookUpTableMPC2; // this array shows all properties of the path2s
    [HideInInspector] public NativeArray<Vector2Int> MPC2LUTID; // this array shows how many paths come from each MPC2

    [HideInInspector] public NativeArray<SeenPath3> LookUpTableMPC3;
    [HideInInspector] public NativeArray<Vector2Int> MPC3SeenID;
    // Start is called before the first frame update

    private void OnDestroy()
    {
        LookUpTableMPC2.Dispose();
        MPC2LUTID.Dispose();

        LookUpTableMPC3.Dispose();
        MPC3SeenID.Dispose();
    }
    void Start()
    {
        GameObject MPC_Spawner = GameObject.Find("CorrectPolygons");
        Correcting_polygons_Native MPC_Native_Script = MPC_Spawner.GetComponent<Correcting_polygons_Native>();

        int MPC2_num = MPC_Native_Script.ActiveV6_MPC2_NativeList.Length;
        int MPC3_num = MPC_Native_Script.ActiveV6_MPC3_NativeList.Length;

        NativeArray<V6> MPC2_Native = MPC_Native_Script.ActiveV6_MPC2_NativeList;
        NativeArray<Vector3> MPC2_Side = MPC_Native_Script.Active_MPC2_Perpendiculars;
        NativeArray<V6> MPC3_Native = MPC_Native_Script.ActiveV6_MPC3_NativeList;
        NativeArray<Vector3> MPC3_Side = MPC_Native_Script.Active_MPC3_Perpendiculars;
        
        NativeArray<float> MPC3_Att = MPC_Native_Script.ActiveV6_MPC3_Power;

        Debug.Log("Size of MPC2 List = " + MPC2_Native.Length + "; MPC3 List = " + MPC3_Native.Length);

        // all about the LookUpTable for MPC2
        #region Generating LookUp Table for MPC2

        
        NativeArray<SeenPath2> allpath2 = new NativeArray<SeenPath2>(MPC2_num * (MPC2_num - 1), Allocator.TempJob);

        NativeArray<RaycastCommand> CommandsNativeArray_MPC2 = new NativeArray<RaycastCommand>(MPC2_num * (MPC2_num - 1), Allocator.TempJob);
        NativeArray<RaycastHit> ResultsNativeArray_MPC2 = new NativeArray<RaycastHit>(MPC2_num * (MPC2_num - 1), Allocator.TempJob);
        NativeArray<Vector2Int> MPC2_ID = new NativeArray<Vector2Int>(MPC2_num * (MPC2_num - 1), Allocator.TempJob);

        float t_V6 = Time.realtimeSinceStartup;

        #region Paralle Raycasting MPC2

        ParallelRayCastingDataV6 RayCastingData_MPC2 = new ParallelRayCastingDataV6
        {
            MPC_Array = MPC2_Native,
            commands = CommandsNativeArray_MPC2,
            ID = MPC2_ID,
        };
        JobHandle jobHandle_RayCastingData_MPC2 = RayCastingData_MPC2.Schedule(MPC2_num * (MPC2_num - 1), 1);
        jobHandle_RayCastingData_MPC2.Complete();

        // parallel raycasting
        JobHandle rayCastJobMPC1 = RaycastCommand.ScheduleBatch(CommandsNativeArray_MPC2, ResultsNativeArray_MPC2, 64, default);
        rayCastJobMPC1.Complete();
        #endregion

        #region Parallel Calculation of Path2 Parameters
        // parallel search of possiblte second order of paths
        ParallelPath2Search parallelPath2Search = new ParallelPath2Search
        {
            MPC_Array = MPC2_Native,
            MPC_Perpendiculars = MPC2_Side,                    // Who is writing code like this??? It's actually Active_MPC2_Perpendiculars (perpendiculars Karl!!!)
            commands = CommandsNativeArray_MPC2,
            results = ResultsNativeArray_MPC2,
            ID = MPC2_ID,

            maxDistance = MaxSeenDistance/3,
            angleThreshold = (float)0.1,

            PossiblePath2 = allpath2,
        };

        JobHandle jobHandlePath2Search = parallelPath2Search.Schedule(MPC2_num * (MPC2_num - 1), 64);
        jobHandlePath2Search.Complete();
        #endregion

        #region Filtering Zero Valued Path2
        NativeList<int> indexes = new NativeList<int>(Allocator.TempJob);
        IndexFilter indexFilter = new IndexFilter
        {
            Array = allpath2,
        };
        JobHandle jobHandleIndexFilter = indexFilter.ScheduleAppend(indexes, allpath2.Length, 64);
        jobHandleIndexFilter.Complete();

        LookUpTableMPC2 = new NativeArray<SeenPath2>(indexes.Length, Allocator.Persistent);
        NativeArray<Vector2Int> LUTIndexArrayMPC2 = new NativeArray<Vector2Int>(indexes.Length, Allocator.TempJob);
        LookUpTableFinal lookUpTableFinalMPC2 = new LookUpTableFinal
        {
            InArray = allpath2,
            IndexArray = indexes,
            OutArray = LookUpTableMPC2,
            IndxArray = LUTIndexArrayMPC2,
        };
        JobHandle jobHandleLookUpTableMPC2 = lookUpTableFinalMPC2.Schedule(indexes.Length, 64);
        jobHandleLookUpTableMPC2.Complete();
        #endregion

        #region Testing Function for Preparing Indexes

        //Vector2Int[] MPC2LUTID = new Vector2Int[MPC2_num];
        MPC2LUTID = new NativeArray<Vector2Int>(MPC2_num, Allocator.Persistent);
        int progressionIndex = 0;
        
        for (int i = 0; i < MPC2_num; i++)
        {
            int index_min = progressionIndex;
            int index_max = progressionIndex;
            int flagIndex = 0; // if 1 then something is found
            for (int j = progressionIndex; j < LookUpTableMPC2.Length; j++)
            {
                if (i == LookUpTableMPC2[j].MPC_IDs.x)
                { index_max = j; flagIndex = 1; }
                else
                { break; }
            }

            // check if the MPC can see others or not
            if (flagIndex == 1)
            {
                progressionIndex = index_max + 1;
                MPC2LUTID[i] = new Vector2Int(index_min, index_max);
                if (index_max - index_min + 1 > maxlengthMPC2)
                { maxlengthMPC2 = index_max - index_min + 1; }
            } // in the case, it sees others 
            else
            {
                MPC2LUTID[i] = new Vector2Int(-1, -1);
            } // in the case it doesn't see others
        }
        
        /* for checking weak paths
        int weak_path2 = 0;
        for (int i = 0; i < LookUpTableMPC2.Length; i++)
        {
            if (LookUpTableMPC2[i].AngularGain < 0.001)
            {
                weak_path2 += 1;
            }
        }*/
        #endregion

        Debug.Log("Time spent for parallel raycasting : " + ((Time.realtimeSinceStartup - t_V6) * 1000f) + " ms");

        #region drawing MPC2 connections
        /*
        for (int i = 0; i < LookUpTableMPC2.Length; i++)
        {
            int fromMPC = LookUpTableMPC2[i].MPC_IDs.x;
            int toMPC = LookUpTableMPC2[i].MPC_IDs.y;
            //Debug.DrawLine(MPC2_Native[fromMPC].Coordinates + new Vector3(0,1,0), MPC2_Native[toMPC].Coordinates + new Vector3(0, 1, 0), Color.green, 1.0f);
            Debug.DrawLine(MPC2_Native[fromMPC].Coordinates, MPC2_Native[toMPC].Coordinates, Color.green, 1.0f);
        }
        
        for (int i = 0; i < allpath2.Length; i++)
        {
            if (allpath2[i].AngularGain != 0)
            {
                Debug.DrawLine(MPC2_Native[MPC2_ID[i].x].Coordinates, MPC2_Native[MPC2_ID[i].y].Coordinates, Color.cyan, 1.0f);
                Debug.DrawLine(MPC2_Native[MPC2_ID[i].x].Coordinates, MPC2_Native[MPC2_ID[i].x].Coordinates + 5*MPC2_Native[MPC2_ID[i].x].Normal, Color.red, 1.0f);
            }
        }*/
        #endregion

        #region Disposing MPC2 NativeArrays
        LUTIndexArrayMPC2.Dispose();
        indexes.Dispose();
        MPC2_ID.Dispose();
        CommandsNativeArray_MPC2.Dispose();
        ResultsNativeArray_MPC2.Dispose();
        allpath2.Dispose();
        #endregion

        #endregion

        // all about the LookUpTable for MPC3
        #region Generating LookUp Table for MPC3

        NativeArray<SeenPath2> allpath3_half = new NativeArray<SeenPath2>(MPC3_num * (MPC3_num - 1), Allocator.TempJob);

        NativeArray<RaycastCommand> CommandsNativeArray_MPC3 = new NativeArray<RaycastCommand>(MPC3_num * (MPC3_num - 1), Allocator.TempJob);
        NativeArray<RaycastHit> ResultsNativeArray_MPC3 = new NativeArray<RaycastHit>(MPC3_num * (MPC3_num - 1), Allocator.TempJob);
        NativeArray<Vector2Int> MPC3_ID = new NativeArray<Vector2Int>(MPC3_num * (MPC3_num - 1), Allocator.TempJob);
        
        // Start: calculation of the first level of path3
        #region Paralle Raycasting MPC3
        
        ParallelRayCastingDataV6 RayCastingData_MPC3 = new ParallelRayCastingDataV6
        {
            MPC_Array = MPC3_Native,
            commands = CommandsNativeArray_MPC3,
            ID = MPC3_ID,
        };
        JobHandle jobHandle_RayCastingData_MPC3 = RayCastingData_MPC3.Schedule(MPC3_num * (MPC3_num - 1), 1);
        jobHandle_RayCastingData_MPC3.Complete();

        // parallel raycasting
        JobHandle rayCastJobMPC3 = RaycastCommand.ScheduleBatch(CommandsNativeArray_MPC3, ResultsNativeArray_MPC3, 64, default);
        rayCastJobMPC3.Complete();
        #endregion

        #region Parallel Calculation of Path3 Parameters
        // parallel search of possiblte second order of paths
        ParallelPath2Search parallelPath3Search = new ParallelPath2Search
        {
            MPC_Array = MPC3_Native,
            MPC_Perpendiculars = MPC3_Side,
            commands = CommandsNativeArray_MPC3,
            results = ResultsNativeArray_MPC3,
            ID = MPC3_ID,

            maxDistance = MaxSeenDistance/5,
            angleThreshold = (float)0.1,

            PossiblePath2 = allpath3_half,
        };

        JobHandle jobHandlePath3Search = parallelPath3Search.Schedule(MPC3_num * (MPC3_num - 1), 64);
        jobHandlePath3Search.Complete();
        #endregion

        #region Filtering Zero Valued First Floor Path3
        NativeList<int> indexes3 = new NativeList<int>(Allocator.TempJob);
        IndexFilter indexFilter3 = new IndexFilter
        {
            Array = allpath3_half,
        };
        JobHandle jobHandleIndexFilter3 = indexFilter3.ScheduleAppend(indexes3, allpath3_half.Length, 64);
        jobHandleIndexFilter3.Complete();

        NativeArray<SeenPath2> LookUpTableMPC3_half = new NativeArray<SeenPath2>(indexes3.Length, Allocator.TempJob);
        NativeArray<Vector2Int> LUTIndexArrayMPC3 = new NativeArray<Vector2Int>(indexes3.Length, Allocator.TempJob);
        LookUpTableFinal lookUpTableFinalMPC3 = new LookUpTableFinal
        {
            InArray = allpath3_half,
            IndexArray = indexes3,
            OutArray = LookUpTableMPC3_half,
            IndxArray = LUTIndexArrayMPC3,
        };
        JobHandle jobHandleLookUpTableMPC3 = lookUpTableFinalMPC3.Schedule(indexes3.Length, 64);
        jobHandleLookUpTableMPC3.Complete();
        #endregion

        #region Testing Function for Preparing Indexes for MPC3
        NativeArray<Vector2Int> MPC3LUTID = new NativeArray<Vector2Int>(MPC3_num, Allocator.TempJob);
        //Vector2Int[] MPC3LUTID = new Vector2Int[MPC3_num];
        int progressionIndex3 = 0;
        for (int i = 0; i < MPC3_num; i++)
        {
            int index_min = progressionIndex3;
            int index_max = progressionIndex3;
            int flagIndex = 0; // if 1 then something is found
            for (int j = progressionIndex3; j < LookUpTableMPC3_half.Length; j++)
            {
                if (i == LookUpTableMPC3_half[j].MPC_IDs.x)
                { index_max = j; flagIndex = 1; }
                else
                { break; }
            }

            // check if the MPC can see others or not
            if (flagIndex == 1)
            {
                progressionIndex3 = index_max + 1;
                MPC3LUTID[i] = new Vector2Int(index_min, index_max);
            } // in the case, it sees others 
            else
            {
                MPC3LUTID[i] = new Vector2Int(-1, -1);
            } // in the case it doesn't see others
        }
        #endregion
        // End: calculation of the first level of path3

        // Start: calculation of the whole path3s
        #region Finding All Possible Path3

        NativeArray<SeenPath3> allpath3 = new NativeArray<SeenPath3>(LookUpTableMPC3_half.Length * MPC3_num, Allocator.TempJob);
        ParallelPath3Search path3Search = new ParallelPath3Search
        {
            In_AttArray = MPC3_Att,
            InArray = LookUpTableMPC3_half,
            MPC_array = MPC3_Native,
            EdgeIndexes = MPC3LUTID,
            OutArray = allpath3,
        };
        JobHandle jobHandle_path3Searc = path3Search.Schedule(LookUpTableMPC3_half.Length * MPC3_num, 64);
        jobHandle_path3Searc.Complete();
        #endregion

        #region Filtering Out Inactive Path3
        NativeList<int> indexes3_full = new NativeList<int>(Allocator.TempJob);
        IndexFilterPath3 indexFilter3_full = new IndexFilterPath3
        {
            Array = allpath3,
        };
        JobHandle jobHandleFilterPath3 = indexFilter3_full.ScheduleAppend(indexes3_full, allpath3.Length, 64);
        jobHandleFilterPath3.Complete();


        LookUpTableMPC3 = new NativeArray<SeenPath3>(indexes3_full.Length, Allocator.Persistent);
        NativeArray<Vector3Int> LookUpTable_test = new NativeArray<Vector3Int>(indexes3_full.Length, Allocator.TempJob);
        LookUpTableFinalPath3 lookUpTableFinalMPC3_full = new LookUpTableFinalPath3
        {
            InArray = allpath3,
            IndexArray = indexes3_full,
            OutArray = LookUpTableMPC3,
            OutVector3Int = LookUpTable_test
        };
        JobHandle jobHandleLUTMPC3 = lookUpTableFinalMPC3_full.Schedule(indexes3_full.Length, 64);
        jobHandleLUTMPC3.Complete();

        /* for checking how many weak paths we have
        int weak_path3 = 0;
        for (int i = 0; i < LookUpTableMPC3_full.Length; i++)
        {
            if (LookUpTableMPC3_full[i].AngularGain < 0.001)
            {
                weak_path3 += 1;
            }
        }
        */
        #endregion

        #region Testing Function for Preparing Indexes for Full Path3
        
        //Vector2Int[] MPC3SeenID = new Vector2Int[MPC3_num];
        MPC3SeenID = new NativeArray<Vector2Int>(MPC3_num, Allocator.Persistent);
        int progressionSeenIndex = 0;
        for (int i = 0; i < MPC3_num; i++)
        {
            int index_min = progressionSeenIndex;
            int index_max = progressionSeenIndex;
            int flagIndex = 0; // if flagIndex = 1, then something is found
            for (int j = progressionSeenIndex; j < LookUpTableMPC3.Length; j++)
            {
                if (i == LookUpTableMPC3[j].MPC_IDs.x)
                { index_max = j; flagIndex = 1; }
                else
                { break; }
            }

            // check if the MPC can see others or not
            if (flagIndex == 1)
            {
                progressionSeenIndex = index_max + 1;
                MPC3SeenID[i] = new Vector2Int(index_min, index_max);
                if (index_max - index_min + 1 > maxlengthMPC3)
                { maxlengthMPC3 = index_max - index_min + 1; }
            } // in the case, it sees others 
            else
            {
                MPC3SeenID[i] = new Vector2Int(-1, -1);
            } // in the case it doesn't see others
        }
        #endregion

        #region drawing MPC3 connections
        /*
        //for (int i = 0; i < LookUpTableMPC3.Length; i++)
        for (int i = 0; i < 100; i++)
        {
            int first_MPC = LookUpTableMPC3[i].MPC_IDs.x;
            int second_MPC = LookUpTableMPC3[i].MPC_IDs.y;
            int third_MPC = LookUpTableMPC3[i].MPC_IDs.z;
            
            Debug.DrawLine(MPC3_Native[first_MPC].Coordinates, MPC3_Native[second_MPC].Coordinates, Color.yellow, 1.0f);
            Debug.DrawLine(MPC3_Native[second_MPC].Coordinates + new Vector3(0,1,0), MPC3_Native[third_MPC].Coordinates + new Vector3(0, 1, 0), Color.red, 1.0f);
        }*/
        
        #endregion
        // End: calculation of the whole path3s


        #region Disposing MPC3 NativeArrays
        LUTIndexArrayMPC3.Dispose();
        LookUpTableMPC3_half.Dispose();
        indexes3.Dispose();
        MPC3_ID.Dispose();
        CommandsNativeArray_MPC3.Dispose();
        ResultsNativeArray_MPC3.Dispose();
        allpath3_half.Dispose();
        allpath3.Dispose();
        MPC3LUTID.Dispose();
        indexes3_full.Dispose();
        LookUpTable_test.Dispose();
        #endregion

        #endregion
    }

}

[BurstCompile]
public struct LookUpTableFinalPath3 : IJobParallelFor
{
    [ReadOnly] public NativeArray<SeenPath3> InArray;
    [ReadOnly] public NativeArray<int> IndexArray;

    [WriteOnly] public NativeArray<SeenPath3> OutArray;
    [WriteOnly] public NativeArray<Vector3Int> OutVector3Int;
    public void Execute(int index)
    {
        OutArray[index] = InArray[IndexArray[index]];
        OutVector3Int[index] = new Vector3Int(InArray[IndexArray[index]].MPC_IDs.x, InArray[IndexArray[index]].MPC_IDs.y, InArray[IndexArray[index]].MPC_IDs.z);
        //IndxArray[index] = new Vector3Int(InArray[IndexArray[index]].MPC_IDs.x, index);
    }
}

[BurstCompile]
public struct LookUpTableFinal : IJobParallelFor
{
    [ReadOnly] public NativeArray<SeenPath2> InArray;
    [ReadOnly] public NativeArray<int> IndexArray;

    [WriteOnly] public NativeArray<SeenPath2> OutArray;
    [WriteOnly] public NativeArray<Vector2Int> IndxArray;
    public void Execute(int index)
    {
        OutArray[index] = InArray[IndexArray[index]];
        IndxArray[index] = new Vector2Int(InArray[IndexArray[index]].MPC_IDs.x, InArray[IndexArray[index]].MPC_IDs.y);
    }
}

[BurstCompile]
public struct IndexFilter : IJobParallelForFilter
{
    public NativeArray<SeenPath2> Array;
    public bool Execute(int index)
    {
        return Array[index].AngularGain > 0;
    }
}

[BurstCompile]
public struct IndexFilterPath3 : IJobParallelForFilter
{
    public NativeArray<SeenPath3> Array;
    public bool Execute(int index)
    {
        return Array[index].AngularGain > 0;
    }
}


[BurstCompile]
public struct ParallelRayCastingDataV6 : IJobParallelFor
{
    [ReadOnly] public NativeArray<V6> MPC_Array;

    [WriteOnly] public NativeArray<RaycastCommand> commands;
    [WriteOnly] public NativeArray<Vector2Int> ID;
    public void Execute(int index)
    {
        int i_org = Mathf.FloorToInt(index / (MPC_Array.Length - 1));   // MPC from which a ray is casted
        int i_dir = index - i_org * (MPC_Array.Length - 1);             // MPC where the ray is coming
        if (i_org > i_dir)
        {
            Vector3 temp_direction = MPC_Array[i_dir].Coordinates - MPC_Array[i_org].Coordinates;
            commands[index] = new RaycastCommand(MPC_Array[i_org].Coordinates, temp_direction.normalized, temp_direction.magnitude);
            ID[index] = new Vector2Int(i_org, i_dir);                   // save indexes of MPC i the seen MPC k : [i,k]
        }
        else if (i_org <= i_dir)
        {
            Vector3 temp_direction = MPC_Array[i_dir + 1].Coordinates - MPC_Array[i_org].Coordinates;
            commands[index] = new RaycastCommand(MPC_Array[i_org].Coordinates, temp_direction.normalized, temp_direction.magnitude);
            ID[index] = new Vector2Int(i_org, i_dir + 1);
        }
    }
}

public struct SeenPath2
{
    public Vector2Int MPC_IDs;  // from MPC[0] to MPC[8] (MPC[0] -> MPC[8])
    public float Distance;
    public float AoD; // angle that indicates departed ray from MPC[0]
    public float SoD;
    public float AoA; // angle that indicates arrived ray to MPC[8]
    public float SoA;
    public float AngularGain;

    public SeenPath2(Vector2Int v_mpc, float d, float aod, float aoa, float sod, float soa, float g)
    {
        MPC_IDs = v_mpc; // from MPC[0] to MPC[8]

        Distance = d;
        AoD = aod;
        SoD = sod;
        AoA = aoa;
        SoA = soa;
        AngularGain = g;
    }
}

public struct SeenPath3
{
    public Vector3Int MPC_IDs;  // from MPC[0] to MPC[8]
    public float Distance;
    public float AoD; // angle that indicates departed ray from MPC[0]
    public float SoD;
    public float AoA; // angle that indicates arrived ray to MPC[8]
    public float SoA;
    public float AngularGain;

    public SeenPath3(Vector3Int v_mpc, float d, float aod, float sod, float aoa, float soa, float g)
    {
        MPC_IDs = v_mpc; // from MPC[0] to MPC[8]

        Distance = d;
        AoD = aod;
        SoD = sod;
        AoA = aoa;
        SoA = soa;
        AngularGain = g;
    }
}
