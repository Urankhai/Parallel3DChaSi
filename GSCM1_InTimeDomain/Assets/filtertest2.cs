using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class filtertest2 : MonoBehaviour
{
    public struct TestData
    {
        public int num;
        public byte filter;

        public TestData(int num, byte filter)
        {
            this.num = num;
            this.filter = filter;
        }
    }

    void Start()
    {
        NativeArray<TestData> nData = new NativeArray<TestData>(2, Allocator.TempJob);

        // Won't filter to use...or won't be filtered away? Not sure how to interpret
        nData[0] = new TestData(0, 0);

        // Opposite, I expect one of these to end up having a value of 1
        nData[1] = new TestData(0, 1);

        NativeList<int> indexes = new NativeList<int>(Allocator.TempJob);

        FilterJob filterJob = new FilterJob();
        filterJob.nData = nData;

        JobHandle handle = filterJob.ScheduleAppend(indexes, nData.Length, 1); //filterJob.ScheduleFilter(indexes, 32);

        // Even forcing a complete here doesn't seem to matter. As best as I can tell the filter doesn't run
        // handle.Complete();

        IncrementIndices incJob = new IncrementIndices();
        incJob.indexes = indexes;
        incJob.nData = nData;
        handle = incJob.Schedule(handle);

        handle.Complete();

        Debug.Log(nData[0].num + " " + nData[1].num);

        nData.Dispose();
        indexes.Dispose();
    }

    private struct FilterJob : IJobParallelForFilter
    {
        [ReadOnly]
        public NativeArray<TestData> nData;

        public bool Execute(int index)
        {
            return nData[index].filter > 0;
        }
    }

    private struct IncrementIndices : IJob
    {
        public NativeArray<TestData> nData;

        [ReadOnly]
        public NativeList<int> indexes;

        public void Execute()
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                TestData value = nData[indexes[i]];
                value.num++;
                nData[indexes[i]] = value;
            }
        }
    }
}
