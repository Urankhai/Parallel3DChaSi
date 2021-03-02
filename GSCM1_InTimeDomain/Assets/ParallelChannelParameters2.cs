using UnityEngine;
using Unity.Collections;
using Unity.Jobs;


public struct ParallelChannelParameters2 : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector2Int> Links;
    [ReadOnly] public NativeArray<Vector3> CarsPositions;
    [ReadOnly] public NativeArray<Vector3> CarsFwd;

    [ReadOnly] public NativeArray<float> MPC_attenuation;
    [ReadOnly] public NativeArray<V6> MPC_array;

    [ReadOnly] public NativeArray<int> ActiveIndexes;
    [ReadOnly] public NativeArray<SeenPath2> LUT;
    [ReadOnly] public NativeArray<Vector2Int> LinksEdges;
    [ReadOnly] public NativeArray<int> CarMPCStartID;
    [ReadOnly] public NativeArray<int> CarSeeMPCnum;

    //[WriteOnly] public NativeArray<Chain4> TestArray;
    [WriteOnly] public NativeArray<Path> OutArray;
    public void Execute(int index)
    {
        int i_link = LinkIDSearch(LinksEdges, index);
        int i_link_start = LinksEdges[i_link].x;
        int i_link_elmnt = index - i_link_start;

        int i_car1 = Links[i_link].x;
        int i_car2 = Links[i_link].y;

        int i_car1_num = CarSeeMPCnum[i_car1];
        int i_car2_num = CarSeeMPCnum[i_car2];

        int i_car1_link_mpc = Mathf.FloorToInt(i_link_elmnt / i_car2_num);
        int i_car2_link_mpc = i_link_elmnt - i_car1_link_mpc * Mathf.FloorToInt(i_link_elmnt / i_car2_num);

        //int i_car1_mpc = ActiveIndexes[CarMPCStartID[i_car1] + i_car1_link_mpc];
        //int i_car2_mpc = ActiveIndexes[CarMPCStartID[i_car2] + i_car2_link_mpc];
    }

    private int LinkIDSearch(NativeArray<Vector2Int> edges, int index)
    {
        int i_link = 0;
        for (int i = 0; i < edges.Length; i++)
        {
            if (index >= edges[i].x && index <= edges[i].y)
            { i_link = i; break; }
        }
        return i_link;
    }
}

