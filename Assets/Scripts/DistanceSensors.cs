using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceSensors : MonoBehaviour
{
    public Transform[] sensorArr;
    public Transform[] sensorValueArr;
    public float latMax;
    public float latMin;
    public float vertMax;
    public float vertMin;
    public Color colorValid;
    public Color colorHigh;
    public Color colorLow;

    // Start is called before the first frame update
    // void Start()
    // {
        
    // }

    // Update is called once per frame
    void FixedUpdate()
    {
        // foreach (Transform t in sensorArr)
        // {
        //     useSensor(t, 0.0f, 2.0f);
        // }
    }

    private void useSensor(Transform t, float min = 0.0f, float max = 2.0f)
    {
        // rb.AddForceAtPosition(spd * Time.deltaTime * t.transform.up, t.transform.position);
        Debug.DrawRay(t.transform.position, max * t.transform.up, Color.red);
    }
}
