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
	public float[] goal = new float[]{0.0f, 0.0f, 0.0f}; // this the correct data type?
    private Transform srauv;
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

        if (goal[0] - srauv.position.x <= 0.1 &&
        	goal[1] - srauv.position.y <= 0.1 &&
        	goal[2] - srauv.position.z <= 0.1)
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
    	// need more reasonable reset default positions
    	// how do I set this?
    	//srauv.position.x = resetParams.GetWithDefault("SRAUVx", 1.0f);
    	//srauv.position.y = resetParams.GetWithDefault("SRAUVy", 1.0f);
    	//srauv.position.z = resetParams.GetWithDefault("SRAUVz", 1.0f);

    	// need more reasonable reset default positions
    	goal[0] = resetParams.GetWithDefault("goalx", 1.0f);
    	goal[1] = resetParams.GetWithDefault("goaly", 1.0f);
    	goal[2] = resetParams.GetWithDefault("goalz", 1.0f);
    	
    	// reset all current velocties
    	// help

    	// reset current rotation
    }
}
