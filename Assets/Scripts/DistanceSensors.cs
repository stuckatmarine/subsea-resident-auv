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

    public Material lineMaterial;
    public bool drawLines = true;
    private LineDrawer ld;

    // Start is called before the first frame update
    void Start()
    {
        ld = new LineDrawer(lineMaterial);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int i = 0; i < 4; i++)
        {
            useSensor(i, latMin, latMax);
        }
        for (int i = 4; i < 6; i++)
        {
            useSensor(i, vertMin, vertMax);
        }
    }

    private void useSensor(int i, float min, float max)
    {
        Transform t = sensorArr[i];
        RaycastHit hit;
        Vector3 end = t.transform.position + (max * t.transform.forward);

        if (drawLines)
            ld.DrawLine(t.transform.position, end, Color.red);

        Physics.Raycast(t.transform.position, end, out hit);
        //     Debug.Log("Sensor dist: " + hit.distance);

        if (hit.distance < max)
        {
            sensorValueArr[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = hit.distance.ToString("#.00");
        }
        else
            sensorValueArr[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "MAX";
    }
}
