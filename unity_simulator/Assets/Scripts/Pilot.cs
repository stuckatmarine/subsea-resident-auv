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
    public bool enablePilot = true;
    public bool lean_training = true;

    public Vector3 goal = new Vector3(0.0f, 0.0f, 0.0f);
    public Transform tank;
    public Bounds tankBounds;

    private Vector3 lastKnownPos = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 lastVel = new Vector3(0.0f, 0.0f, 0.0f);
    private float lastKnownHeading = 0.0f;

    public Transform srauv;
    public Transform startPos;
    public Transform goalBox;
    private Rigidbody massUpper;
    private Rigidbody massLower;
    private Transform indGreen;
    private Transform indRed;

    public int trigger = 0;
    
    private Rigidbody rb;
    private Collider collider;
    private ThrusterController thrustCtrl;
    
    private Camera downCam;
    private Texture2D frontCamTexture;

    private GameObject thrusterController;
    public float[] forces = new float[]{0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};
    public float[] distancesFloat;

    public float LongitudinalSpd = 5.0f;
    public float LaterialSpd = 5.0f;
    public float VerticalSpd = 5.0f;
    public float YawSpd = 2.5f;

    private Vector3 TankMins = new Vector3(0.0f, 2.0f, 0.0f);
    private Vector3 TankMaxs = new Vector3(3.6576f, 3.4f, 3.6576f);

    private EnvironmentParameters resetParams;
    private StatsRecorder statsRecorder;
    private int successes = 1;

    public List<Transform> tags;

    public override void Initialize()
    {
        
        tank = gameObject.transform.parent.gameObject.transform;
        tankBounds = tank.GetComponent<Collider>().bounds;
        tank.GetComponent<Collider>().enabled = false;
        
        srauv = gameObject.transform;
        startPos = gameObject.transform.parent.gameObject.transform.Find("startPos").gameObject.transform;
        if (enablePilot)
        {
            goalBox = gameObject.transform.parent.gameObject.transform.Find("goalBox").gameObject.transform;
            indGreen = gameObject.transform.parent.gameObject.transform.Find("indicatorGreen").gameObject.transform;
            indRed = gameObject.transform.parent.gameObject.transform.Find("indicatorRed").gameObject.transform;
            downCam = gameObject.transform.Find("DownCam").gameObject.transform.Find("CameraDown").GetComponent<Camera>();
            statsRecorder = Academy.Instance.StatsRecorder;
            
            for (int i=0; i < 9; i++)
            {
                tags.Add(gameObject.transform.parent.gameObject.transform.Find(string.Format("aprilTag ({0})", i)).gameObject.transform);
            }
        }

        rb = srauv.GetComponent<Rigidbody>();
        massUpper = gameObject.transform.Find("massUpper").gameObject.transform.GetComponent<Rigidbody>();
        massLower = gameObject.transform.Find("massLower").gameObject.transform.GetComponent<Rigidbody>();

        collider = srauv.GetComponent<Collider>();
        thrustCtrl = srauv.GetComponent<ThrusterController>();

        //frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();
        //distancesFloat = gameObject.GetComponent<DistanceSensors>().distancesFloat;

        //resetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        bool tagInView = false;
        for (int i=0; i < 9; i++)
        {
            // note this api uses y variable as the equalivant of our z
            Vector3 viewPos = downCam.WorldToViewportPoint(tags[i].position);

            // simulating noise of random inaccuracies in AprilTag detector
            float rand = Random.Range(0, 10);

            if (0.15f < viewPos.x && viewPos.x < 0.85f &&
                0.15f < viewPos.y && viewPos.y < 0.85f &&
                0.95f < viewPos.z && rand < 9)
            {
                tagInView = true;
            }
        }

        if (tagInView)
        {
            // we see an AprilTag so last known pos is right now
            lastKnownPos = srauv.position - tank.position;
            lastKnownHeading = srauv.rotation.y % 360;
        }

        // position & heading from AprilTag
        sensor.AddObservation(lastKnownPos);
        sensor.AddObservation(lastKnownHeading);

        // goal position
        sensor.AddObservation(goal - tank.position);

        // mimic IMU instantaneous accelerations
        sensor.AddObservation((rb.velocity - lastVel)/Time.deltaTime);
        lastVel = rb.velocity;

        // angular velocity from IMU gyro
        sensor.AddObservation(rb.angularVelocity.y);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-1f / MaxStep);

        if (Math.Abs(goal.x - srauv.position.x) <= 0.3f &&
            Math.Abs(goal.y - srauv.position.y) <= 0.3f &&
            Math.Abs(goal.z - srauv.position.z) <= 0.3f)
        {
            statsRecorder.Add("Targets Reached", successes++);
            AddReward(2.0f);
            StartCoroutine(TargetReachedSwapGroundMaterial(indGreen, 0.5f));
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

        if (yaw == 0)
        {
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
        if (!enablePilot)
            return;
            
        // if academy resetParams.GetWithDefault()
        srauv.position = GetRandomLocation();
        goal = GetRandomLocation(); // maybe check its not to close already
        goalBox.position = goal;
        startPos.position = new Vector3(tank.position.x, 3.6576f, tank.position.z);
        lastKnownPos = goal;
        
        // reset current rotation
        srauv.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        
        // reset all current velocties
        lastVel = Vector3.zero;
        lastKnownPos = Vector3.zero;
        lastKnownHeading = 0;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        massUpper.velocity = Vector3.zero;
        massUpper.angularVelocity = Vector3.zero;
        massLower.velocity = Vector3.zero;
        massLower.angularVelocity = Vector3.zero;
    }

    IEnumerator TargetReachedSwapGroundMaterial(Transform ind, float time)
    {
        ind.position = new Vector3(tank.position.x + 1.8288f, 0.0f, tank.position.z + 1.8288f);
        yield return new WaitForSeconds(time); // Wait for 2 sec
        ind.position = new Vector3(tank.position.x + 1.8288f, -0.3048f, tank.position.z + 1.8288f);
    }

    private Vector3 GetRandomLocation()
    {
        // x: 1 - 11, y: 1 - 6, z: (-1) - (-11)
        float x = 0.0f, y = 0.0f, z = 0.0f;

        do
        {
            x = Random.Range(-tankBounds.extents.x * 0.75f, tankBounds.extents.x * 0.75f) + tank.position.x + 1.8288f;
            y = Random.Range(TankMins.y, TankMaxs.y);
            z = Random.Range(-tankBounds.extents.z * 0.75f, tankBounds.extents.z * 0.75f) + tank.position.z + 1.8288f;

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
        if (enablePilot)
        {
          SetReward(-1.0f);
          StartCoroutine(TargetReachedSwapGroundMaterial(indRed, 0.5f));
          EndEpisode();
        }
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
