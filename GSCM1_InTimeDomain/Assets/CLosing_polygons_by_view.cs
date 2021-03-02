using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;


public class CLosing_polygons_by_view : MonoBehaviour
{
    

    public Material ObsPoint_Material;
    public Material CG_Material;
    private GameObject[] Buildings;
    private GameObject[] Observation_Points;


    // Start is called before the first frame update
    void Start()
    {
        // Declaring buildings and observation points
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        // The number of observation points should not be too big, a few is enough
        Observation_Points = GameObject.FindGameObjectsWithTag("Observation_Points");
        // Define the main road direction
        Vector3 main_axis = (Observation_Points[0].transform.position - Observation_Points[1].transform.position).normalized;
        Vector3 perp_axis = new Vector3(main_axis.z, 0, -main_axis.x);

        // subset of the seen Buldings
        List <GameObject> Building_list = new List<GameObject>();

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

        
        // analyzing each seen building separately
        for (int k = 0; k < Building_list.Count; k++)
        {
            // writing MPCs into a txt file
            //string writePath;
            List<string> Obj_List = new List<string>();

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

                    // adding coordinates and normals of the vertices into a single object V6 (like a N x 6 matrix)
                    V6 valid_pair = new V6(vrtx[l], nrml[l]);
                    pairs.Add(valid_pair);

                }
            }

            for (int l = 0; l < floor_vrtx.Count-1; l++)
            {
                Debug.DrawLine(floor_vrtx[l], floor_vrtx[l+1], Color.cyan, 5.0f);
            }
            
            
            List<Vector3> floor_points = Remove_repetiting_points(floor_vrtx);

            Vector3 cg = new Vector3(0.0f, 0.0f, 0.0f);

            for (int l = 0; l < floor_points.Count; l++)
            {
                cg = cg + floor_points[l];

                /*
                GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                cleared_sphere.transform.position = floor_points[l];
                Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                sphereRenderer.material = ObsPoint_Material;
                cleared_sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                */

            }
            cg /= floor_points.Count;


            // update cg that takes into account the closest verteces
            Search_for_weighted_CG(floor_points, cg, 1.0f, out Vector3 updated_cg);

            /*
            GameObject upc_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            upc_sphere.transform.position = updated_cg;
            Destroy(upc_sphere.GetComponent<SphereCollider>()); // remove collider
            var upc_sphereRenderer = upc_sphere.GetComponent<Renderer>();
            upc_sphereRenderer.material = CG_Material;

            // search for max distance
            float max_dist = 0;
            for (int l = 0; l < floor_points.Count; l++)
            {
                float temp_dist = (floor_points[l] - updated_cg).magnitude;
                if (temp_dist > max_dist) { max_dist = temp_dist; }
            }

            // defining four observation points around a building that are on the car's antenna height
            Vector3 height_of_car = new Vector3(0.0f, 1.3f, 0.0f);
            Vector3[] check_points = new Vector3[4];
            float how_much_step_out_from_cg = 3.0f;
            check_points[0] = updated_cg + main_axis * (max_dist * how_much_step_out_from_cg) + height_of_car;
            check_points[1] = updated_cg + perp_axis * (max_dist * how_much_step_out_from_cg) + height_of_car;
            check_points[2] = updated_cg - main_axis * (max_dist * how_much_step_out_from_cg) + height_of_car;
            check_points[3] = updated_cg - perp_axis * (max_dist * how_much_step_out_from_cg) + height_of_car;

            // We create 8 observation points that are located outside of the building and are surrounding it
            // All the seen vertices of a floor, we save into a List < List < V4 > > structure
            List<List<V4>> seen_points = new List<List<V4>>();

            // Choos the check point, for which verteces are highlighted
            int check_point = 4;
            for (int l = 0; l < 8; l++)
            {

                Vector3 obs_point = new Vector3(0,0,0);
                if (l % 2 == 0) { obs_point = check_points[l / 2];}
                else
                { // if l is odd, we take half of the sum of two neighboring points
                    if (l != 7){ obs_point = (check_points[(l - 1) / 2] + check_points[(l + 1) / 2])/2; }
                    else { obs_point = (check_points[3] + check_points[0])/2; }
                }
                Vector3 obs2cg = (updated_cg - obs_point).normalized;
                Vector3 obs2right = -Vector3.Cross(obs2cg, new Vector3(0, 1, 0));
                if (l == check_point)
                {
                    Debug.DrawLine(obs_point, obs_point + obs2cg * 3.0f, Color.green, 5.0f);
                    Debug.DrawLine(obs_point, obs_point + obs2right * 3.0f, Color.red, 5.0f);
                }

                // defining list of seen point for each observation point
                List<V4> side_seen_points = new List<V4>();
                for (int n = 0; n < floor_points.Count; n++)
                {
                    // defining direction and distance from the observation point to floor points
                    Vector3 temp_dir = (floor_points[n] - obs_point);
                    float temp_dist = temp_dir.magnitude;
                    Vector3 unit_dir = temp_dir.normalized;

                    if( Physics.Raycast(obs_point, unit_dir, out RaycastHit hitInfo, temp_dist - 0.01f) )
                    {
                        //Debug.Log("Hit something");
                    }
                    else
                    {
                        float temp_cos = 1.0f - Vector3.Dot(unit_dir, obs2right); // 1 - cos(phi) from 0 to 2
                        V4 point_angle = new V4(floor_points[n], temp_cos);
                        side_seen_points.Add(point_angle);

                        
                        if (l == check_point)
                        {
                            GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                            cleared_sphere.transform.position = floor_points[n];
                            Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                            var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                            sphereRenderer.material = ObsPoint_Material;
                            cleared_sphere.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                        }
                        
                    }

                }
                var SortedByCos = side_seen_points.OrderBy(xx => xx.Distance).ToList();
                seen_points.Add(SortedByCos);
                //for (int m = 0; m < SortedByCos.Count; m++)
                //{
                //    Obj_List.Add("Obs point #" + l + "," + SortedByCos[m].Coordinates.x + "," + SortedByCos[m].Coordinates.z + "," + SortedByCos[m].Distance);
                //}
                
            }
            // defining the text file and writing
            // writePath = Application.dataPath + "/obs2_point" + ".csv";
            // WriteFile(Obj_List, writePath);

            // Check the very first list and the very last list since they are overlaping with each other
            // This is done to avoid unseen verteces from the first observation point (Exampl: Berlin, building 302)
            int temp_entrance_pos = 0;
            for (int l = 0; l < seen_points[0].Count; l++)
            {
                int current_pos = 0;
                int flag = 0;
                while(flag == 0 && temp_entrance_pos + current_pos < seen_points[7].Count)
                {
                    if (seen_points[0][l].Coordinates == seen_points[7][temp_entrance_pos + current_pos].Coordinates)
                    {
                        flag = 1;
                        temp_entrance_pos = temp_entrance_pos + current_pos + 1;
                        if (l > 0 && current_pos > 0)
                        {
                            seen_points[0].Remove(seen_points[0][l-1]);
                            l -= 1;
                        }
                    }
                    else { current_pos += 1; }
                }
            }

            List<V4> previous_list = seen_points[0];
            for (int n = 1; n < seen_points.Count; n++)
            //for (int n = 3; n < 4; n++)
            {
                // ASSUMPTION1: the first vertex from the next set is always existing in the previous set
                // ASSUMPTION2: the last vertex from the previous set is always existing in the next set
                // TODO: this can be formulated as a theorem and proven

                // According to the assumption, we search for the first vertex location in the previous set
                List<int> coincidence_positions_prev = new List<int>();
                List<int> coincidence_positions_next = new List<int>();

                // it is very crucial to find the first entrance that is located as far as possible (in terms of sequence position)
                int first_entrance_pos = 0;
                for (int i = 0; i < previous_list.Count; i++)
                {
                    if (previous_list[i].Coordinates == seen_points[n][0].Coordinates) 
                    { first_entrance_pos = i; }
                }
                coincidence_positions_prev.Add(first_entrance_pos);
                coincidence_positions_next.Add(0);

                // this wierd step is done in order to separate all previous positions from the first entrance
                // TODO: this needs to be checked
                int previous_entrance_pos = first_entrance_pos;
                int entrance_num = 1;
                for (int nn = 1; nn < seen_points[n].Count; nn++)
                {
                    
                    int entrance_flag = 0;
                    int entrance_pos = 1;
                    while (entrance_flag == 0 && previous_entrance_pos + entrance_pos < previous_list.Count)
                    {
                        if (previous_list[previous_entrance_pos + entrance_pos].Coordinates == seen_points[n][nn].Coordinates)
                        {
                            entrance_flag = 1;
                            previous_entrance_pos = previous_entrance_pos + entrance_pos;
                            // adding indexes to the set of entrance indexes
                            coincidence_positions_prev.Add(previous_entrance_pos);
                            coincidence_positions_next.Add(nn);
                            Debug.Log("Vertex " + nn + " is found in the previous list on position " + previous_entrance_pos);

                            
                            entrance_num += 1;
                        }
                        
                        entrance_pos += 1;
                    }
                }

                // defining sets: previous set is referred as prev[]; next set is referred as next[].
                // Assumption: in the final vertexes sequence, prev[0] >= next[0] and prev[end] >= next[end]
                // here ">=" mean higher or on the same place in terms of sequence position
                List<V4> temp_list = new List<V4>();
                // add everything from previous_list before the first entrance
                for (int nnn = 0; nnn < coincidence_positions_prev[0]; nnn++)
                {
                    temp_list.Add(previous_list[nnn]);
                }
                // add between
                for (int j = 0; j < entrance_num - 1; j++)
                {
                    // for the explanation: coincidence_positions_prev = prev, coincidence_positions_next = next
                    // there are four options:
                    // 1) prev[i+1] - prev[i] = 1 and next[i+1] - next[i] = 1 => temp_list.Add(previous_list[prev[i]])
                    // 2) prev[i+1] - prev[i] > 1 and next[i+1] - next[i] = 1 => temp_list.Add(previous_list[prev[i]: prev[i+1] - 1])
                    // 3) prev[i+1] - prev[i] = 1 and next[i+1] - next[i] > 1 => temp_list.Add(seen_points[n][next[i]: next[i+1] - 1])
                    // 4) prev[i+1] - prev[i] > 1 and next[i+1] - next[i] > 1 => temp_list.Add(seen_points[n][next[i]: next[i+1] - 1], previous_list[prev[i]: prev[i+1] - 1])
                    
                    // 1):
                    if (coincidence_positions_prev[j + 1] - coincidence_positions_prev[j] == 1 &&
                        coincidence_positions_next[j + 1] - coincidence_positions_next[j] == 1)
                    {
                        temp_list.Add(previous_list[coincidence_positions_prev[j]]);
                    }
                    // 2):
                    if (coincidence_positions_prev[j + 1] - coincidence_positions_prev[j] > 1 &&
                        coincidence_positions_next[j + 1] - coincidence_positions_next[j] == 1)
                    {
                        for (int jj = coincidence_positions_prev[j]; jj < coincidence_positions_prev[j+1]; jj++)
                        {
                            temp_list.Add(previous_list[jj]);
                        }
                    }
                    // 3):
                    if (coincidence_positions_prev[j + 1] - coincidence_positions_prev[j] == 1 &&
                        coincidence_positions_next[j + 1] - coincidence_positions_next[j] > 1)
                    {
                        for (int jj = coincidence_positions_next[j]; jj < coincidence_positions_next[j + 1]; jj++)
                        {
                            temp_list.Add(seen_points[n][jj]);
                        }
                    }
                    // 4):
                    if (coincidence_positions_prev[j + 1] - coincidence_positions_prev[j] > 1 &&
                        coincidence_positions_next[j + 1] - coincidence_positions_next[j] > 1)
                    {
                        for (int jj = coincidence_positions_next[j]; jj < coincidence_positions_next[j + 1]; jj++)
                        {
                            temp_list.Add(seen_points[n][jj]);
                        }
                        for (int jjj = coincidence_positions_prev[j]; jjj < coincidence_positions_prev[j + 1]; jjj++)
                        {
                            temp_list.Add(previous_list[jjj]);
                        }
                    }
                }
                // add everything from next_list after the last entrance
                for (int nnn = coincidence_positions_next[entrance_num - 1]; nnn < seen_points[n].Count; nnn++)
                {
                    temp_list.Add(seen_points[n][nnn]);
                }

                previous_list = temp_list;
            }



            
            List<Vector3> close_loop_repeats = new List<Vector3>();
            close_loop_repeats.Add(previous_list[0].Coordinates);
            Obj_List.Add("Obs point #" + 0 + ", " + previous_list[0].Coordinates.x + ", " + previous_list[0].Coordinates.y + ", " + previous_list[0].Coordinates.z);
            for (int n = 1; n < previous_list.Count; n++)
            {
                if (previous_list[0].Coordinates != previous_list[n].Coordinates)
                {
                    close_loop_repeats.Add(previous_list[n].Coordinates);
                    //Debug.DrawLine(previous_list[n - 1].Coordinates, previous_list[n].Coordinates, Color.red, 5.0f);
                    Obj_List.Add("Obs point #" + n + ", " + previous_list[n].Coordinates.x + ", " + previous_list[n].Coordinates.y + ", " + previous_list[n].Coordinates.z);
                }
                else 
                {
                    //Debug.DrawLine(previous_list[n - 1].Coordinates, previous_list[n].Coordinates, Color.green, 5.0f);
                    n = previous_list.Count;
                }
                
            }
            //writePath = Application.dataPath + "/close_loop_points_norep" + ".csv";
            //WriteFile(Obj_List, writePath);
            */

        }
    }

















    // define a write function
    void WriteFile(List<string> ObjList, string filePath)
    {
        StreamWriter sWriter;
        if (!File.Exists(filePath))
        {
            sWriter = File.CreateText(filePath);
            //Debug.Log("Creating file");
        }
        else
        {
            sWriter = new StreamWriter(filePath);
            //Debug.Log("Not creating file");
        }

        for (int k = 0; k < ObjList.Count; k++)
        {
            sWriter.WriteLine(ObjList[k]);
        }
        sWriter.Close();
    }

    List<Vector3> Remove_repetiting_points(List<Vector3> points)
    {
        List<Vector3> cleaned_array = points;
        float epsilon = 0.01f; // threshold withing which points should be merged

        for (int k = 0; k < points.Count - 1; k++)
        {
            for (int l = k + 1; l < points.Count; l++)
            {
                if ( (points[k] - points[l]).magnitude < epsilon)
                {
                    cleaned_array.RemoveAt(l);
                    l -= 1;
                }
            }
        }
        return cleaned_array;
    }


    void Mean_STD(List<float> float_list, out float out_mean, out float out_std)
    {
        // Mean of the set
        out_mean = float_list.Average();

        float temp_std = 0;
        for (int k = 0; k < float_list.Count; k++)
        {
            temp_std += (float_list[k] - out_mean) * (float_list[k] - out_mean);
        }

        // Standard deviation according to sample std calculation
        out_std = Mathf.Sqrt(temp_std/(float_list.Count - 1));

    }

    void Search_for_weighted_CG(List<Vector3> points, Vector3 cg, float threshold, out Vector3 update_cg)
    {
        List<V4> points_and_distances = new List<V4>();
        List<float> dist_set = new List<float>();

        for (int k = 0; k < points.Count; k++)
        {
            // calculate distances from the CG to the vertices
            float temp_dist = (points[k] - cg).magnitude;
            // put the values to a list structure
            dist_set.Add(temp_dist);
            // put vertices and distances to a V4 structure
            points_and_distances.Add(new V4(points[k], temp_dist));
        }

        // calculate mean and std of dist_set
        Mean_STD(dist_set, out float dist_mean, out float dist_std);

        // remove outlying vertices from the consideration
        Vector3 temp_cg = new Vector3(0.0f, 0.0f, 0.0f);
        int temp_count = 0;
        // threshold defines how many sigmas we take into account (usually, 3 sigma covers 99.7% of the samples)
        for (int l = 0; l < points.Count; l++)
        {
            if (points_and_distances[l].Distance < dist_mean + threshold * dist_std)
            {
                temp_count += 1;
                temp_cg = temp_cg + points_and_distances[l].Coordinates;
            }
        }
        update_cg = temp_cg / temp_count;
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

    public class V4
    {
        public Vector3 Coordinates;
        public float Distance;
        
        public V4(Vector3 vrtx, float dist)
        {
            Coordinates = vrtx;
            Distance = dist;
        }
    }
}


