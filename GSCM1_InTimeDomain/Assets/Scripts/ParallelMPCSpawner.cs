using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

[BurstCompile]
public struct ParallelMPCSpawner : IJobParallelFor
{
    public Vector3 Corner_bottom_left;
    public float Height;
    public float Width;



    //public Unity.Mathematics.Random random;
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Unity.Mathematics.Random> rngs;
    [NativeSetThreadIndex]
    readonly int threadId;

    [WriteOnly] public NativeArray<V4> Array;
    public void Execute(int index)
    {
        var rng = rngs[threadId];
        
        
        float x_value = Corner_bottom_left.x + rng.NextFloat(0, Width);
        float y_value = 1.2f + rng.NextFloat(0f, 1f);
        float z_value = Corner_bottom_left.z + rng.NextFloat(0, Height);

        rngs[threadId] = rng;
        
        // Preparing V4 format of the nativearray
        Array[index] = new V4(new Vector3(x_value, y_value, z_value), 0);
    }
}
