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
    public bool lean_training = true;
    public Vector3 goal = new Vector3(0.0f, 0.0f, 0.0f);
    public Transform tank;
    public Bounds tankBounds;

    public Transform srauv;
    public Transform startPos;
    public Transform goalBox;
    public Rigidbody massUpper;
    public Rigidbody massLower;
    public Transform indGreen;
    public Transform indRed;

    public int trigger = 0;
    
    private Rigidbody rb;
    private Collider collider;
    private ThrusterController thrustCtrl;
    
    private Camera frontCam;
    private Texture2D frontCamTexture;

    private GameObject thrusterController;
    public float[] forces = new float[]{0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};
    public float[] distancesFloat;

    public float LongitudinalSpd = 5.0f;
    public float LaterialSpd = 5.0f;
    public float VerticalSpd = 5.0f;
    public float YawSpd = 5.0f;

    private Vector3 TankMins = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 TankMaxs = new Vector3(12.0f, 6.0f, 12.0f);

    private EnvironmentParameters resetParams;
    private StatsRecorder statsRecorder;
    private int successes = 1;

    public override void Initialize()
    {
        tank = gameObject.transform.parent.gameObject.transform;
        tankBounds = tank.GetComponent<Collider>().bounds;
        tank.GetComponent<Collider>().enabled = false;
        
        srauv = gameObject.transform;
        startPos = gameObject.transform.parent.gameObject.transform.Find("startPos").gameObject.transform;
        goalBox = gameObject.transform.parent.gameObject.transform.Find("goalBox").gameObject.transform;
        
        rb = srauv.GetComponent<Rigidbody>();
        massUpper = gameObject.transform.Find("massUpper").gameObject.transform.GetComponent<Rigidbody>();
        massLower = gameObject.transform.Find("massLower").gameObject.transform.GetComponent<Rigidbody>();
        indGreen = gameObject.transform.parent.gameObject.transform.Find("indicatorGreen").gameObject.transform;
        indRed = gameObject.transform.parent.gameObject.transform.Find("indicatorRed").gameObject.transform;

        collider = srauv.GetComponent<Collider>();
        thrustCtrl = srauv.GetComponent<ThrusterController>();

        //frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();
        distancesFloat = gameObject.GetComponent<DistanceSensors>().distancesFloat;
        
        statsRecorder = Academy.Instance.StatsRecorder;
        //resetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // dist sensors
        foreach (float dist in distancesFloat) {
            sensor.AddObservation(Normalize(dist, 0.0f, 12.0f));
        }

        // srauv info
        sensor.AddObservation(Normalize(srauv.position - tank.position, TankMins, TankMaxs));
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(Normalize(srauv.rotation.y, 0.0f, 360.0f));
        sensor.AddObservation(rb.angularVelocity.y); //change this to just the one

        // goal position
        sensor.AddObservation(Normalize(goal - tank.position, TankMins, TankMaxs));
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.0005f);

        if (Math.Abs(goal.x - srauv.position.x) <= 1.0f &&
            Math.Abs(goal.y - srauv.position.y) <= 1.0f &&
            Math.Abs(goal.z - srauv.position.z) <= 1.0f)
        {
            statsRecorder.Add("Targets Reached", successes++);
            TargetReachedSwapGroundMaterial(indGreen, 0.5f);
            AddReward(1.0f);
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
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();

        if (Input.GetKeyDown(KeyCode.Escape))
            EndEpisode();

        discreteActionsOut[0] = 0;
        if (Input.GetKey(KeyCode.W))
            discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.S))
            discreteActionsOut[0] = 2;

        discreteActionsOut[1] = 0;
        if (Input.GetKey(KeyCode.D))
            discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.A))
            discreteActionsOut[1] = 2;

        discreteActionsOut[2] = 0;
        if (Input.GetKey(KeyCode.UpArrow))
            discreteActionsOut[2] = 1;
        else if (Input.GetKey(KeyCode.DownArrow))
            discreteActionsOut[2] = 2;

        discreteActionsOut[3] = 0;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow))
            discreteActionsOut[3] = 1;
        else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow))
            discreteActionsOut[3] = 2;
    }

    public void SetResetParameters()
    {
        // if academy resetParams.GetWithDefault()
        srauv.position = GetRandomLocation();
        goal = GetRandomLocation(); // maybe check its not to close already
        goalBox.position = goal;
        startPos.position = new Vector3(tank.position.x, 12.0f, tank.position.z);
        
        // reset current rotation
        srauv.rotation = new Quaternion(0f, Random.Range(-10f, 10f)/10, 0f, Random.Range(-10f, 10f)/10);

        // rb.isKinematic = true;
        // rb.isKinematic = false;
        
        // reset all current velocties
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        massUpper.velocity = Vector3.zero;
        massUpper.angularVelocity = Vector3.zero;
        massLower.velocity = Vector3.zero;
        massLower.angularVelocity = Vector3.zero;
    }

    IEnumerator TargetReachedSwapGroundMaterial(Transform ind, float time)
    {
        Debug.Log("test");
        ind.position = new Vector3(tank.position.x + 6.0f, 0.0f, tank.position.z + 6.0f);
        yield return new WaitForSeconds(time); // Wait for 2 sec
        ind.position = new Vector3(tank.position.x + 6.0f, -1.0f, tank.position.z + 6.0f);
    }

    private Vector3 GetRandomLocation()
    {
        // x: 1 - 11, y: 1 - 6, z: (-1) - (-11)
        float x = 0.0f, y = 0.0f, z = 0.0f;

        do
        {
            x = Random.Range(-tankBounds.extents.x * 0.75f, tankBounds.extents.x * 0.75f) + tank.position.x + 6.0f;
            y = Random.Range(TankMins.y, TankMaxs.y);
            z = Random.Range(-tankBounds.extents.z * 0.75f, tankBounds.extents.z * 0.75f) + tank.position.z + 6.0f;

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
        TargetReachedSwapGroundMaterial(indRed, 0.5f);
        SetReward(-1.0f);
        EndEpisode();
    }

    private void OnTriggerEnter(Collider c)
    {
        trigger += 1;
    }

    private void OnTriggerExit(Collider c)
    {
        trigger -= 1;
    }
}
