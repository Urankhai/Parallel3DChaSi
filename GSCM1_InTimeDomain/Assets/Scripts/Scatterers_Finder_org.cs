using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Scatterers_Finder_org : MonoBehaviour
{
    public float Vision_Area;
    private GameObject[] scatterers;
    private GameObject[] antennas;
    private GameObject ant1, ant2;
    // Start is called before the first frame update
    void Start()
    {

        scatterers = GameObject.FindGameObjectsWithTag("MPC1");
        Debug.Log("Number of DMCs = " + scatterers.Length);

        antennas = GameObject.FindGameObjectsWithTag("Antenna");
        Debug.Log("Number of antennas = " + antennas.Length);

        //Debug.Log("Number of seen DMCs = "+visibleDMCs.Length);

    }

    // Update is called once per frame
    void Update()
    {
        /*
        //Debug.Log("Position of the antenna "+transform.position);
        for (int j = 0; j < antennas.Length - 1; j++)
        {
            int ant_num = j+1;
            Debug.Log("Antenna "+ant_num);
            int count = 0;
            //List<GameObject> visibleDMCs;
            for (int i = 0; i < scatterers.Length - 1; i++)
            {

                Vector3 AB = antennas[j].transform.position - scatterers[i].transform.position;
                if (AB.magnitude < Vision_Area)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(scatterers[i].transform.position, AB, out hit))
                    {
                        if (hit.collider.tag == "Antenna")
                        {
                            var objectRenderer = scatterers[i].GetComponent<Renderer>();
                            objectRenderer.material.SetColor("_Color", Color.cyan);

                            count++;
                        }

                    }
                }
                else
                {
                    var objectRenderer = scatterers[i].GetComponent<Renderer>();
                    objectRenderer.material.SetColor("_Color", Color.grey);
                }
            }
            string msg = "Number of seen DMCs for ant " + ant_num + " is " + count;
            Debug.Log(msg);
        }
        */

        ant1 = antennas[0];
        ant2 = antennas[1];

        //Debug.Log("pos1 = " + ant1.transform.position);
        //Debug.Log("pos2 = " + ant2.transform.position);

        //int count1 = 0;
        //int count2 = 0;
        //List<GameObject> visibleDMCs;
        for (int i = 0; i < scatterers.Length - 1; i++)
        {
            //Debug.Log("Name of DMC "+scatterers[i].name);

            Vector3 AB1 = ant1.transform.position - scatterers[i].transform.position;
            Vector3 AB2 = ant2.transform.position - scatterers[i].transform.position;

            if ((AB1.magnitude < Vision_Area) && (AB2.magnitude < Vision_Area))
            {
                RaycastHit hit1;
                RaycastHit hit2;
                if ((Physics.Raycast(scatterers[i].transform.position, AB1, out hit1)) && (Physics.Raycast(scatterers[i].transform.position, AB2, out hit2)))
                {
                    if (hit1.collider.CompareTag("Antenna") && hit2.collider.CompareTag("Antenna"))
                    {
                        //Debug.Log("OK1");
                        var objectRenderer = scatterers[i].GetComponent<Renderer>();
                        objectRenderer.material.SetColor("_Color", Color.cyan);

                        //count1++;
                    }
                    else
                    {
                        var objectRenderer = scatterers[i].GetComponent<Renderer>();
                        objectRenderer.material.SetColor("_Color", Color.grey);
                    }

                }
            }
            else
            {
                var objectRenderer = scatterers[i].GetComponent<Renderer>();
                objectRenderer.material.SetColor("_Color", Color.grey);
            }

            /*
            if (AB2.magnitude < Vision_Area)
            {
                RaycastHit hit;
                if (Physics.Raycast(scatterers[i].transform.position, AB2, out hit))
                {
                    if (hit.collider.tag == "Antenna")
                    {
                        Debug.Log("OK2");
                        var objectRenderer = scatterers[i].GetComponent<Renderer>();
                        objectRenderer.material.SetColor("_Color", Color.blue);

                        count2++;
                    }

                }
            }
            else
            {
                var objectRenderer = scatterers[i].GetComponent<Renderer>();
                objectRenderer.material.SetColor("_Color", Color.grey);
            }
            */
        }

        //Debug.Log("Number of DMCs for ant1 = " + count1);
        //Debug.Log("Number of DMCs for ant2 = " + count2);
    }
}
