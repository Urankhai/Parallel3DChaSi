using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Collections;


public class AllVehiclesControl : MonoBehaviour
{
    public NavMeshAgent[] carsArray;
    public Transform[] destinations;

    [HideInInspector] public NativeArray<Vector3> OldCoordinates;
    [HideInInspector] public NativeArray<Vector3> CarCoordinates;
    [HideInInspector] public NativeArray<Vector3> CarForwardVect;
    [HideInInspector] public NativeArray<Vector3> CarsSpeed;

    private void OnEnable()
    {
        CarCoordinates = new NativeArray<Vector3>(carsArray.Length, Allocator.Persistent);
        OldCoordinates = new NativeArray<Vector3>(carsArray.Length, Allocator.Persistent);
        CarsSpeed = new NativeArray<Vector3>(carsArray.Length, Allocator.Persistent);
        CarForwardVect = new NativeArray<Vector3>(carsArray.Length, Allocator.Persistent);
    }
    private void OnDestroy()
    {
        CarCoordinates.Dispose();
        OldCoordinates.Dispose();
        CarsSpeed.Dispose();
        CarForwardVect.Dispose();
    }
    
    

    void Start()
    {
        // defining cars and their positions
        for (int i = 0; i < carsArray.Length; i++)
        { 
            // turn on AI path finding
            carsArray[i].SetDestination(destinations[i].position);
            // Initial positions of all vehicles
            OldCoordinates[i] = carsArray[i].transform.Find("Antenna").position;
        }
    }

    //private void FixedUpdate()
    private void FixedUpdate()
    {
        for (int i = 0; i < carsArray.Length; i++)
        {
            // Current positions of all vehicles
            CarCoordinates[i] = carsArray[i].transform.Find("Antenna").position;
            CarForwardVect[i] = carsArray[i].transform.Find("Antenna").forward;
            CarsSpeed[i] = CarCoordinates[i] - OldCoordinates[i];
            // Updating previous positions of vehicles
            OldCoordinates[i] = CarCoordinates[i];
        }
    }


    
}
