using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ThrusterController : MonoBehaviour
{
    public Transform[] vertThrusters; // front, rear
    public Transform[] latThrusters; // clockwise from 0deg heading
    public Rigidbody rb;
    public float vertSpd = 5.0f;
    public float latSpd = 5.0f;

    public ServerCommunication WS;
    public Material lineMaterial;
    public bool drawLines = true;
    private LineDrawer ld;

    void Start()
    {
        ld = new LineDrawer(lineMaterial);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
             WS.SendRequest("test msg");


        // verts
        if (Input.GetKey(KeyCode.UpArrow))
        {
            vertUp();
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            vertDown();
        }

        // move laterlly
        if (Input.GetKey(KeyCode.D))
        {
            strafeRight();
        }
        else if (Input.GetKey(KeyCode.A))
        {
            strafeLeft();
        }

        // move forward
        if (Input.GetKey(KeyCode.W))
        {
            moveForward();
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveReverse();
        }

        // turn
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow))
        {
            turnRight();
        }
        else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow))
        {
            turnLeft();
        }

        // RL
        if (Input.GetKey(KeyCode.T))
        {
            applyThrust(latThrusters[2], latSpd);
        }
        else if (Input.GetKey(KeyCode.G))
        {
            applyThrust(latThrusters[2], -latSpd);
        }

        // FL
        if (Input.GetKey(KeyCode.Y))
        {
            applyThrust(latThrusters[3], latSpd);
        }
        else if (Input.GetKey(KeyCode.H))
        {
            applyThrust(latThrusters[3], -latSpd);
        }

        // FR
        if (Input.GetKey(KeyCode.U))
        {
            applyThrust(latThrusters[0], latSpd);
        }
        else if (Input.GetKey(KeyCode.J))
        {
            applyThrust(latThrusters[0], -latSpd);
        }

        // RR
        if (Input.GetKey(KeyCode.I))
        {
            applyThrust(latThrusters[1], latSpd);
        }
        else if (Input.GetKey(KeyCode.K))
        {
            applyThrust(latThrusters[1], -latSpd);
        }
    }

    // apply force at position of thruster transform 't'
    // also draws green ray to show thrust in editor
    // TODO: apply ray to camera views and GUI
    private void applyThrust(Transform t, float spd = 1.0f)
    {
        rb.AddForceAtPosition(spd * Time.deltaTime * t.transform.up, t.transform.position);
        if (drawLines)
            ld.DrawLine(t.transform.position, t.transform.position + (-spd * 0.3f * t.transform.up * Time.deltaTime), Color.green, 0.1f);
        // Debug.DrawRay(t.transform.position, -spd * Time.deltaTime * t.transform.up, Color.green);
    }

    // 4 vectored thruster move forward command
    // +/- latSpd to control output direction
    // thrusters ordered clockwise {frontRight, backRight, backLeft, fronLeft}
    public void moveForward()
    {
        applyThrust(latThrusters[0], latSpd);
        applyThrust(latThrusters[1], latSpd);
        applyThrust(latThrusters[2], latSpd);
        applyThrust(latThrusters[3], latSpd);
    }

    public void moveReverse()
    {
        applyThrust(latThrusters[0], -latSpd);
        applyThrust(latThrusters[1], -latSpd);
        applyThrust(latThrusters[2], -latSpd);
        applyThrust(latThrusters[3], -latSpd);
    }

    public void strafeRight()
    {
        applyThrust(latThrusters[0], -latSpd);
        applyThrust(latThrusters[1], latSpd);
        applyThrust(latThrusters[2], -latSpd);
        applyThrust(latThrusters[3], latSpd);
    }

    public void strafeLeft()
    {
        applyThrust(latThrusters[0], latSpd);
        applyThrust(latThrusters[1], -latSpd);
        applyThrust(latThrusters[2], latSpd);
        applyThrust(latThrusters[3], -latSpd);
    }

    public void turnRight()
    {
        applyThrust(latThrusters[0], -latSpd);
        applyThrust(latThrusters[1], -latSpd);
        applyThrust(latThrusters[2], latSpd);
        applyThrust(latThrusters[3], latSpd);
    }

    public void turnLeft()
    {
        applyThrust(latThrusters[0], latSpd);
        applyThrust(latThrusters[1], latSpd);
        applyThrust(latThrusters[2], -latSpd);
        applyThrust(latThrusters[3], -latSpd);
    }

    public void vertUp()
    {
        foreach(Transform t in vertThrusters){
            applyThrust(t, vertSpd);
        }
    }

    public void vertDown()
    {
        foreach(Transform t in vertThrusters){
            applyThrust(t, -vertSpd);
        }
    }

    
}
