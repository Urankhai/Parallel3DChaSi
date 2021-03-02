using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Closing_polygons : MonoBehaviour
{
    public Material ObsPoint_Material;
    private GameObject[] Buildings;
    private GameObject[] Observation_Points;


    // Start is called before the first frame update
    void Start()
    {
        // Declaring buildings and observation points
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        Observation_Points = GameObject.FindGameObjectsWithTag("Observation_Points");
        // subset of the seen Buldings
        List<GameObject> Building_list = new List<GameObject>();

        // Going through all observation points
        for (int k = 0; k < Observation_Points.Length; k++)
        {
            // analyzing which buidings are seen
            for (int b = 0; b < Buildings.Length; b++)
            {
                Vector3[] vrtx = Buildings[b].GetComponent<MeshFilter>().mesh.vertices;
                Vector3[] nrml = Buildings[b].GetComponent<MeshFilter>().mesh.normals;

                int seen_vrtx_count = 0;

                for (int v = 0; v < vrtx.Length; v++)
                {
                    if (vrtx[v].y == 0)
                    {
                        if (!Physics.Linecast(Observation_Points[k].transform.position, vrtx[v] + 1.1f * nrml[v]))
                        {
                            seen_vrtx_count += 1;
                        }
                    }
                }
                if (seen_vrtx_count != 0)
                {
                    if (!Building_list.Contains(Buildings[b]))
                    {
                        Building_list.Add(Buildings[b]);
                    }
                }
            }
        }

        // deactivating nonseen buildings
        for (int b = 0; b < Buildings.Length; b++)
        {
            if (!Building_list.Contains(Buildings[b]))
            {
                Buildings[b].SetActive(false);
            }

        }
        Debug.Log("The number of seen Buildings = " + Building_list.Count);

        int checked_k = 10000;
        // analyzing each seen building separately
        for (int k = 0; k < Building_list.Count; k++)
        {


            Vector3[] vrtx = Building_list[k].GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] nrml = Building_list[k].GetComponent<MeshFilter>().mesh.normals;

            List<Vector3> floor_vrtx = new List<Vector3>();
            List<Vector3> floor_nrml = new List<Vector3>();
            var pairs = new List<V6>();

            List<Vector3> possible_vrtx = new List<Vector3>();
            List<Vector3> possible_nrml = new List<Vector3>();


            for (int l = 0; l < vrtx.Length; l++)
            {
                if (vrtx[l].y == 0 && Mathf.Abs(nrml[l].y) < 0.9)
                {
                    floor_vrtx.Add(vrtx[l]);
                    floor_nrml.Add(nrml[l]);
                    
                    GameObject v_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    v_sphere.transform.position = vrtx[l];
                    Destroy(v_sphere.GetComponent<SphereCollider>()); // remove collider
                    var sphereRenderer = v_sphere.GetComponent<Renderer>();
                    sphereRenderer.material = ObsPoint_Material;

                    // adding coordinates and normals of the vertices into a single object V6 (like a N x 6 matrix)
                    V6 valid_pair = new V6(vrtx[l], nrml[l]);
                    pairs.Add(valid_pair);

                }
            }


            // lb_start - the left bottom vertex with 
            Search_for_LeftBottom(pairs, out V6 lb_start, out V6 lb_end);


            //GameObject v_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            //v_sphere.transform.position = lb_start.Coordinates;
            //Destroy(v_sphere.GetComponent<SphereCollider>()); // remove collider
            Debug.DrawLine(lb_start.Coordinates, lb_start.Coordinates + 3.0f * lb_start.Normal, Color.green, 5.0f);
            //v_sphere.transform.position = lb_end.Coordinates;
            //Destroy(v_sphere.GetComponent<SphereCollider>()); // remove collider
            Debug.DrawLine(lb_end.Coordinates, lb_end.Coordinates + 3.0f * lb_end.Normal, Color.blue, 5.0f);
            //var sphereRenderer = v_sphere.GetComponent<Renderer>();
            //sphereRenderer.material = ObsPoint_Material;





            
            // TODO: the method is assuming that all Normals have zero Y components (it may not work for an arbitrary Normal with nonzero Normal.y)
            List<V6> remaining_pairs = pairs;
            List<V6> close_loop_poly = new List<V6>();

            float steps_out = 2.0f;

            // remove the elements step by step from the list of vrtx and nrml
            V6 next_V6_to_remove = lb_start;
            close_loop_poly.Add(next_V6_to_remove);         // adding to the close loop list
            remaining_pairs.Remove(next_V6_to_remove);      // removing from the consideration
            int if_loop_is_closed = 0;                      // flag indicating the end of the loop
            int to_avoid_the_first_step_stop = 0;
            // remove elements until we consider all the elements of the outer side
            while (remaining_pairs.Count > 0 && if_loop_is_closed != 1)
            {

                // define perpendicular direction to the given Normal of next_V6_to_remove (it may not work for an arbitrary Normal with nonzero Normal.y)
                Vector3 temp_perpendicular = new Vector3(next_V6_to_remove.Normal.z, 0, -next_V6_to_remove.Normal.x).normalized; // a vector product in 3D with (0, 1, 0)

                // several vertexes can lay in the direction of temp_perpendicular
                List<V6> possible_pairs = new List<V6>();
                List<V2> distance_index = new List<V2>(); // to track distances and indexes
                List<V3> distence_index_ncount = new List<V3>(); // to track distances, indexes, and number of normals


                int possibilities_count = 0;
                int check_count1 = 0;
                for (int j = 0; j < remaining_pairs.Count; j++)
                {
                    //if (j == 0 && possibilities_count == 0)
                    //{
                    //Debug.DrawLine(next_V6_to_remove.Coordinates + 1.0f * next_V6_to_remove.Normal, remaining_pairs[j].Coordinates, Color.yellow, 5.0f);
                    //}
                    if ((next_V6_to_remove.Normal - remaining_pairs[j].Normal).magnitude < 0.05)
                    {
                        Vector3 temp_direction = remaining_pairs[j].Coordinates - next_V6_to_remove.Coordinates;
                        if ((temp_direction.normalized - temp_perpendicular).magnitude < 0.037) // in order to take into account numerical errors
                        {
                            Vector3 A2B = remaining_pairs[j].Coordinates - (next_V6_to_remove.Coordinates + steps_out * next_V6_to_remove.Normal);
                            if (Physics.Raycast(next_V6_to_remove.Coordinates + steps_out * next_V6_to_remove.Normal, A2B.normalized, out RaycastHit hitInfo, (A2B.magnitude - 0.01f)))
                            {
                                if (Building_list[k].name == "Building256" && remaining_pairs.Count == 4)
                                {
                                    //Debug.Log("hit Info " + hitInfo);
                                    Debug.DrawLine(next_V6_to_remove.Coordinates + steps_out * next_V6_to_remove.Normal, next_V6_to_remove.Coordinates + steps_out * next_V6_to_remove.Normal + A2B.normalized * (A2B.magnitude - 0.01f), Color.cyan, Mathf.Infinity);
                                }
                            }
                            else
                            {
                                //if (Building_list[k].name == "Building284" && remaining_pairs.Count == 13)
                                {
                                    //Debug.DrawLine(next_V6_to_remove.Coordinates + 1.0f * next_V6_to_remove.Normal, next_V6_to_remove.Coordinates + 1.0f * next_V6_to_remove.Normal + A2B.normalized * (A2B.magnitude - 0.01f), Color.cyan, 5.0f);
                                }

                                possible_pairs.Add(remaining_pairs[j]);

                                int nCount = 1;
                                // searching if there are vertices with the same coordinates but different normals
                                for (int jj = 0; jj < remaining_pairs.Count; jj++)
                                {
                                    if (remaining_pairs[j].Coordinates == remaining_pairs[jj].Coordinates && j != jj)
                                    {
                                        nCount += 1;
                                    }
                                }

                                if (nCount > 2)
                                {
                                    Debug.Log("Something wrong: a vertex has more than two normals");
                                }
                                if (nCount == 2)
                                {
                                    check_count1 += 1;
                                }

                                V3 dIDnCount = new V3(temp_direction.magnitude, possibilities_count, nCount);
                                distence_index_ncount.Add(dIDnCount);

                                possibilities_count += 1;


                            }
                        }
                    }
                }

                //Debug.Log("How many vertices on the same side of building #" + Building_list[k].name + " have left " + distence_index_ncount.Count);


                if (distence_index_ncount.Count == 0)
                {
                    Debug.Log("Check here! Buildign name " + Building_list[k].name);
                }


                List<V3> SortedByDistance = distence_index_ncount.OrderBy(d => d.Distance).ToList();

                if (k == 50)
                {
                    Debug.Log("Check!");
                }

                int srch_steps = 0;
                while (SortedByDistance[srch_steps].Normals_Number < 2 && if_loop_is_closed != 1)
                {
                    if (checked_k != k)
                    {
                        Debug.Log("Drawing lines for building #" + k);
                        checked_k = k;
                    }
                    // removing all the vertecies that 
                    Debug.DrawLine(next_V6_to_remove.Coordinates, possible_pairs[SortedByDistance[srch_steps].Index].Coordinates, Color.red, 5.0f);
                    next_V6_to_remove = possible_pairs[SortedByDistance[srch_steps].Index];
                    close_loop_poly.Add(next_V6_to_remove);
                    remaining_pairs.Remove(next_V6_to_remove);
                    srch_steps += 1;
                    if (next_V6_to_remove.Coordinates == lb_end.Coordinates) // check if the last vertex does not match with the last vertex of the loop
                    {
                        if_loop_is_closed = 1;
                        srch_steps -= 1;
                    }
                }

                if (k == 12)
                {
                    Debug.Log("Check ");
                }

                if (SortedByDistance[srch_steps].Normals_Number > 1)
                {
                    if (checked_k != k)
                    {
                        Debug.Log("Drawing lines for building #" + k);
                        checked_k = k;
                    }
                    Debug.DrawLine(next_V6_to_remove.Coordinates, possible_pairs[SortedByDistance[srch_steps].Index].Coordinates, Color.red, 5.0f);
                    next_V6_to_remove = possible_pairs[SortedByDistance[srch_steps].Index];
                    close_loop_poly.Add(next_V6_to_remove);
                    remaining_pairs.Remove(next_V6_to_remove);

                    int matching_number = 0;
                    for (int iii = 0; iii < remaining_pairs.Count; iii++)
                    {
                        if (next_V6_to_remove.Coordinates == remaining_pairs[iii].Coordinates)
                        {
                            next_V6_to_remove = remaining_pairs[iii];
                            matching_number += 1;

                            close_loop_poly.Add(next_V6_to_remove);
                            remaining_pairs.Remove(next_V6_to_remove);
                        }
                    }
                }


                to_avoid_the_first_step_stop += 1;
            }

            //Debug.Log("Number of Vertices of the building = " + pairs.Count);
            //Debug.Log("Number of Vertices of the building after closing the loop = " + close_loop_poly.Count);

        }

    }





















    // function that searches for the most left bottom vertex to define the start and end points of the building's ground polygon
    void Search_for_LeftBottom(List<V6> valid_vrtx_nrml, out V6 lb_start, out V6 lb_end)
    {
        var SortedByX = valid_vrtx_nrml.OrderBy(xx => xx.Coordinates.x).ToList();

        // several of vertices may have the same X value among which we need to choose the most left
        var list_min_by_x = new List<V6>();
        int i = 1;
        list_min_by_x.Add(SortedByX[0]);
        while (SortedByX[0].Coordinates == SortedByX[i].Coordinates)
        {
            list_min_by_x.Add(SortedByX[i]);
            i += 1;
        }
        if (i == 1)
        {
            Debug.Log("WARNING! Something wrong: the most left bottom vertex has to have two normals");
            lb_start = new V6(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
            lb_end = new V6(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
            return;
        }


        // searching for the most bottom and left vertex in the building's bottom polygon
        var SortedByZ = list_min_by_x.OrderByDescending(zz => zz.Coordinates.z).ToList();

        // shity assigning of the values, I do not understand it yet
        lb_start = new V6(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
        lb_end = new V6(new Vector3(0, 0, 0), new Vector3(0, 0, 0));

        // defining the start and end vertices
        if (SortedByZ[0].Coordinates == SortedByZ[1].Coordinates)
        {
            if (SortedByZ[0].Normal.z > SortedByZ[1].Normal.z)
            {
                lb_start = SortedByZ[0];
                lb_end = SortedByZ[1];
            }
            else
            {
                lb_start = SortedByZ[1];
                lb_end = SortedByZ[0];
            }
        }

    }


    public class V6
    {
        public Vector3 Coordinates;
        public Vector3 Normal;


        public V6(Vector3 vrtx, Vector3 nrml)
        {
            Coordinates = vrtx;
            Normal = nrml;
        }

    }

    public class V2
    {
        public float Distance;
        public int Index;

        public V2(float d, int ID)
        {
            Distance = d;
            Index = ID;
        }
    }

    public class V3
    {
        public float Distance;      // tracking the distances
        public int Index;           // tracking the indeces
        public int Normals_Number;  // counting the number of normals

        public V3(float d, int ID, int nCount)
        {
            Distance = d;
            Index = ID;
            Normals_Number = nCount;
        }
    }

    public class V6phi
    {
        public Vector3 Coordinates;
        public Vector3 Normal;
        public float Angles;
        public V6phi(Vector3 vrtx, Vector3 nrml, float phi)
        {
            Coordinates = vrtx;
            Normal = nrml;
            Angles = phi;
        }
    }
}
