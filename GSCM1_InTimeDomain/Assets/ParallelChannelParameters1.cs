using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


[BurstCompile]
public struct ParallelChannelParameters1 : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> MPC_attenuation;
    [ReadOnly] public NativeArray<V6> MPC_array;
    [ReadOnly] public NativeArray<Vector2Int> Links;
    [ReadOnly] public NativeArray<Vector3> CarsPositions;
    [ReadOnly] public NativeArray<Vector3> CarsFwd;

    [ReadOnly] public NativeArray<RaycastCommand> commands;
    [ReadOnly] public NativeArray<RaycastHit> raycastresults;
    [ReadOnly] public NativeArray<int> VisibilityIndicator;

    [WriteOnly] public NativeArray<Vector3Int> TestArray;
    [WriteOnly] public NativeArray<Path> OutArray;
    public void Execute(int index)
    {
        int i_link = Mathf.FloorToInt(index / MPC_array.Length);
        int i_mpc = index - i_link * MPC_array.Length;


        int i_car1 = Links[i_link].x;
        int i_car2 = Links[i_link].y;

        int i_mpc_car1 = i_car1 * MPC_array.Length + i_mpc;
        int i_mpc_car2 = i_car2 * MPC_array.Length + i_mpc;

        float test_dist = raycastresults[i_mpc_car1].distance + raycastresults[i_mpc_car2].distance; // if resulting distance is zero, then the raya haven't hit anything
        float test_mult = VisibilityIndicator[i_mpc_car1] + VisibilityIndicator[i_mpc_car2];

        if (test_dist == 0 && test_mult > 1)
        {
            // calulating perpendicular direction to the normal of the mpc
            Vector3 norm = MPC_array[i_mpc].Normal;
            Vector3 perp = new Vector3(-norm.z, 0, norm.x); // this vector is needed to distinguish angles between cars
                                                            // from car1 to the MPC
            Vector3 dir1 = commands[i_mpc_car1].direction;
            Vector3 dir2 = commands[i_mpc_car2].direction;
            // from data preparation, we already know that incoming angles are correct, so, we need to check if two signals come from left and right sides
            float direction_sign = Vector3.Dot(perp, dir1) * Vector3.Dot(perp, dir2); // in a correct situation, the sign must be negative
            if (direction_sign < 0)
            {
                float thr1 = (float)0.35;// the value is hard written according to Carls paper
                float thr2 = (float)1.22;// the value is hard written according to Carls paper

                Vector2Int link = new Vector2Int(i_car1, i_car2);
                float distance = commands[i_mpc_car1].distance + commands[i_mpc_car2].distance;

                float AoA = Mathf.Acos(Vector3.Dot(dir1, -norm));
                float AoD = Mathf.Acos(Vector3.Dot(dir2, -norm));

                float angular_gain = AngularGainFunc(AoA, AoD, thr1, thr2);
                float attenuation = angular_gain * MPC_attenuation[i_mpc];

                TestArray[index] = new Vector3Int(i_car1, i_mpc, i_car2);
                OutArray[index] = new Path(link, distance, attenuation);
            }

        }

    }
    private float AngularGainFunc(float angle1, float angle2, float threshold1, float threshold2)
    {
        float Gain0 = 1;
        float Gain1 = 1;
        float Gain2 = 1;

        if (Mathf.Abs(angle1 - angle2) > threshold1)
        { Gain0 = Mathf.Exp(-12 * (Mathf.Abs(angle1 - angle2) - threshold1)); }

        if (Mathf.Abs(angle1) > threshold2)
        { Gain1 = Mathf.Exp(-12 * (Mathf.Abs(angle1) - threshold2)); }

        if (Mathf.Abs(angle2) > threshold2)
        { Gain2 = Mathf.Exp(-12 * (Mathf.Abs(angle2) - threshold2)); }

        float Gain = Gain0 * Gain1 * Gain2;
        return Gain;
    }
}


[BurstCompile]
public struct FilterOutZeros : IJobParallelForFilter
{
    public NativeArray<RaycastHit> RaycastResults;
    public NativeArray<int> IndicatorArray;
    public bool Execute(int index)
    {
        return (IndicatorArray[index] == 1 && RaycastResults[index].distance == 0);
    }
}