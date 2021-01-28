﻿using System.Collections;
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

    public bool lean_training = false;

    void Start()
    {
        lean_training = gameObject.GetComponent<Pilot>().lean_training;
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

        Physics.Raycast(t.transform.position, t.transform.position + (max * t.transform.forward), out hit);

        distancesFloat[i] = hit.distance;

        if (lean_training)
            return;

        if (hit.distance < max && hit.distance > 0.0f)
            sensorValueArr[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = hit.distance.ToString("#.00");
        else
            sensorValueArr[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "MAX";

        // min distance for close proximity checks
        if (hit.distance < min && hit.distance > 0.0f)
        {
            // Debug.Log( i + "   " + hit.distance);
            sensorValueArr[i].GetComponent<Image>().color = Color.red;
        }
        else
            sensorValueArr[i].GetComponent<Image>().color = Color.grey;
    }
}
