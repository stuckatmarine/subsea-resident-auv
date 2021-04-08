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

    public float goalHeading = 0f;
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

    private float LongitudinalSpd = 200.0f;
    private float LaterialSpd = 200.0f;
    private float VerticalSpd = 400.0f;
    private float YawSpd = 1000.0f;
    private float maxVel = 0.3f;

    private Vector3 TankMins = new Vector3(0.8f, 1.5f, 0.8f);
    private Vector3 TankMaxs = new Vector3(2.3f, 3.4f, 2.3f);

    private EnvironmentParameters resetParams;
    private StatsRecorder statsRecorder;
    private int score = 0;
    [SerializeField] private int goalsReached = 0;
    [SerializeField] private int numWaypoints = 1;
    [SerializeField] private float goalSize = 0.45f;
    [SerializeField] private float headingTolerance = 180f;

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
            Application.targetFrameRate = 30;
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

        resetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        int tagInView = 0;
        for (int i=0; i < 9; i++)
        {
            // note this api uses y variable as the equalivant of our z
            Vector3 viewPos = downCam.WorldToViewportPoint(tags[i].position);

            // simulating noise of random inaccuracies in AprilTag detector
            float rand = Random.Range(0, 10);

            if (0.1f < viewPos.x && viewPos.x < 0.9f &&
                0.1f < viewPos.y && viewPos.y < 0.9f &&
                0.95f < viewPos.z && rand < 9)
            {
                // simulating noise of AprilTag localization
                Vector3 noise = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));

                // we see an AprilTag so last known pos is right now
                lastKnownPos = srauv.position - tank.position; // + noise;
                lastKnownHeading = srauv.eulerAngles.y;
                lastVel = rb.velocity;
                tagInView = 1;
                break;
            }
        }

        // position & heading from AprilTag
        sensor.AddObservation(tagInView);
        sensor.AddObservation(srauv.position - tank.position);
        sensor.AddObservation(srauv.eulerAngles.y);

        // goal position
        sensor.AddObservation(goal - tank.position);

        // yolo velocity calcs
        sensor.AddObservation(lastVel);

        // mimic IMU instantaneous accelerations
        //sensor.AddObservation((rb.velocity - lastVel)/Time.deltaTime);
        //lastVel = rb.velocity; // or maybe v = d/t ?

        // angular velocity from IMU gyro
        sensor.AddObservation(rb.angularVelocity.y);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-1f / MaxStep);

        if (Math.Abs(rb.velocity.z) > maxVel)
            AddReward(-1f / MaxStep);
        if (Math.Abs(rb.velocity.z) > maxVel)
            AddReward(-1f / MaxStep);
        if (Math.Abs(rb.velocity.z) > maxVel)
            AddReward(-1f / MaxStep);
        if (Math.Abs(rb.angularVelocity.y) > maxVel)
            AddReward(-1f / MaxStep);
        if (!isInGoal())
            AddReward(-1f / MaxStep);

        if (isInGoal() && isInHeading())
        {
            goalsReached++;
            statsRecorder.Add("Score", ++score);

            AddReward(1.0f);
            StartCoroutine(TargetReachedSwapGroundMaterial(indGreen, 0.5f));

            if (goalsReached < numWaypoints)
            {
                goal = GetRandomLocation();
                goalBox.position = goal;
            }
            else
            {
                EndEpisode();
            }
        }

        MoveAgent(actionBuffers.DiscreteActions);
    }

    private bool isInGoal()
    {
        if (Math.Abs(goal.x - srauv.position.x) <= goalSize &&
            Math.Abs(goal.y - srauv.position.y) <= goalSize &&
            Math.Abs(goal.z - srauv.position.z) <= goalSize)
        {
            return true;
        }

        return false;
    }

    private bool isInHeading()
    {
        // this is a bit hacky, there is probably a better vector math solution for this
        float min = srauv.eulerAngles.y - (headingTolerance / 2);
        float max = srauv.eulerAngles.y + (headingTolerance / 2);

        if (max > 360)
        {
            max = max % 360;
            if ((goalHeading < max && goalHeading > 0) ||
                (goalHeading > min && goalHeading < 360))
            {
                return true;
            }
        }
        else if (min < 0)
        {
            min = 360 + min;
            if ((goalHeading > min && goalHeading < 360) ||
                (goalHeading < max && goalHeading > 0))
            {
                return true;
            }
        }
        else if (goalHeading < max && goalHeading > min)
        {
            return true;
        }

        return false;
    }

    private void MoveAgent(ActionSegment<int> act)
    {
        for (int i=1; i < 5; i++)
        {
            var thrustSpd = act[i-1];

            if (i < 4)
            {
                var thrusters = thrustCtrl.latThrusters;

                if (thrustSpd == 0)
                    thrustCtrl.applyThrust(thrusters[i], -LongitudinalSpd);
                else if (thrustSpd == 1)
                    thrustCtrl.applyThrust(thrusters[i], -LongitudinalSpd / 2);
                else if (thrustSpd == 2)
                    thrustCtrl.applyThrust(thrusters[i], -LongitudinalSpd / 4);
                else if (thrustSpd == 4)
                    thrustCtrl.applyThrust(thrusters[i],  LongitudinalSpd / 4);
                else if (thrustSpd == 5)
                    thrustCtrl.applyThrust(thrusters[i],  LongitudinalSpd / 2);
                else if (thrustSpd == 6)
                    thrustCtrl.applyThrust(thrusters[i],  LongitudinalSpd);
            }
            else
            {
                float spd = 0;

                if (thrustSpd == 0)
                    spd = -VerticalSpd;
                else if (thrustSpd == 1)
                    spd = -VerticalSpd / 2;
                else if (thrustSpd == 2)
                    spd = -VerticalSpd / 4;
                else if (thrustSpd == 4)
                    spd =  VerticalSpd / 4;
                else if (thrustSpd == 5)
                    spd =  VerticalSpd / 2;
                else if (thrustSpd == 6)
                    spd =  VerticalSpd;

                foreach(Transform t in thrustCtrl.vertThrusters)
                {
                    thrustCtrl.applyThrust(t, spd);
                }
            }
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

        if (Input.GetKey(KeyCode.W))
            thrustCtrl.moveForward(LongitudinalSpd);
        else if (Input.GetKey(KeyCode.S))
            thrustCtrl.moveReverse(LongitudinalSpd);

        if (Input.GetKey(KeyCode.D))
            thrustCtrl.strafeRight(LaterialSpd);
        else if (Input.GetKey(KeyCode.A))
            thrustCtrl.strafeLeft(LaterialSpd);

        if (Input.GetKey(KeyCode.UpArrow))
            thrustCtrl.vertUp(VerticalSpd);
        else if (Input.GetKey(KeyCode.DownArrow))
            thrustCtrl.vertDown(VerticalSpd);

        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow))
            thrustCtrl.turnRight(YawSpd);
        else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow))
            thrustCtrl.turnLeft(YawSpd);
    }

    public void SetResetParameters()
    {
        if (!enablePilot)
            return;
            
        // if academy resetParams.GetWithDefault()
        srauv.position = GetRandomLocation();
        goal = GetRandomLocation();
        goalBox.position = goal;
        startPos.position = new Vector3(tank.position.x, 3.6576f, tank.position.z);
        goalHeading = Random.Range(0, 360);
        lastKnownPos = goal;
        
        // params from curriculum lesson
        numWaypoints = (int) resetParams.GetWithDefault("num_waypoints", 8);
        goalSize = resetParams.GetWithDefault("goal_size", 0.05f);
        headingTolerance = resetParams.GetWithDefault("heading_tolerance", 15f);
        
        // reset scoring metrics
        goalsReached = 0;
        score = 0;

        // reset current rotation
        srauv.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        
        // reset all current velocties
        lastVel = Vector3.zero;
        lastKnownPos = Vector3.zero;
        lastKnownHeading = 0.0f;
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
        float x = 0.0f, y = 0.0f, z = 0.0f;
        do
        {
            x = Random.Range(TankMins.x, TankMaxs.x) + tank.position.x;
            y = Random.Range(TankMins.y, TankMaxs.y);
            z = Random.Range(TankMins.z, TankMaxs.z) + tank.position.z;

            startPos.position = new Vector3(x, y, z);
        } while (trigger > 0);

        startPos.position = new Vector3(0f, 6f, 0f);
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

            statsRecorder.Add("Goals Reached Before Fail", goalsReached);
            statsRecorder.Add("Score", --score);

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
