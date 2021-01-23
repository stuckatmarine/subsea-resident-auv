using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class Pilot : Agent
{
    public Transform goal;
    public Transform srauv;
    public Transform startPos;

    public bool trigger = false;
    
    private Rigidbody rb;
    private Collider collider;
    private ThrusterController thrustCtrl;
    
    private Camera frontCam;
    private Texture2D frontCamTexture;

    private GameObject thrusterController;
    private float[] forces = new float[]{0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};
    private float[] distancesFloat;

    private EnvironmentParameters resetParams;

    public override void Initialize()
    {
        goal = GameObject.Find("goal").GetComponent<Transform>();
        srauv = GameObject.Find("SRAUV").GetComponent<Transform>();
        startPos = GameObject.Find("startPos").GetComponent<Transform>();

        rb = srauv.GetComponent<Rigidbody>();
        collider = srauv.GetComponent<Collider>();
        thrustCtrl = srauv.GetComponent<ThrusterController>();

        frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();
        distancesFloat = GameObject.Find("SRAUV").GetComponent<DistanceSensors>().distancesFloat;
        
        //resetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor) //consider adding norming
    {
        // dist sensors
        sensor.AddObservation(normalize(distancesFloat[0], 0.0f, 10.0f));
        sensor.AddObservation(normalize(distancesFloat[1], 0.0f, 10.0f));
        sensor.AddObservation(normalize(distancesFloat[2], 0.0f, 10.0f));
        sensor.AddObservation(normalize(distancesFloat[3], 0.0f, 10.0f));
        sensor.AddObservation(normalize(distancesFloat[4], 0.0f, 10.0f));
        sensor.AddObservation(normalize(distancesFloat[5], 0.0f, 10.0f));

        // srauv position
        sensor.AddObservation(normalize(srauv.position.x, 1.0f, 11.0f));
        sensor.AddObservation(normalize(srauv.position.y, 1.0f, 6.0f));
        sensor.AddObservation(normalize(srauv.position.z, -11.0f, 1.0f));
        sensor.AddObservation(srauv.rotation.x);
        sensor.AddObservation(srauv.rotation.y);
        sensor.AddObservation(srauv.rotation.z);
        
        // goal position
        sensor.AddObservation(normalize(goal.position.x, 1.0f, 11.0f));
        sensor.AddObservation(normalize(goal.position.y, 1.0f, 6.0f));
        sensor.AddObservation(normalize(goal.position.z, -11.0f, -1.0f));

        // maybe add current velocity?
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.0005f);

        var i = -1;
        var continuousActions = actionBuffers.ContinuousActions;

        thrustCtrl.applyLatThrust(0, continuousActions[++i]*2);
        thrustCtrl.applyLatThrust(1, continuousActions[++i]*2);
        thrustCtrl.applyLatThrust(2, continuousActions[++i]*2);
        thrustCtrl.applyLatThrust(3, continuousActions[++i]*2);
        thrustCtrl.applyVertThrust(0, continuousActions[++i]*2);
        thrustCtrl.applyVertThrust(1, continuousActions[++i]*2);

        if (distancesFloat[0] <= 0.5f)
            AddReward(-0.05f); // 0.5f - distancesFloat[0] ?
        if (distancesFloat[1] <= 0.5f)
            AddReward(-0.05f);
        if (distancesFloat[2] <= 0.5f)
            AddReward(-0.05f);
        if (distancesFloat[3] <= 0.5f)
            AddReward(-0.05f);
        if (distancesFloat[4] <= 0.5f)
            AddReward(-0.05f);
        if (distancesFloat[5] <= 0.5f)
            AddReward(-0.05f);

        if (Math.Abs(goal.position.x - srauv.position.x) <= 0.25f &&
            Math.Abs(goal.position.y - srauv.position.y) <= 0.25f &&
            Math.Abs(goal.position.z - srauv.position.z) <= 0.25f)
        {
            Debug.Log($"Curr Pos x:{srauv.position.x}, y:{srauv.position.y} z:{srauv.position.z}");
            Debug.Log($"Goal x:{goal.position.x}, y:{goal.position.y} z:{goal.position.z}");
            Debug.Log($"Diffs x:{Math.Abs(goal.position.x - srauv.position.x)}, y:{Math.Abs(goal.position.y - srauv.position.y)} z:{Math.Abs(goal.position.z - srauv.position.z)} Target Reached!");
            SetReward(1f);
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        SetResetParameters();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // ThrusterController should take care of this
    }

    public void SetResetParameters()
    {
        // if academy resetParams.GetWithDefault()
        srauv.position = getRandomLocation();
        goal.position = getRandomLocation(); // maybe check its not to close already

        // reset all current velocties
        rb.isKinematic = true;
        rb.isKinematic = false;

        // reset current rotation
        srauv.rotation = new Quaternion(0f, Random.Range(-10f, 10f)/10, 0f, Random.Range(-10f, 10f)/10);
    }

    private Vector3 getRandomLocation()
    {
        // x: 1 - 11, y: 1 - 6, z: (-1) - (-11)
        float x = 0.0f, y = 0.0f, z = 0.0f;

        do
        {
            x = Random.Range(1f, 11f);
            y = Random.Range(1f, 6f);
            z = Random.Range(-1f, -11f);

            startPos.position = new Vector3(x, y, z);
        } while (trigger);

        return new Vector3(x, y, z);
    }

    private float normalize(float val, float min, float max)
    {
        return (val - min)/(max - min);
    }

    private void OnCollisionEnter(Collision collisionInfo)
    {
        AddReward(-0.5f);
    }

    private void OnTriggerEnter(Collider collision)
    {
        trigger = true;
    }

    private void OnTriggerExit(Collider collision)
    {
        trigger = false;
    }
}
