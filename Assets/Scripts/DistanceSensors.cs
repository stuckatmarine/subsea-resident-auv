using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DistanceSensors : MonoBehaviour
{
    public Transform[] sensorArr;
    public Transform[] sensorValueArr;
    public float[] distancesFloat;
    public float latMax;
    public float latMin;
    public float vertMax;
    public float vertMin;
    public Color colorValid;
    public Color colorHigh;
    public Color colorLow;

    public bool drawLines = true;

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

        if (drawLines)
            ;

        Physics.Raycast(t.transform.position, t.transform.position + (max * t.transform.forward), out hit);

        distancesFloat[i] = hit.distance;

        if (hit.distance < max)
            sensorValueArr[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = hit.distance.ToString("#.00");
        else
            sensorValueArr[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "MAX";

        // min distance for close proximity checks
        if (hit.distance < min)
        {
            // Debug.Log( i + "   " + hit.distance);
            sensorValueArr[i].GetComponent<Image>().color = Color.red;
        }
        else
            sensorValueArr[i].GetComponent<Image>().color = Color.grey;
    }
}
