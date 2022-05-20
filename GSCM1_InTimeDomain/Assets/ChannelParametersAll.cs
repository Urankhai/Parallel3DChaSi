using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;


[BurstCompile]
public struct ChannelParametersAll : IJobParallelFor
{
    [ReadOnly] public bool OmniAntennaFlag;
    // Environment
    [ReadOnly] public NativeArray<float> MPC_Attenuation;
    [ReadOnly] public NativeArray<V6> MPC_Array;
    [ReadOnly] public NativeArray<Vector3> MPC_Perp;
    [ReadOnly] public int DMCNum;
    [ReadOnly] public int MPC1Num;
    [ReadOnly] public int MPC2Num;
    [ReadOnly] public int MPC3Num;
    [ReadOnly] public int MPCNum;

    [ReadOnly] public NativeArray<SeenPath2> LookUpTableMPC2;
    [ReadOnly] public NativeArray<Vector2Int> LUTIndexRangeMPC2;
    [ReadOnly] public NativeArray<SeenPath3> LookUpTableMPC3;
    [ReadOnly] public NativeArray<Vector2Int> LUTIndexRangeMPC3;

    // Cars related information
    [ReadOnly] public NativeArray<Vector2Int> ChannelLinks;
    [ReadOnly] public NativeArray<Vector3> CarsCoordinates;
    [ReadOnly] public NativeArray<Vector3> CarsForwardsDir;

    // Raycasting results
    [ReadOnly] public NativeArray<RaycastCommand> Commands;
    [ReadOnly] public NativeArray<RaycastHit> Results;
    [ReadOnly] public NativeArray<float> SignOfArrival;
    [ReadOnly] public NativeArray<int> SeenIndicator;

    // Antenna Pattern (EADF)
    [ReadOnly] public NativeArray<Vector2> Pattern;

    [WriteOnly] public NativeArray<int> IDArray; // Size MPCNum*LinksSize
    [WriteOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeMultiHashMap<int, Path_and_IDs>.ParallelWriter HashMap;
    public void Execute(int index)
    {
        float thr1 = (float)0.35;// the value is hard written according to Carls paper
        float thr2 = (float)1.22;// the value is hard written according to Carls paper

        int i_link = Mathf.FloorToInt(index / MPCNum);
        int i_mpc = index - i_link * MPCNum; // index of an mpc

        int i_car1 = ChannelLinks[i_link].x;
        int i_car2 = ChannelLinks[i_link].y;

        Vector3 fwd1 = CarsForwardsDir[i_car1];
        Vector3 fwd2 = CarsForwardsDir[i_car2];

        int i_mpc_car1 = i_car1 * MPCNum + i_mpc;
        float test_dist1 = Results[i_mpc_car1].distance;
        float comm_dist1 = Commands[i_mpc_car1].distance;
        float SoA1 = SignOfArrival[i_mpc_car1]; // car1 sees the MPC[i_mpc] from left or right direction


        float Optimization_Angle = 1.0f;
        //Vector3 asd1 = MPC_Array[i_mpc].Coordinates - CarsCoordinates[i_car1];
        //float signofA1 = Mathf.Sign(Vector3.Dot( MPC_Perp[i_mpc], -asd1));

        if (test_dist1 == 0 && comm_dist1 != 0)
        {
            if (i_mpc < DMCNum + MPC1Num)// DMC and MCP1 Channel Parameters
            {
                int i_mpc_car2 = i_car2 * MPCNum + i_mpc;
                float test_dist2 = Results[i_mpc_car2].distance; // if the resulting distance is zero, then the ray hasn't hit anything
                float comm_dist2 = Commands[i_mpc_car2].distance; // if the resulting distance is zero, then the ray hasn't hit anything
                float SoA2 = SignOfArrival[i_mpc_car2]; // SignOfArrival = Vector3.Dot(MPC_Perpendiculars[i_mpc], car - mpc)
                float test_sign = SoA1 * SoA2;
                if (test_dist2 == 0 && comm_dist2 !=0 && test_sign < 0.7f) // optimally, test_sign should be < 1
                {
                    Vector3 dir1 = Commands[i_mpc_car1].direction; // from car1 to the MPC
                    Vector3 dir2 = Commands[i_mpc_car2].direction; // from car2 to the MPC

                    

                    float antenna_gain1 = 1;
                    float antenna_gain2 = 1;
                    if (OmniAntennaFlag == false)
                    {
                        // find the angle of vision of MPCs from the car sight
                        float phi1 = Mathf.Acos(Vector3.Dot(dir1, fwd1));
                        float phi2 = Mathf.Acos(Vector3.Dot(dir2, fwd2));

                        antenna_gain1 = EADF_Reconstruction(Pattern, phi1);
                        antenna_gain2 = EADF_Reconstruction(Pattern, phi2);
                    }

                    Vector3 norm = MPC_Array[i_mpc].Normal;
                    Vector3 perp = MPC_Perp[i_mpc];

                    //Vector2Int link = new Vector2Int(i_car1, i_car2);
                    float distance = Commands[i_mpc_car1].distance + Commands[i_mpc_car2].distance;

                    // picture below explains directions from car1 to MPC to car2
                    //        MPC --> perp
                    //     >   |   \
                    //    /    V    >
                    // car1  norm  car2
                    float SignAoA = Mathf.Sign(Vector3.Dot(dir1, perp)); // sign of dir1 is pointed to the MPC
                    float SignAoD = Mathf.Sign(Vector3.Dot(-dir2, perp)); // sign of dir2 is pointed from the MPC

                    float AoA = Mathf.Acos(Vector3.Dot(-dir1, norm));
                    float AoD = Mathf.Acos(Vector3.Dot(-dir2, norm));
                    float sAoD;
                    if (SignAoA != SignAoD)
                    { sAoD = -AoD; }
                    else
                    { sAoD = AoD; }

                    float angular_gain = AngularGainFunc(AoA, sAoD, thr1, thr2);
                    if (angular_gain > 0.000001)
                    {
                        float attenuation = antenna_gain1 * antenna_gain2 * angular_gain * MPC_Attenuation[i_mpc];




                        //Path3 path1 = new Path3(CarsCoordinates[i_car1], MPC_Array[i_mpc].Coordinates, new Vector3(0, 0, 0), new Vector3(0, 0, 0), CarsCoordinates[i_car2], distance, attenuation);
                        PathChain pathChain1 = new PathChain(i_car1, i_mpc, 0, 0, i_car2);
                        Path path1 = new Path(ChannelLinks[i_link], distance, attenuation);
                        
                        Path_and_IDs path1_and_IDs = new Path_and_IDs(pathChain1, path1, 1);
                        IDArray[index] = 1;
                        HashMap.Add(index, path1_and_IDs);
                    }
                }
            }
            else if (i_mpc < DMCNum + MPC1Num + MPC2Num)// MCP2 Channel Parameters
            {
                int i_mpc2 = i_mpc - (DMCNum + MPC1Num);

                Vector2Int IndexRange2 = LUTIndexRangeMPC2[i_mpc2];
                if (IndexRange2 != new Vector2Int(-1, -1))
                {
                    for (int i = IndexRange2.x; i < IndexRange2.y; i++)
                    {
                        // Data from LUT for MPC2
                        int seen_mpc2_from_i_mpc2 = LookUpTableMPC2[i].MPC_IDs.y; // the ID of the mpc within MPC2_Array (not MPC_Array)
                        float SoA2_car1 = LookUpTableMPC2[i].SoD; // we check if incident directions come from different sides
                        float SoA2_car2 = LookUpTableMPC2[i].SoA; // direction of arrival for the MPC2[i_mpc2]

                        //if (SoA1 * SoA2_car1 < Optimization_Angle) // car1 ---> SoA1 {---> MPC[i_mpc] = MPC2[i_mpc2] --->} SoA2_car1 <--- MPC2[seen_mpc2_from_i_mpc2]
                        //{
                        // from car2 to different MCP2s
                        int i_mpc2_car2 = i_car2 * MPCNum + DMCNum + MPC1Num + seen_mpc2_from_i_mpc2; // the ID of the mpc within the array that has length link_num*MPC_num
                        float test_dist = Results[i_mpc2_car2].distance;    // if the resulting distance is zero, then the ray hasn't hit anything
                        float comm_dist = Commands[i_mpc2_car2].distance;  // if the resulting distance is zero, then the ray hasn't hit anything
                        float SoA2 = SignOfArrival[i_mpc2_car2]; // car2 sees MPC2[seen_mpc2_from_i_mpc2] from left or right side

                        #region Addional testing features
                        /*
                        Vector3 c1 = MPC_Array[i_mpc].Coordinates;
                        Vector3 n1 = MPC_Array[i_mpc].Normal;
                        Vector3 p1 = MPC_Perp[i_mpc];

                        Vector3 c2 = MPC_Array[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2].Coordinates;
                        Vector3 n2 = MPC_Array[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2].Normal;
                        Vector3 p2 = MPC_Perp[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2];

                        Vector3 c2c1 = c2 - c1;
                        float sign1 = Mathf.Sign(Vector3.Dot(p1, c2c1));
                        float sign2 = Mathf.Sign(Vector3.Dot(p2, -c2c1));

                        Vector3 asd2 = MPC_Array[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2].Coordinates - CarsCoordinates[i_car2];
                        float signofA2 = Mathf.Sign(Vector3.Dot(MPC_Perp[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2], -asd2));
                        */
                        #endregion

                        if (test_dist == 0 && comm_dist != 0)// && SoA2_car2 * SoA2 < Optimization_Angle) // car2 ---> SoA2 {---> MPC[i_mpc2_car2] = MPC2[seen_mpc2_from_i_mpc2] --->} SoA2_car2 <--- MPC2[i_mpc2]
                        {
                            float distance = Commands[i_mpc_car1].distance + LookUpTableMPC2[i].Distance + Commands[i_mpc2_car2].distance;
                            Vector3 dir1 = Commands[i_mpc_car1].direction; // from car1 to the MPC
                            Vector3 dir2 = Commands[i_mpc2_car2].direction; // from car2 to the MPC

                            // find the angle of vision of MPCs from the car sight

                            float antenna_gain1 = 1;
                            float antenna_gain2 = 1;
                            if (OmniAntennaFlag == false)
                            {
                                // find the angle of vision of MPCs from the car sight
                                float phi1 = Mathf.Acos(Vector3.Dot(dir1, fwd1));
                                float phi2 = Mathf.Acos(Vector3.Dot(dir2, fwd2));

                                antenna_gain1 = EADF_Reconstruction(Pattern, phi1);
                                antenna_gain2 = EADF_Reconstruction(Pattern, phi2);
                            }




                            // Parameters of active MPC
                            Vector3 car_coor1 = CarsCoordinates[i_car1];
                            float att1 = MPC_Attenuation[i_mpc];
                            Vector3 norm1 = MPC_Array[i_mpc].Normal;
                            Vector3 coor1 = MPC_Array[i_mpc].Coordinates;


                            // Parameters of active MPC
                            Vector3 car_coor2 = CarsCoordinates[i_car2];
                            float att2 = MPC_Attenuation[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2];
                            Vector3 norm2 = MPC_Array[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2].Normal;
                            Vector3 coor2 = MPC_Array[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2].Coordinates;


                            float AoA1 = Mathf.Acos(Vector3.Dot(-dir1, norm1));             // from car1 to MPC[i1]
                            float AoD1 = LookUpTableMPC2[i].AoD;                            // AoD from MPC[i1] to MPC[i2]

                            float sign_11 = -Mathf.Sign(SoA1); //Mathf.Sign(Vector3.Dot(dir1, perp1)); //Mathf.Sign(Vector3.Dot(mpc2_dir_orgn_car1, perp1));
                            float sign_12 = LookUpTableMPC2[i].SoD;  // Mathf.Sign(Vector3.Dot(perp1, mpc2_dir_dest_orgn));

                            if (sign_11 - sign_12 != 0)
                            { AoD1 = -AoD1; }

                            float angular_gain1 = AngularGainFunc(AoA1, AoD1, thr1, thr2);


                            float AoD2 = Mathf.Acos(Vector3.Dot(-dir2, norm2));             // from MPC[i2] to car2
                            float AoA2 = LookUpTableMPC2[i].AoA;                            // AoA from MPC[i2] to MPC[i1]

                            float sign_21 = -LookUpTableMPC2[i].SoA; // Mathf.Sign(Vector3.Dot(mpc2_dir_dest_orgn, perp2));
                            float sign_22 = Mathf.Sign(SoA2); //Mathf.Sign(Vector3.Dot(perp2, -dir2)); //Mathf.Sign(Vector3.Dot(perp2, mpc2_dir_car2_dest));

                            if (sign_21 - sign_22 != 0)
                            { AoD2 = -AoD2; }

                            float angular_gain2 = AngularGainFunc(AoD2, AoA2, thr1, thr2);


                            //-----------------------------------------------------------------------------------
                            //-----------------------------------------------------------------------------------
                            //-----------------------------------------------------------------------------------
                            // testing code is here
                            // all the below test has worked out and all the data is correct. 01.03.2022

                            Vector3 car1 = CarsCoordinates[i_car1];
                            Vector3 car2 = CarsCoordinates[i_car2];

                            Vector3 mpc2_orgn = MPC_Array[i_mpc].Coordinates;
                            Vector3 perp1 = MPC_Perp[i_mpc];

                            Vector3 mpc2_dest = MPC_Array[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2].Coordinates;
                            Vector3 perp2 = MPC_Perp[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2];

                            Vector3 mpc2_dir_orgn_car1 = (mpc2_orgn - car1).normalized;
                            Vector3 mpc2_dir_car2_dest = (car2 - mpc2_dest).normalized;

                            float asd_sign_11 = -Mathf.Sign(SoA1); float asd1 = Mathf.Sign(Vector3.Dot(dir1, perp1)); //Mathf.Sign(Vector3.Dot(mpc2_dir_orgn_car1, perp1));
                            float ads_sign_12 = LookUpTableMPC2[i].SoD;  //Mathf.Sign(Vector3.Dot(perp1, mpc2_dir_dest_orgn));
                            float test_sign_11 = Mathf.Sign(Vector3.Dot(mpc2_dir_orgn_car1, perp1));
                            float test_test_sign_11 = -Mathf.Sign(SoA1);


                            float asd_sign_21 = -LookUpTableMPC2[i].SoA; // Mathf.Sign(Vector3.Dot(mpc2_dir_dest_orgn, perp2));
                            float asd_sign_22 = Mathf.Sign(SoA2); //Mathf.Sign(Vector3.Dot(perp2, -dir2)); //Mathf.Sign(Vector3.Dot(perp2, mpc2_dir_car2_dest));
                                                                  //float test_sign_22 = Mathf.Sign(Vector3.Dot(perp2, mpc2_dir_car2_dest));
                                                                  //float test_test_sign_22 = Mathf.Sign(SoA2);


                            //float Sign_Org_to_Destination_AoD1 = LookUpTableMPC2[i].SoD;  // same as sign_12
                            //float Sign_Org_to_Destination_AoA2 = -LookUpTableMPC2[i].SoA; // same as sign_21


                            //-----------------------------------------------------------------------------------
                            //-----------------------------------------------------------------------------------
                            //-----------------------------------------------------------------------------------


                            float gain12 = LookUpTableMPC2[i].AngularGain;                  // pre-calculated gain between MPC[i1] and MPC[i2]

                            float angular_gain = angular_gain1 * gain12 * angular_gain2;
                            float attenuation = antenna_gain1 * antenna_gain2 * att1 * angular_gain * att2;
                            if (attenuation > 0.0000001) // 10^(-7) => -140 dBm
                            {




                                //Debug.Log(20 * Mathf.Log10(attenuation));

                                //Path path2 = new Path(ChannelLinks[i_link], distance, attenuation);
                                /*Path3 path2 = new Path3(CarsCoordinates[i_car1],
                                    MPC_Array[i_mpc].Coordinates,
                                    MPC_Array[DMCNum + MPC1Num + seen_mpc2_from_i_mpc2].Coordinates,
                                    new Vector3(0, 0, 0),
                                    CarsCoordinates[i_car2],
                                    distance,
                                    attenuation);*/

                                PathChain pathChain2 = new PathChain(i_car1, i_mpc, DMCNum + MPC1Num + seen_mpc2_from_i_mpc2, 0, i_car2);
                                Path path2 = new Path(ChannelLinks[i_link], distance, attenuation);

                                Path_and_IDs path2_and_IDs = new Path_and_IDs(pathChain2, path2, 2);
                                IDArray[index] = 1;
                                HashMap.Add(index, path2_and_IDs);
                            }
                        }
                        //}
                    }
                }

            }
            else
            {
                int i_mpc3 = i_mpc - (DMCNum + MPC1Num + MPC2Num);
                Vector2Int IndexRange3 = LUTIndexRangeMPC3[i_mpc3];
                if (IndexRange3 != new Vector2Int(-1, -1))
                {
                    for (int i = IndexRange3.x; i < IndexRange3.y; i++)
                    {
                        // Data from LUT for MPC3
                        int seen_mpc3_from_i_mpc3_level2 = LookUpTableMPC3[i].MPC_IDs.y; // the ID of the mpc within MPC3_Array (not MPC_Array)
                        int seen_mpc3_from_i_mpc3 = LookUpTableMPC3[i].MPC_IDs.z; // the ID of the mpc within MPC3_Array (not MPC_Array)
                        float SoA3_car1 = LookUpTableMPC3[i].SoD; // we check if incident directions come from different sides
                        float SoA3_car2 = LookUpTableMPC3[i].SoA; // direction of arrival for the MPC2[i_mpc2]
                        if (SoA1 * SoA3_car1 < Optimization_Angle)
                        {
                            // from car2 to different MCP2s
                            int i_mpc3_car2 = i_car2 * MPCNum + DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3; // the ID of the mpc within the array that has length link_num*MPC_num
                            float test_dist = Results[i_mpc3_car2].distance;    // if the resulting distance is zero, then the ray hasn't hit anything
                            float comm_dist = Commands[i_mpc3_car2].distance;   // if the resulting distance is zero, then the ray hasn't hit anything
                            float SoA2 = SignOfArrival[i_mpc3_car2]; // car2 sees MPC3[seen_mpc3_from_i_mpc3] from left or right side

                            if (test_dist == 0 && comm_dist != 0 && SoA3_car2 * SoA2 < Optimization_Angle)
                            {
                                float distance = Commands[i_mpc_car1].distance + LookUpTableMPC3[i].Distance + Commands[i_mpc3_car2].distance;
                                Vector3 dir1 = Commands[i_mpc_car1].direction; // from car1 to the MPC
                                Vector3 dir3 = Commands[i_mpc3_car2].direction; // from car2 to the MPC

                                // find the angle of vision of MPCs from the car sight
                                /*
                                float phi1 = Mathf.Acos(Vector3.Dot(dir1, fwd1));
                                float phi2 = Mathf.Acos(Vector3.Dot(dir3, fwd2));

                                float antenna_gain1 = EADF_Reconstruction(Pattern, phi1);
                                float antenna_gain2 = EADF_Reconstruction(Pattern, phi2);
                                */
                                float antenna_gain1 = 1;
                                float antenna_gain2 = 1;
                                if (OmniAntennaFlag == false)
                                {
                                    // find the angle of vision of MPCs from the car sight
                                    float phi1 = Mathf.Acos(Vector3.Dot(dir1, fwd1));
                                    float phi2 = Mathf.Acos(Vector3.Dot(dir3, fwd2));

                                    antenna_gain1 = EADF_Reconstruction(Pattern, phi1);
                                    antenna_gain2 = EADF_Reconstruction(Pattern, phi2);
                                }

                                // Parameters of active MPC
                                float att1 = MPC_Attenuation[i_mpc];
                                Vector3 norm1 = MPC_Array[i_mpc].Normal;
                                //Vector3 coor1 = MPC_Array[i_mpc].Coordinates;
                                //Vector3 perp1 = MPC_Perp[i_mpc];
                                //Vector3 car1 = CarsCoordinates[i_car1];

                                //Vector3 from_car1_to_mpc_orgn = (coor1 - car1).normalized;
                                //float sign_AoA1 = Mathf.Sign(Vector3.Dot(perp1, from_car1_to_mpc_orgn));

                                float AoA1 = Mathf.Acos(Vector3.Dot(-dir1, norm1));             // (alpha) from car1 to MPC[i1]
                                float AoD1 = LookUpTableMPC3[i].AoD;                            // (beta)  AoD from MPC[i1] to MPC[i2]

                                float sign_11 = -Mathf.Sign(SoA1);
                                float sign_12 = LookUpTableMPC3[i].SoD;
                                /*
                                if (sign_AoA1 - sign_11 != 0)
                                {
                                    sign_11 = -sign_11;
                                }
                                */

                                if (sign_11 - sign_12 != 0)
                                {
                                    AoD1 = -AoD1;
                                }

                                float angular_gain1 = AngularGainFunc(AoA1, AoD1, thr1, thr2);  // AngularGainFunc(alpha, beta, thr1, thr2)

                                // Parameters of active MPC
                                float att3 = MPC_Attenuation[DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3];
                                Vector3 norm3 = MPC_Array[DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3].Normal;
                                //Vector3 coor3 = MPC_Array[DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3].Coordinates;
                                //Vector3 perp3 = MPC_Perp[DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3];
                                //Vector3 car2 = CarsCoordinates[i_car2];

                                //Vector3 from_mpc_dest_to_car2 = (car2 - coor3).normalized;
                                //float sign_AoD3 = Mathf.Sign(Vector3.Dot(perp3, from_mpc_dest_to_car2));

                                
                                float AoD3 = Mathf.Acos(Vector3.Dot(-dir3, norm3));             // (alpha) from MPC[i2] to car2
                                float AoA3 = LookUpTableMPC3[i].AoA;                            // (beta)  AoA from MPC[i2] to MPC[i1]

                                float sign_31 = -LookUpTableMPC3[i].SoA; // Mathf.Sign(Vector3.Dot(mpc2_dir_dest_orgn, perp2));
                                float sign_32 = Mathf.Sign(SoA2);
                                /*
                                if (sign_AoD3 - sign_32 != 0)
                                {
                                    sign_32 = -sign_32;
                                }
                                */

                                if (sign_31 - sign_32 != 0)
                                {
                                    AoD3 = -AoD3;
                                }

                                float angular_gain3 = AngularGainFunc(AoD3, AoA3, thr1, thr2);  // AngularGainFunc(alpha, beta, thr1, thr2)

                                float gain123 = LookUpTableMPC3[i].AngularGain;                 // pre-calculated gain between MPC[i1], MPC[i2], and MPC[i3]

                                float angular_gain = angular_gain1 * gain123 * angular_gain3;
                                float attenuation = antenna_gain1 * antenna_gain2 * att1 * angular_gain * att3;
                                if (attenuation > 0.0000001) // 10^(-7) => -140 dBm
                                {
                                    



                                    //Path path3 = new Path(ChannelLinks[i_link], distance, attenuation);
                                    /*Path3 path3 = new Path3(
                                        CarsCoordinates[i_car1],
                                        MPC_Array[i_mpc].Coordinates,
                                        MPC_Array[DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3_level2].Coordinates,
                                        MPC_Array[DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3].Coordinates,
                                        CarsCoordinates[i_car2],
                                        distance,
                                        attenuation
                                        );*/

                                    PathChain pathChain3 = new PathChain(i_car1, i_mpc, DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3_level2, DMCNum + MPC1Num + MPC2Num + seen_mpc3_from_i_mpc3, i_car2);
                                    Path path3 = new Path(ChannelLinks[i_link], distance, attenuation);

                                    Path_and_IDs path3_and_IDs = new Path_and_IDs(pathChain3, path3, 3);
                                    IDArray[index] = 1;
                                    HashMap.Add(index, path3_and_IDs);
                                }
                            }


                        }



                    }


                }
            }
        }


    }

    private float EADF_Reconstruction(NativeArray<Vector2> Pattern, float angle1)
    {
        System.Numerics.Complex Gain1 = 0;
        int L = Pattern.Length;
        // Analog of Inverse Fourier Transform
        for (int i = 0; i < L; i++)
        {
            float mu = -(L - 1) / 2 + i;
            System.Numerics.Complex db = new System.Numerics.Complex(Mathf.Cos(angle1 * mu), Mathf.Sin(angle1 * mu));
            System.Numerics.Complex complex_pattern = new System.Numerics.Complex(Pattern[i].x, Pattern[i].y);
            Gain1 += System.Numerics.Complex.Multiply(complex_pattern, db);
            
        }
        float Gain = (float)System.Numerics.Complex.Abs(Gain1);
        return Gain;
    }

    private float AngularGainFunc(float angle1, float angle2, float threshold1, float threshold2)
    {
        float Gain0 = 1;
        

        if (Mathf.Abs(angle1 - angle2) > threshold1)
        { Gain0 = Mathf.Exp(-12 * (Mathf.Abs(angle1 - angle2) - threshold1)); }

        /*
        float Gain1 = 1; // should be commented it according to Carl's Matlab script
        float Gain2 = 1;
        
        if (Mathf.Abs(angle1) > threshold2)
        { Gain1 = Mathf.Exp(-12 * (Mathf.Abs(angle1) - threshold2)); }

        if (Mathf.Abs(angle2) > threshold2)
        { Gain2 = Mathf.Exp(-12 * (Mathf.Abs(angle2) - threshold2)); }
        
         */
        // commented since the gain part from mpc side is already calculated

        float Gain = Gain0;// * Gain1 * Gain2; // should be commented according to Carl's Matlab script
        return Gain;
    }
}


