using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;


public class TankController : MonoBehaviour
{
    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
    }

    void EnvironmentReset()
    {
        Debug.Log("resetting!!");
    }
}
