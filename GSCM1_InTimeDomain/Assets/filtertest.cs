using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class filtertest : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        NativeList<int> indices = new NativeList<int>(Allocator.TempJob);
        NativeArray<int> numbers = new NativeArray<int>(5, Allocator.TempJob);
        numbers[0] = 0;
        numbers[1] = 1;
        numbers[2] = 0;
        numbers[3] = 1;
        numbers[4] = 0;
        var handle = default(JobHandle);

        /*
        var condition = new Condition
        {
            indices = indices,
            numbers = numbers
        };
        handle = condition.Schedule(handle);
        */

        var append = new Append
        {
            numbers = numbers
        };
        handle = append.ScheduleAppend(indices, numbers.Length, 1, handle);

        var filter = new Filter
        {
            numbers = numbers,
        };
        handle = filter.ScheduleFilter(indices, 1, handle);
        handle.Complete();
        Debug.Log(indices.Length);
        indices.Dispose();
        numbers.Dispose();
    }

    private struct Condition : IJob
    {
        public NativeList<int> indices;
        public NativeArray<int> numbers;
        public void Execute()
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                if (numbers[i] == 1)
                {
                    indices.Add(i);
                }
            }
        }
    }

    // the first filter job must be scheduled with ScheduleAppend to create the initial NativeList containing the filtered indices to be iterated through (not required, if the Native List Already Exists)
    private struct Append : IJobParallelForFilter
    {
        public NativeArray<int> numbers;
        public bool Execute(int index)
        {
            return numbers[index] == 1;
        }
    }

    // can chain a few of these for further filtering
    private struct Filter : IJobParallelForFilter
    {
        public NativeArray<int> numbers;
        public bool Execute(int index)
        {
            Debug.Log(index);
            //return true;
            return (index == 1);
        }
    }
}
