using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct ParallelPath3Search : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> In_AttArray;
    [ReadOnly] public NativeArray<V6> MPC_array;
    [ReadOnly] public NativeArray<SeenPath2> InArray;
    [ReadOnly] public NativeArray<Vector2Int> EdgeIndexes; // this has to have the length of MPC3_num

    [WriteOnly] public NativeArray<SeenPath3> OutArray;
    public void Execute(int index)
    {
        int i_org = Mathf.FloorToInt(index / EdgeIndexes.Length);
        int i_sub = index - i_org * EdgeIndexes.Length;

        int first_level_ID = InArray[i_org].MPC_IDs.x;
        int second_level_ID = InArray[i_org].MPC_IDs.y; // we looking at the second level IDs

        float second_level_Att = In_AttArray[second_level_ID];

        // defining ranges of the seen MPC from MPC[second_level_ID]
        int start_ID = EdgeIndexes[second_level_ID].x; 
        int finis_ID = EdgeIndexes[second_level_ID].y;

        for (int i = start_ID; i < finis_ID + 1; i++)
        {
            int third_level_ID = InArray[i].MPC_IDs.y;
            if (third_level_ID == i_sub && i_sub != first_level_ID)
            {
                Vector3 incoming_vector = (MPC_array[second_level_ID].Coordinates - MPC_array[first_level_ID].Coordinates).normalized;
                Vector3 outgoing_vector = (MPC_array[third_level_ID].Coordinates - MPC_array[second_level_ID].Coordinates).normalized;
                
                Vector3 mpc_perp_vector = new Vector3(-MPC_array[second_level_ID].Normal.z, 0, MPC_array[second_level_ID].Normal.x);

                float direction_sign = Vector3.Dot(mpc_perp_vector, incoming_vector) * Vector3.Dot(mpc_perp_vector, outgoing_vector);
                
                if (direction_sign > 0)
                {
                    float thr = (float)0.35;// the value is hard written according to Carls paper

                    // extracting parameters of the first chain
                    float distance1 = InArray[i_org].Distance;
                    float aod1 = InArray[i_org].AoD;
                    float sod1 = InArray[i_org].SoD;
                    float aoa1 = InArray[i_org].AoA;
                    float gain1 = InArray[i_org].AngularGain;

                    // extracting parameters of the second chain
                    float distance2 = InArray[i].Distance;
                    float aod2 = InArray[i].AoD;
                    float aoa2 = InArray[i].AoA;
                    float soa2 = InArray[i].SoA;
                    float gain2 = InArray[i].AngularGain;

                    // calculating angular gain
                    float gain12 = AngularGainFunc(aoa1, aod2, thr);

                    // calculating total path3 parameters
                    float total_gain = gain1 * (second_level_Att * gain12) * gain2; // in the brakets: we multiply to the attenuation coefficient
                    Vector3Int path3vector = new Vector3Int(first_level_ID, second_level_ID, third_level_ID);

                    OutArray[index] = new SeenPath3(path3vector, distance1 + distance2, aod1, sod1, aoa2, soa2, total_gain);
                }
                break;
            }
        }

    }
    private float AngularGainFunc(float angle1, float angle2, float threshold)
    {
        float Gain = 1;

        if (Mathf.Abs(angle1 - angle2) > threshold)
        { Gain = Mathf.Exp(-12 * (Mathf.Abs(angle1 - angle2) - threshold)); }

        return Gain;
    }
}
