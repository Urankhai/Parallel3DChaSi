using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Scatterers_Finder : MonoBehaviour
{
    public float Vision_Area;
    public int MPC_order;
    private GameObject[] scatterers;
    private GameObject[] scatterers2;
    private GameObject[] scatterers3;
    private GameObject[] antennas;
    private GameObject[] mat_colors;
    private GameObject ant1, ant2;
    //private Color initial_color1;



    private

    // Start is called before the first frame update
    void Start()
    {
        string MPC_name = "MPC1";

        if (MPC_order == 2)
        {
            MPC_name = "MPC2";
        }
        else if (MPC_order == 3)
        {
            MPC_name = "MPC3";
        }
        

        scatterers = GameObject.FindGameObjectsWithTag(MPC_name);
        Debug.Log("Number of MPCs = " + scatterers.Length);

        /*
        scatterers2 = GameObject.FindGameObjectsWithTag("MPC2");
        Debug.Log("Number of MPC2s = " + scatterers2.Length);

        scatterers3 = GameObject.FindGameObjectsWithTag("MPC3");
        Debug.Log("Number of MPC3s = " + scatterers3.Length);*/

        antennas = GameObject.FindGameObjectsWithTag("Antenna");
        //Debug.Log("Number of antennas = " + antennas.Length);
        Debug.Log("Name the first antenna = " + antennas[0].name);


        mat_colors = GameObject.FindGameObjectsWithTag("mat_colors");
        //Debug.Log("Name the first material = " + mat_colors[1].name);

        //Debug.Log("Number of seen DMCs = "+visibleDMCs.Length);

        Debug.Log("MPC order = " + MPC_order);


    }

    void Update()
    {

        ant1 = antennas[0];
        ant2 = antennas[1];

        string MPC_name = "MPC1";

        if (MPC_order == 2)
        {
            MPC_name = "MPC2";
        }
        else if (MPC_order == 3)
        {
            MPC_name = "MPC3";
        }

        // LoS path
        Vector3 LoS = ant1.transform.position - ant2.transform.position;
        RaycastHit LoS_to, LoS_from;
        if ( (Physics.Raycast(ant2.transform.position, LoS, out LoS_to)) && (Physics.Raycast(ant1.transform.position, -LoS, out LoS_from)) )
        {
            if (LoS_to.collider.CompareTag("Antenna") && LoS_from.collider.CompareTag("Antenna"))
            {
                float LoS_dist = LoS.magnitude;
                Debug.DrawLine(ant1.transform.position, ant2.transform.position, Color.magenta);
            }
        }

        List<GameObject> seen_by_ant1 = new List<GameObject>();
        List<GameObject> seen_by_ant2 = new List<GameObject>();

        for (int i = 0; i < scatterers.Length; i++)
        {
            Vector3 AB1 = scatterers[i].transform.position - ant1.transform.position;
            if (AB1.magnitude < Vision_Area)
            {
                // firing a ray from an mpc
                RaycastHit hit_from1, hit_to1;
                if ((Physics.Raycast(ant1.transform.position, AB1, out hit_from1)) && (Physics.Raycast(scatterers[i].transform.position, -AB1, out hit_to1)))
                {

                    //Color initcolor = objectRenderer.material.GetColor("_Color");

                    if ((hit_from1.collider.CompareTag(MPC_name)) && (hit_to1.collider.CompareTag("Antenna")))
                    {
                        seen_by_ant1.Add(scatterers[i]);
                    }
                }
            }

        }


        for (int j = 0; j < scatterers.Length; j++)
        {
            Vector3 AB2 = scatterers[j].transform.position - ant2.transform.position;
            if (AB2.magnitude < Vision_Area)
            {

                // firing a ray from an mpc
                RaycastHit hit_from2, hit_to2;
                if (Physics.Raycast(ant2.transform.position, AB2, out hit_from2) && Physics.Raycast(scatterers[j].transform.position, -AB2, out hit_to2))
                {
                    if ((hit_from2.collider.CompareTag(MPC_name)) && (hit_to2.collider.CompareTag("Antenna")))
                    {
                        seen_by_ant2.Add(scatterers[j]);
                    }

                }
            }

        }



        if (MPC_order == 1)
        {
            List<GameObject> frst_ordr = new List<GameObject>();
            for (int k = 0; k < seen_by_ant1.Count; k++)
            {
                for (int l = 0; l < seen_by_ant2.Count; l++)
                {
                    if (seen_by_ant1[k].transform.position == seen_by_ant2[l].transform.position)
                    {
                        frst_ordr.Add(seen_by_ant1[k]);
                        var objectRenderer = seen_by_ant1[k].GetComponent<Renderer>();
                        //objectRenderer.material.SetColor("_Color", Color.cyan);
                        objectRenderer.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                        Debug.DrawLine(ant1.transform.position, seen_by_ant1[k].transform.position, Color.green);
                        Debug.DrawLine(seen_by_ant1[k].transform.position, ant2.transform.position, Color.green);
                    }

                }
            }
        }

        if (MPC_order == 2)
        {
            Debug.Log("Searchign for MPC2");
            // finding second order reflecting points
            List<GameObject> scnd_ordr = new List<GameObject>();
            for (int k = 0; k < seen_by_ant1.Count; k++)
            {
                for (int l = 0; l < seen_by_ant2.Count; l++)
                {
                    Vector3 AB = seen_by_ant1[k].transform.position - seen_by_ant2[l].transform.position;
                    if (AB.magnitude < 40.0f)
                    {
                        RaycastHit hit_to, hit_from;
                        if ((Physics.Raycast(seen_by_ant2[l].transform.position, AB, out hit_to)) && (Physics.Raycast(seen_by_ant1[k].transform.position, -AB, out hit_from)))
                        {
                            if (hit_to.collider.CompareTag(MPC_name) && hit_from.collider.CompareTag(MPC_name))
                            {
                                scnd_ordr.Add(seen_by_ant2[l]);

                                var objectRenderer1 = seen_by_ant2[l].GetComponent<Renderer>();
                                //objectRenderer1.material.SetColor("_Color", Color.red);
                                objectRenderer1.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                                var objectRenderer2 = seen_by_ant1[k].GetComponent<Renderer>();
                                //objectRenderer2.material.SetColor("_Color", Color.red);
                                objectRenderer2.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                                Debug.DrawLine(ant1.transform.position, seen_by_ant1[k].transform.position, Color.red);
                                Debug.DrawLine(seen_by_ant1[k].transform.position, seen_by_ant2[l].transform.position, Color.red);
                                Debug.DrawLine(seen_by_ant2[l].transform.position, ant2.transform.position, Color.red);
                            }
                        }

                    }
                }
            }
            scnd_ordr = scnd_ordr.Distinct().ToList();
            //Debug.Log("Number of second order reflectors = " + scnd_ordr.Count);
        }

        if (MPC_order == 3)
        {
            //Debug.Log("Searchign for scatterers seen by the mpcs of ant1");
            //Debug.Log("Number of all scatterers "+scatterers.Length);
            //Debug.Log("Number of multipath cmp1 "+seen_by_ant1.Count);
            List<GameObject> mpc_to_mpc = new List<GameObject>();

            for (int ii = 0; ii < scatterers.Length; ii++)
            {
                for (int kk = 0; kk < seen_by_ant1.Count; kk++)
                {
                    if (scatterers[ii].transform.position != seen_by_ant1[kk].transform.position)
                    {
                        Vector3 p2p = scatterers[ii].transform.position - seen_by_ant1[kk].transform.position;
                        //if (p2p.magnitude < Vision_Area)
                        if (p2p.magnitude < 40.0f)
                        {

                            RaycastHit hit_to, hit_from;
                            if ((Physics.Raycast(seen_by_ant1[kk].transform.position, p2p, out hit_to)) && (Physics.Raycast(scatterers[ii].transform.position, -p2p, out hit_from)))
                            {

                                if (hit_to.collider.CompareTag(MPC_name) && hit_from.collider.CompareTag(MPC_name))
                                {

                                    for (int ll = 0; ll < seen_by_ant2.Count; ll++)
                                    {
                                        if (scatterers[ii].transform.position != seen_by_ant2[ll].transform.position)
                                        {
                                        Vector3 p2pp2p = seen_by_ant2[ll].transform.position - scatterers[ii].transform.position;
                                        //string name_asd = seen_by_ant2[ll].name;
                                        //Vector3
                                        //Debug.Log("asdasdas " + seen_by_ant2[ll].name);
                                        //Debug.Log("asdasdas " + seen_by_ant2[ll].name.Length);
                                        //Debug.Log("asdasdas " + name_asd.Substring(4,name_asd.Length - 4));

                                        if (p2pp2p.magnitude < 40.0f)
                                        {
                                            RaycastHit hit_to2, hit_from2;
                                            if ((Physics.Raycast(scatterers[ii].transform.position, p2pp2p, out hit_to2)) && (Physics.Raycast(seen_by_ant2[ll].transform.position, -p2pp2p, out hit_from2)))
                                            {
                                                if (hit_to2.collider.CompareTag(MPC_name) && hit_from2.collider.CompareTag(MPC_name))
                                                {
                                                    
                                                    mpc_to_mpc.Add(scatterers[ii]);
                                                    var p2pRenderer1 = scatterers[ii].GetComponent<Renderer>();
                                                    //p2pRenderer1.material.SetColor("_Color", Color.green);
                                                    p2pRenderer1.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                                                    Debug.DrawLine(ant1.transform.position, seen_by_ant1[kk].transform.position, Color.green);
                                                    Debug.DrawLine(seen_by_ant1[kk].transform.position, scatterers[ii].transform.position, Color.yellow);
                                                    Debug.DrawLine(scatterers[ii].transform.position, seen_by_ant2[ll].transform.position, Color.yellow);
                                                    Debug.DrawLine(seen_by_ant2[ll].transform.position, ant2.transform.position, Color.green);
                                                }
                                            }
                                        }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }

            //Debug.Log("Number of seen scatterers = " + mpc_to_mpc.Count);
        }


    }


    private Collider[] mpcsNearby(GameObject current_antenna)
    {
        Collider[] objcts = Physics.OverlapSphere(current_antenna.transform.position, Vision_Area);
        return objcts;
    }
}
