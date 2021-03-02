using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

[BurstCompile]
public struct ParallelMPCSpawnerAll : IJobParallelFor
{
    public Vector3 Corner_bottom_left;
    public float Height;
    public float Width;

    public int MPC1_quantity;
    public int MPC2_quantity;
    public int MPC3_quantity;
    public int  DMC_quantity;


    //public Unity.Mathematics.Random random;
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Unity.Mathematics.Random> rngs;
    [NativeSetThreadIndex]
    readonly int threadId;

    [WriteOnly] public NativeArray<V5> Array;
    public void Execute(int index)
    {
        var rng = rngs[threadId];
        
        
        float x_value = Corner_bottom_left.x + rng.NextFloat(0, Width);
        float y_value = 1.2f + rng.NextFloat(0f, 1f);
        float z_value = Corner_bottom_left.z + rng.NextFloat(0, Height);

        rngs[threadId] = rng;

        // Preparing V4 format of the nativearray
        int order;
        if (index < MPC1_quantity)
        { order = 1; } // MPC1
        else if (index >= MPC1_quantity && index < MPC1_quantity + MPC2_quantity)
        { order = 2; } // MPC2
        else if (index >= MPC1_quantity + MPC2_quantity && index < MPC1_quantity + MPC2_quantity + MPC3_quantity)
        { order = 3; } // MPC3
        else
        { order = 4; } // DMC

        Array[index] = new V5(new Vector3(x_value, y_value, z_value), order, 0);
    }
}
