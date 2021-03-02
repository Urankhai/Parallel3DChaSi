using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using System;

[BurstCompile]
public struct Path3ActiveSet : IJobParallelFor
{
    [ReadOnly] public NativeArray<V6> SeenMPC3Table;
    [ReadOnly] public NativeArray<Path3Half> InputArray;
    [ReadOnly] public NativeArray<Path3Half> CompareArray;
    [ReadOnly] public int MaxListsLength;
    [ReadOnly] public Path3 EmptyElement;
    [ReadOnly] public float Speed_of_Light;

    [WriteOnly] public NativeArray<Path3> Output;
    [WriteOnly] public NativeArray<float> OutputDelays;
    [WriteOnly] public NativeArray<float> OutputAmplitudes;
    public void Execute(int index)
    {
        Output[index] = EmptyElement;
        OutputDelays[index] = 0;
        OutputAmplitudes[index] = 0;

        // calculate a relative index of the element inside each batch
        var temp_out_index = Mathf.FloorToInt(index / MaxListsLength);
        var relative_index = index - temp_out_index * MaxListsLength;
        Vector3 direction1 = (InputArray[temp_out_index].MPC3_2 - InputArray[temp_out_index].MPC3_1).normalized;


        int number_of_entrance = 0;
        
        for (int i = 0; i < CompareArray.Length; i++)
        {
            if (InputArray[temp_out_index].MPC3_2ID == CompareArray[i].MPC3_2ID)
            {
                if (relative_index == number_of_entrance)
                {
                    Vector3 direction2 = (CompareArray[i].MPC3_2 - CompareArray[i].MPC3_1).normalized;
                    Vector3 normal1 = SeenMPC3Table[CompareArray[i].MPC3_2ID].Normal;
                    Vector3 n_perp = new Vector3(-normal1.z, 0, normal1.x);
                    if ((Vector3.Dot(direction1, n_perp) * Vector3.Dot(direction2, n_perp)) < 0)
                    {
                        double theta1 = Math.Acos(Vector3.Dot(normal1, -direction1));
                        double theta2 = Math.Acos(Vector3.Dot(normal1, -direction2));
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

                        float angular_gain1 = (float)Math.Exp(-12 * ((theta1 + theta2 - 0.35) * I1 + (theta1 - 1.22) * I2 + (theta2 - 1.22) * I3));
                        float angular_gain2 = CompareArray[i].AngularGain;
                        float angular_gain3 = InputArray[temp_out_index].AngularGain;
                        float angular_gain = angular_gain1 * angular_gain2 * angular_gain3;

                        var total_distance = InputArray[temp_out_index].Distance + CompareArray[i].Distance;
                        Output[index] = new Path3(InputArray[temp_out_index].Point, InputArray[temp_out_index].MPC3_1, InputArray[temp_out_index].MPC3_2, CompareArray[i].MPC3_1, CompareArray[i].Point, total_distance, angular_gain);
                        OutputDelays[index] = total_distance;// / Speed_of_Light;
                        OutputAmplitudes[index] = angular_gain * (1 / total_distance);
                        break;
                    }
                }

                number_of_entrance += 1;
            }
        }
        
    }
}
