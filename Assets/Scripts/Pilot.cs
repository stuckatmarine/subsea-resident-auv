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
	public Transform srauv;
	public Transform startPos;
	public Vector3 goal = new Vector3(0.0f, 0.0f, 0.0f);

    public bool trigger = false;
    public bool colliding = false;
    
    private Rigidbody rb;
    private Collider collider;
    private ThrusterController thrustCtrl;
    
    private Camera frontCam;
    private Texture2D frontCamTexture;

	private GameObject thrusterController;
    private float[] forces = new float[]{0.0f, 0.0f, 0.0f,0.0f,0.0f,0.0f};
	private float[] distancesFloat;

	private EnvironmentParameters resetParams;

    public override void Initialize()
    {
        srauv = GameObject.Find("SRAUV").GetComponent<Transform>();
        startPos = GameObject.Find("startPos").GetComponent<Transform>();
        
        rb = srauv.GetComponent<Rigidbody>();
        collider = srauv.GetComponent<Collider>();
        thrustCtrl = srauv.GetComponent<ThrusterController>();

        frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();
        distancesFloat = GameObject.Find("SRAUV").GetComponent<DistanceSensors>().distancesFloat;
    	
    	resetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
    	// dist sensors
        sensor.AddObservation(distancesFloat[0]); 
        sensor.AddObservation(distancesFloat[1]);
        sensor.AddObservation(distancesFloat[2]);
        sensor.AddObservation(distancesFloat[3]);
        sensor.AddObservation(distancesFloat[4]);
        sensor.AddObservation(distancesFloat[5]);

        // srauv position
       	sensor.AddObservation(srauv.position.x); 
        sensor.AddObservation(srauv.position.y);
        sensor.AddObservation(srauv.position.z);
        sensor.AddObservation(srauv.rotation.y * 360.0f);
        sensor.AddObservation(srauv.rotation.x * 360.0f);
        sensor.AddObservation(srauv.rotation.z * 360.0f);

        // dist from goal
        sensor.AddObservation(goal[0] - srauv.position.x); 
        sensor.AddObservation(goal[1] - srauv.position.y);
        sensor.AddObservation(goal[2] - srauv.position.z);

        // maybe add current velocity?
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
    	var i = -1;
    	var continuousActions = actionBuffers.ContinuousActions;

    	thrustCtrl.applyLatThrust(0, continuousActions[++i]);
        thrustCtrl.applyLatThrust(1, continuousActions[++i]);
        thrustCtrl.applyLatThrust(2, continuousActions[++i]);
        thrustCtrl.applyLatThrust(3, continuousActions[++i]);
        thrustCtrl.applyVertThrust(0, continuousActions[++i]);
        thrustCtrl.applyVertThrust(1, continuousActions[++i]);

        if (colliding) 
        {
        	AddReward(-0.5f);
        	//EndEpisode(); // or not?
        }

        if (distancesFloat[0] <= 0.5f)
            AddReward(-0.1f); // 0.5f - distancesFloat[0] ?
        if (distancesFloat[1] <= 0.5f)
            AddReward(-0.1f);
        if (distancesFloat[2] <= 0.5f)
            AddReward(-0.1f);
        if (distancesFloat[3] <= 0.5f)
            AddReward(-0.1f);
        if (distancesFloat[4] <= 0.5f)
            AddReward(-0.1f);
        if (distancesFloat[5] <= 0.5f)
            AddReward(-0.1f);

        if (goal.x - srauv.position.x <= 0.3 &&
        	goal.y - srauv.position.y <= 0.3 &&
        	goal.z - srauv.position.z <= 0.3)
        {
        	SetReward(1f);
        	EndEpisode();
        }
        AddReward(-0.05f);
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
    	goal = getRandomLocation();
    	
    	// reset all current velocties
		rb.isKinematic = false;
		rb.isKinematic = true;

    	// reset current rotation
		srauv.rotation = new Quaternion(0f, 0f, 0f, 0f);
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

    void OnCollisionExit(Collision collisionInfo)
    {
    	// TODO: deal with multiple collisions
    	colliding = false;
    }

    void OnCollisionEnter(Collision collisionInfo)
    {
    	Debug.Log("Collision Enter!!");
    	colliding = true;
    }

    void OnTriggerEnter(Collider collision)
    {
    	Debug.Log("Trigger Enter!!");;
    	trigger = true;
    }

  	void OnTriggerExit(Collider collision)
  	{
  		trigger = false;
  	}
}
