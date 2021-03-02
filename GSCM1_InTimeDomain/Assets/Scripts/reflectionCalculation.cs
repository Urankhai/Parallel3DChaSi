using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reflectionCalculation : MonoBehaviour
{

    public int distanceMax;

    Vector3 target;

    float perpDistance;
    RaycastHit hit;
    RaycastHit hitWall;
    RaycastHit hitReflect;
    RaycastHit reflect;
    RaycastHit hitLOS;



    // Update is called once per frame
    void Update()
    {

        foreach (Collider Building in wallsNearby())
        {
            calculate(Building);
            foreach (GameObject reciever in GameObject.FindGameObjectsWithTag("rx"))
            {
                reflectCalculate(reciever);
            }
        }
    }

    void calculate(Collider Building)
    {
        if (Physics.Raycast(transform.position, Building.transform.position - transform.position, out hitWall, distanceMax))
        {
            Vector3 perpAngle = hitWall.normal;
            float hitDistance = hitWall.distance;
            Vector3 hitDirection = Building.transform.position - transform.position;
            float hitAngle = Vector3.Angle((hitWall.normal), -hitDirection.normalized);

            perpDistance = hitDistance * Mathf.Cos((hitAngle * Mathf.PI) / 180);

            target = transform.position - (hitWall.normal * perpDistance * 2);


        }
    }

    void reflectCalculate(GameObject reciever)
    {
        if (Physics.Raycast(reciever.transform.position, target - reciever.transform.position, out hit, distanceMax))
        {
            if (Physics.Raycast(transform.position, hit.point - transform.position, out hitReflect, distanceMax) && (hit.point==hitReflect.point))
            //if ( hit.point == hitReflect.point )
            {
                //Debug.DrawLine(transform.position, transform.position - (hitWall.normal * perpDistance * 2), Color.red);
                //Debug.DrawLine(reciever.transform.position, target, Color.red);
                Debug.DrawLine(transform.position, hit.point, Color.green);
                Debug.DrawLine(hit.point, reciever.transform.position, Color.green);
                
                //Debug.DrawLine(transform.position, reciever.transform.position, Color.yellow);
            }
        }
        Debug.DrawLine(transform.position, reciever.transform.position, Color.yellow);
        if (Physics.Raycast(transform.position, reciever.transform.position - transform.position, out hitLOS))// && ((hitLOS.point - reciever.transform.position).magnitude < 10) )
        {
            //Debug.Log("!!!!![" + (hitLOS.point - reciever.transform.position).magnitude);
            Vector3 ant2obs = hitLOS.point - transform.position;
            Vector3 ant2ant = reciever.transform.position - transform.position;

            Debug.DrawLine(transform.position, reciever.transform.position, Color.yellow);

            if ( ant2obs.magnitude > ant2ant.magnitude+3)
            {
                Debug.DrawLine(transform.position, reciever.transform.position, Color.yellow);
                //Debug.Log("ant2obs = " +ant2obs.magnitude + "; ant2ant = " +ant2ant.magnitude);
            }
            else
            {
                Debug.DrawLine(transform.position, reciever.transform.position, Color.red);
            }
        }
    }


    private Collider[] wallsNearby()
    {
        Collider[] walls = Physics.OverlapSphere(transform.position, distanceMax);
        return walls;
    }
}
