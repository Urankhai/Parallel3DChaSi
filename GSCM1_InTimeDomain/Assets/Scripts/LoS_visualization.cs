using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoS_visualization : MonoBehaviour
{
    private LineRenderer lr1, lr2;
    public Transform[] ant_rx;
    public Transform ant_tx;

    void Start()
    {
        lr1 = GetComponent<LineRenderer>();
        lr2 = GetComponent<LineRenderer>();
    }

}
