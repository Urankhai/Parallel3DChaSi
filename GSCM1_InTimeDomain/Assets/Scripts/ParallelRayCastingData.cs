using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

[BurstCompile]
public struct ParallelRayCastingData : IJobParallelFor
{
    [ReadOnly] public NativeArray<V4> MPC_Array;
    [ReadOnly] public NativeArray<Vector3> OBS_Array;

    [WriteOnly] public NativeArray<RaycastCommand> commands;
    public void Execute(int index)
    {
        int i_obs = Mathf.FloorToInt(index/MPC_Array.Length);
        int i_mpc = index - i_obs * MPC_Array.Length;

        Vector3 temp_direction = MPC_Array[i_mpc].Coordinates - OBS_Array[i_obs];
        commands[index] = new RaycastCommand(OBS_Array[i_obs], temp_direction.normalized, temp_direction.magnitude);
    }
}
