using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct PickMPCParallelV5 : IJobParallelFor
{
    [ReadOnly] public Vector3 n1;
    [ReadOnly] public Vector3 p1;
    [ReadOnly] public Vector3 s1;
    [ReadOnly] public Vector3 n2;
    [ReadOnly] public Vector3 p2;
    [ReadOnly] public Vector3 s2;

    [ReadOnly] public float Xleft;
    [ReadOnly] public float Xright;
    [ReadOnly] public float Zdown;
    [ReadOnly] public float Zup;


    public NativeArray<V5> MPC_array;

    [WriteOnly] public NativeArray<V6> MPC_V6;

    public void Execute(int index)
    {
        if (MPC_array[index].Coordinates.x >= Xleft && MPC_array[index].Coordinates.x <= Xright && MPC_array[index].Coordinates.z >= Zdown && MPC_array[index].Coordinates.z <= Zup)
        {
            if (MPC_array[index].Inclusion == 0)
            {
                if (p1 == p2) // the polygon is a triangle
                {
                    Vector3 side1 = s1 - p1;
                    Vector3 side2 = s2 - p2;
                    float S_triangle = Mathf.Abs(-side1.x * side2.z + side1.z * side2.x); // double area


                    // We check if MPC_array[index] is in the triangle side1 side2
                    Vector3 v1 = s1 - MPC_array[index].Coordinates;
                    Vector3 v2 = p1 - MPC_array[index].Coordinates;
                    Vector3 v3 = s2 - MPC_array[index].Coordinates;
                    float S1 = Mathf.Abs(-v1.x * v2.z + v1.z * v2.x); // double area
                    float S2 = Mathf.Abs(-v2.x * v3.z + v2.z * v3.x); // double area
                    float S3 = Mathf.Abs(-v3.x * v1.z + v3.z * v1.x); // double area

                    if (Mathf.Abs(S_triangle - S1 - S2 - S3) < 0.001)
                    {
                        // if the sum of areas is almost equal to the whole area, then the MPC is included into the active set of MPCs
                        MPC_V6[index] = new V6(MPC_array[index].Coordinates, (n1 + n2).normalized);
                        MPC_array[index] = new V5(MPC_array[index].Coordinates, MPC_array[index].MPC_order, 1);
                    }
                }
                else // the polygon is not a triangle
                {
                    float dot_prod_normals = Vector3.Dot(n1, n2);
                    if (dot_prod_normals <= 0)    // then the polygon is not convex (or triangle where p1 != p2)
                    {
                        // in this case, we consider two triangles separately
                        Vector3 v11 = p1 - s1;
                        Vector3 v12 = p2 - s1;
                        float S1 = Mathf.Abs(-v11.x * v12.z + v11.z * v12.x); //(v11, v12, out float S1);

                        Vector3 v21 = p1 - s2;
                        Vector3 v22 = p2 - s2;
                        float S2 = Mathf.Abs(-v21.x * v22.z + v21.z * v22.x); //Area2D(v21, v22, out float S2);
                                                                              // we consider such strange triangles in order to cover the biggest area defined at leas with one correct triangle
                                                                              // and at the same time to avoid considering a non-convex quadrangle

                        Vector3 vp1 = p1 - MPC_array[index].Coordinates;
                        Vector3 vp2 = p2 - MPC_array[index].Coordinates;

                        Vector3 vs1 = s1 - MPC_array[index].Coordinates;
                        Vector3 vs2 = s2 - MPC_array[index].Coordinates;

                        // common area that is related to both S1 and S2
                        float Spp = Mathf.Abs(-vp1.x * vp2.z + vp1.z * vp2.x); //Area2D(vp1, vp2, out float Spp);

                        // areas related to S1
                        float Sp1s1 = Mathf.Abs(-vp1.x * vs1.z + vp1.z * vs1.x); //Area2D(vp1, vs1, out float Sp1s1);
                        float Sp2s1 = Mathf.Abs(-vp2.x * vs1.z + vp2.z * vs1.x); //Area2D(vp2, vs1, out float Sp2s1);

                        // areas related to S2
                        float Sp1s2 = Mathf.Abs(-vp1.x * vs2.z + vp1.z * vs2.x);//Area2D(vp1, vs2, out float Sp1s2);
                        float Sp2s2 = Mathf.Abs(-vp2.x * vs2.z + vp2.z * vs2.x);//Area2D(vp2, vs2, out float Sp2s2);

                        // check if the point [i] belongs to S1 or S2
                        if (Mathf.Abs(S1 - Spp - Sp1s1 - Sp2s1) < 0.001)
                        {
                            MPC_V6[index] = new V6(MPC_array[index].Coordinates, n1);
                            MPC_array[index] = new V5(MPC_array[index].Coordinates, MPC_array[index].MPC_order, 1);
                        }
                        else if (Mathf.Abs(S2 - Spp - Sp1s2 - Sp2s2) < 0.001)
                        {
                            MPC_V6[index] = new V6(MPC_array[index].Coordinates, n2);
                            MPC_array[index] = new V5(MPC_array[index].Coordinates, MPC_array[index].MPC_order, 1);
                        }
                    }
                    else // the polygon is convex
                    {
                        // the first triangle of the quadrangle
                        Vector3 v11 = p1 - s1;
                        Vector3 v12 = s2 - s1;
                        float S1 = Mathf.Abs(-v11.x * v12.z + v11.z * v12.x);//Area2D(v11, v12, out float S1);

                        // the second triangle of the quadrangle
                        Vector3 v21 = p1 - p2;
                        Vector3 v22 = s2 - p2;
                        float S2 = Mathf.Abs(-v21.x * v22.z + v21.z * v22.x);//Area2D(v21, v22, out float S2);

                        Vector3 v1 = p1 - MPC_array[index].Coordinates;
                        Vector3 v2 = s1 - MPC_array[index].Coordinates;
                        Vector3 v3 = s2 - MPC_array[index].Coordinates;
                        Vector3 v4 = p2 - MPC_array[index].Coordinates;

                        float A1 = Mathf.Abs(-v1.x * v2.z + v1.z * v2.x);//Area2D(v1, v2, out float A1);
                        float A2 = Mathf.Abs(-v2.x * v3.z + v2.z * v3.x);//Area2D(v2, v3, out float A2);
                        float A3 = Mathf.Abs(-v3.x * v4.z + v3.z * v4.x);//Area2D(v3, v4, out float A3);
                        float A4 = Mathf.Abs(-v4.x * v1.z + v4.z * v1.x);//Area2D(v4, v1, out float A4);


                        float area_diff = Mathf.Abs(S1 + S2 - A1 - A2 - A3 - A4);

                        if (area_diff < 1)
                        {
                            MPC_V6[index] = new V6(MPC_array[index].Coordinates, (n1 + n2).normalized);
                            MPC_array[index] = new V5(MPC_array[index].Coordinates, MPC_array[index].MPC_order, 1);
                        }
                    }
                }
            }
        }
    }
}