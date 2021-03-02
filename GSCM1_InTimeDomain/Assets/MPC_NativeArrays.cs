using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;



public class MPC_NativeArrays : MonoBehaviour
{
    public GameObject[] spawnTrafficVehicles;

    private NativeArray<Vector3> OldCoordinates;
    private NativeArray<Vector3> CarsCoordinates;
    private NativeArray<Vector3> CarsSpeed;

    private void OnEnable()
    {
        CarsCoordinates = new NativeArray<Vector3>(spawnTrafficVehicles.Length, Allocator.Persistent);
        OldCoordinates = new NativeArray<Vector3>(spawnTrafficVehicles.Length, Allocator.Persistent);
        CarsSpeed = new NativeArray<Vector3>(spawnTrafficVehicles.Length, Allocator.Persistent);
    }
    private void OnDestroy()
    {
        CarsCoordinates.Dispose();
        OldCoordinates.Dispose();
        CarsSpeed.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < spawnTrafficVehicles.Length; i++)
        {
            OldCoordinates[i] = spawnTrafficVehicles[i].transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        string ListOfCoordinates = "";
        for (int i = 0; i < spawnTrafficVehicles.Length; i++)
        {
            CarsCoordinates[i] = spawnTrafficVehicles[i].transform.position;
            CarsSpeed[i] = CarsCoordinates[i] - OldCoordinates[i];

            string temp_string = "Car with ID = " + i + " has direction " + CarsSpeed[i]*50 + ";";
            ListOfCoordinates += " " + temp_string;

            OldCoordinates[i] = CarsCoordinates[i];
        }
        //Debug.Log(ListOfCoordinates);
    }
}
