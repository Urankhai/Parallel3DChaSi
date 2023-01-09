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
        float angularFilter = 1.57f;
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

                //float sign_incoming = Mathf.Sign(Vector3.Dot(incoming_vector, mpc_perp_vector));
                //float sign_outgoing = Mathf.Sign(Vector3.Dot(mpc_perp_vector, outgoing_vector));


                float direction_sign = Vector3.Dot(mpc_perp_vector, incoming_vector) * Vector3.Dot(mpc_perp_vector, outgoing_vector);
                
                if (direction_sign > -1.5f) // the angle limiting possible paths, 
                {
                    float thr = (float)0.2;// the value is hard written according to Carls paper

                    Vector3 center = new Vector3(40.8f, 1.2f, -13.8f);

                    Vector3 mpc1 = MPC_array[first_level_ID].Coordinates - center;
                    Vector3 mpc2 = MPC_array[second_level_ID].Coordinates - center;
                    Vector3 nrm2 = MPC_array[second_level_ID].Normal;
                    Vector3 mpc3 = MPC_array[third_level_ID].Coordinates - center;

                    Vector3 a = mpc1 - mpc2;
                    Vector3 b = mpc3 - mpc2;
                    
                    Vector3 p = new Vector3(nrm2.z, 0, -nrm2.x);
                    
                    float Y11 = Vector3.Dot(a, p);
                    float X11 = Vector3.Dot(a, nrm2);

                    float Y12 = Vector3.Dot(b, p);
                    float X12 = Vector3.Dot(b, nrm2);

                    //float theta1 = 180.0f * Mathf.Atan2( Y1, X1) / Mathf.PI; 
                    //float theta2 = 180.0f * Mathf.Atan2( Y2, X2) / Mathf.PI;
                    float theta1 = Mathf.Atan2( Y11, X11); 
                    float theta2 = -Mathf.Atan2( Y12, X12);
                    float test_gain12 = AngularGainFunc(theta1, theta2, thr);

                    float val1 = Y11 / X11;
                    float val2 = Y12 / X12;

                    // extracting parameters of the first chain
                    float distance1 = InArray[i_org].Distance;
                    float aod1 = InArray[i_org].AoD;
                    float sod1 = InArray[i_org].SoD;
                    float aoa1 = InArray[i_org].AoA;
                    float soa1 = -InArray[i_org].SoA; // same as sign_incoming
                    float gain1 = InArray[i_org].AngularGain;

                    /*
                    if (sign_incoming - soa1 != 0)
                    { 
                        soa1 = -soa1;
                    }
                    */

                    // extracting parameters of the second chain
                    float distance2 = InArray[i].Distance;
                    float aod2 = InArray[i].AoD;
                    float sod2 = InArray[i].SoD; // same as sign_outgoing
                    float aoa2 = InArray[i].AoA;
                    float soa2 = InArray[i].SoA;
                    float gain2 = InArray[i].AngularGain;

                    /*
                    if (sign_outgoing - sod2 != 0)
                    {
                        sod2 = -sod2;
                    }
                    */

                    // calculating angular gain
                    if (soa1 - sod2 != 0)
                    {
                        if (aoa1 < angularFilter || aod2 < angularFilter)
                        {aod2 = -aod2;}
                    }

                    float gain12 = AngularGainFunc(aoa1, aod2, thr);

                    // calculating total path3 parameters
                    //float total_gain = gain1 * (second_level_Att * gain12) * gain2; // in the brakets: we multiply to the attenuation coefficient
                    float total_gain = gain12;
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
        { 
            Gain = Mathf.Exp(-12 * (Mathf.Abs(angle1 - angle2) - threshold)); 
        }

        return Gain;
    }
}
