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
    public Vector3 goal = new Vector3(0.0f, 0.0f, 0.0f);
    public Transform srauv;
    public Transform startPos;

    public int trigger = 0;
    
    private Rigidbody rb;
    public Collider collider;
    private ThrusterController thrustCtrl;
    
    private Camera frontCam;
    private Texture2D frontCamTexture;

    public float[] forces = new float[]{0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};
    public float[] distancesFloat;

    private float LongitudinalSpd = 3.0f;
    private float LaterialSpd = 3.0f;
    private float VerticalSpd = 3.0f;
    private float YawSpd = 3.0f;

    private Vector3 TankMins = new Vector3(1.0f, 1.0f, -11.0f);
    private Vector3 TankMaxs = new Vector3(11.0f, 6.0f, -1.0f);

    public EnvironmentParameters resetParams;

    public override void Initialize()
    {
        //goal = GameObject.Find("goal").GetComponent<Transform>();
        srauv = GameObject.Find("SRAUV").GetComponent<Transform>();
        startPos = GameObject.Find("startPos").GetComponent<Transform>();

        rb = srauv.GetComponent<Rigidbody>();
        collider = srauv.GetComponent<Collider>();
        thrustCtrl = srauv.GetComponent<ThrusterController>();

        //frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();
        distancesFloat = GameObject.Find("SRAUV").GetComponent<DistanceSensors>().distancesFloat;
        
        //resetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // dist sensors
        foreach (float dist in distancesFloat) 
            sensor.AddObservation(Normalize(dist, 0.0f, 10.0f));

        // srauv info
        sensor.AddObservation(Normalize(srauv.position, TankMins, TankMaxs));
        sensor.AddObservation(srauv.rotation);
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(rb.angularVelocity);

        // goal position
        sensor.AddObservation(Normalize(goal, TankMins, TankMaxs));
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.005f);

        foreach (float dist in distancesFloat)
        {
            if (dist < 0.5f)
                AddReward(-(0.5f - dist));
        }

        if (Math.Abs(goal.x - srauv.position.x) <= 0.5f &&
            Math.Abs(goal.y - srauv.position.y) <= 0.5f &&
            Math.Abs(goal.z - srauv.position.z) <= 0.5f)
        {
            SetReward(1.0f);
            EndEpisode();
        }
        
        MoveAgent(actionBuffers.DiscreteActions);
    }

    private void MoveAgent(ActionSegment<int> act)
    {
        var longitudinal = act[0];
        var laterial = act[1];
        var vertical = act[2];
        var yaw = act[3];

        switch (longitudinal)
        {
            case 1:
                thrustCtrl.moveForward(LongitudinalSpd);                
                break;
            case 2:
                thrustCtrl.moveReverse(LongitudinalSpd);
                break;
        }

        switch (laterial)
        {
            case 1:
                thrustCtrl.strafeRight(LaterialSpd);
                break;
            case 2:
                thrustCtrl.strafeLeft(LaterialSpd);
                break;
        }

        switch (vertical)
        {
            case 1:
                thrustCtrl.vertUp(VerticalSpd);
                break;
            case 2:
                thrustCtrl.vertDown(VerticalSpd);
                break;
        }

        switch (yaw)
        {
            case 1:
                thrustCtrl.turnRight(YawSpd);
                break;
            case 2:
                thrustCtrl.turnLeft(YawSpd);
                break;
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
        srauv.position = GetRandomLocation();
        goal = GetRandomLocation(); // maybe check its not to close already
        startPos.position = new Vector3(3.0f, 15.0f, -3.0f); // collision with this = crash

        // reset all current velocties
        rb.isKinematic = true;
        rb.isKinematic = false;

        // reset current rotation
        srauv.rotation = new Quaternion(0.0f, Random.Range(-1.0f, 10.0f)/10.0f, 0f, Random.Range(-1.0f, 10.0f)/10.0f);
    }

    private Vector3 GetRandomLocation()
    {
        // x: 1 - 11, y: 1 - 6, z: (-1) - (-11)
        float x = 0.0f, y = 0.0f, z = 0.0f;

        do
        {
            x = Random.Range(TankMins.x, TankMaxs.x);
            y = Random.Range(TankMins.y, TankMaxs.y);
            z = Random.Range(TankMins.z, TankMaxs.z);

            startPos.position = new Vector3(x, y, z);
        } while (trigger > 0);

        return new Vector3(x, y, z);
    }

    private Vector3 Normalize(Vector3 val, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Normalize(val.x, min.x, max.x),
            Normalize(val.y, min.y, max.y),
            Normalize(val.z, min.z, max.z));
    }

    private float Normalize(float val, float min, float max)
    {
        return (val - min)/(max - min);
    }

    private void OnCollisionEnter(Collision collisionInfo)
    {
        AddReward(-1.0f);
        EndEpisode();
    }

    private void OnTriggerEnter(Collider collision)
    {

        trigger += 1;
    }

    private void OnTriggerExit(Collider collision)
    {
        trigger -= 1;
    }
}
