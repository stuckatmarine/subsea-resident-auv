using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class ThrusterController : MonoBehaviour
{
    public Transform[] vertThrusters; // front, rear
    public Transform[] latThrusters; // clockwise from 0deg heading
    public Transform[] UiVertThrusters;
    public Transform[] UiLatThrusters;
    public float[] raw_thrust;
    public float raw_thrust_spd = 25.0f;
    public string[] dir_thrust;
    public Rigidbody rb;
    public float vertSpd = 5.0f;
    public float latSpd = 5.0f;
    
    public bool enableManualCmds = true;        // use thrust inputs in sim as force
    public bool enableAbsoluteCmds = false;     // move around sim without physics

    public ServerCommunication WS;
    public Material lineMaterial;
    public bool drawLinesCmd = false;
    public bool drawLinesManual = true;
    private LineDrawer ld;

    public bool raw_thrust_used = false;
    public bool dir_thrust_used = false;
    public bool lean_training = false;

    void Start()
    {
        ld = new LineDrawer(lineMaterial);
        // enableManual();

        raw_thrust = new float[6]{0.0f,0.0f,0.0f,0.0f,0.0f,0.0f};
        dir_thrust = new string[4]{"", "", "", ""};

        lean_training = gameObject.GetComponent<Pilot>().lean_training;
    }

    // Update is called once per frame
    void Update()
    {
        if (lean_training)
            return;

        for (int i = 0 ; i < 6; i++)
        {
            raw_thrust[i] = 0;
            if (i < 4)
                dir_thrust[i] = "_";
        }

        raw_thrust_used = false;
        dir_thrust_used = false;
        
        // exchange what spacebar does
        if (Input.GetKeyDown(KeyCode.Escape))
            resetScene();

        // verts
        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x,
                                                 transform.position.y + 0.1f,
                                                 transform.position.z);
            else
                vertUp(vertSpd);

            dir_thrust[3] = "up";
            raw_thrust[4] = raw_thrust_spd;
            raw_thrust[5] = raw_thrust_spd;
            
            raw_thrust_used = true;
            dir_thrust_used = true;
            // transform.position = transform.position + new Vector3(horizontalInput * movementSpeed * Time.deltaTime, verticalInput * movementSpeed * Time.deltaTime, 0);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x,
                                                 transform.position.y - 0.1f,
                                                 transform.position.z);
            else
                vertDown(vertSpd);

            dir_thrust[3] = "down";
            raw_thrust[4] = -raw_thrust_spd;
            raw_thrust[5] = -raw_thrust_spd;
            raw_thrust_used = true;
            dir_thrust_used = true;
        }

        // move laterlly
        if (Input.GetKey(KeyCode.D))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x,
                                                 transform.position.y,
                                                 transform.position.z + 0.1f);
            else
                strafeRight(latSpd);

            dir_thrust[1] = "lat_right";
            dir_thrust_used = true;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x,
                                                 transform.position.y,
                                                 transform.position.z - 0.1f);
            else
                strafeLeft(latSpd);

            dir_thrust[1] = "lat_left";
            dir_thrust_used = true;
        }

        // move forward
        if (Input.GetKey(KeyCode.W))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x - 0.1f,
                                                 transform.position.y,
                                                 transform.position.z);
            else
                moveForward(latSpd);

            dir_thrust[0] = "fwd";
            dir_thrust_used = true;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x + 0.1f,
                                                 transform.position.y,
                                                 transform.position.z);
            else
                moveReverse(latSpd);

            dir_thrust[0] = "rev";
            dir_thrust_used = true;
        }

        // turn
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow))
        {
            if (enableAbsoluteCmds)
                transform.Rotate(transform.rotation.x, transform.rotation.y + 1.0f, transform.rotation.z, Space.Self);
            else
                turnRight(latSpd);

            dir_thrust[2] = "rot_right";
            dir_thrust_used = true;
        }
        else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow))
        {
            if (enableAbsoluteCmds)
                transform.Rotate(transform.rotation.x, transform.rotation.y - 1.0f, transform.rotation.z, Space.Self);
            else
                turnLeft(latSpd);

            dir_thrust[2] = "rot_left";
            dir_thrust_used = true;
        }

        //////    Individual thrusters     //////
        // 
        //   fwd
        //  Y    U
        //  
        //  T    I

        // RL
        if (Input.GetKey(KeyCode.T))
        {
            applyThrust(latThrusters[2], latSpd);
            raw_thrust[2] = raw_thrust_spd;
            raw_thrust_used = true;
        }
        else if (Input.GetKey(KeyCode.G))
        {
            applyThrust(latThrusters[2], -latSpd);
            raw_thrust[2] = -raw_thrust_spd;
            raw_thrust_used = true;
        }

        // FL
        if (Input.GetKey(KeyCode.Y))
        {
            applyThrust(latThrusters[3], latSpd);
            raw_thrust[3] = raw_thrust_spd;
            raw_thrust_used = true;
        }
        else if (Input.GetKey(KeyCode.H))
        {
            applyThrust(latThrusters[3], -latSpd);
            raw_thrust[3] = -raw_thrust_spd;
            raw_thrust_used = true;
        }

        // FR
        if (Input.GetKey(KeyCode.U))
        {
            applyThrust(latThrusters[0], latSpd);
            raw_thrust[0] = raw_thrust_spd;
            raw_thrust_used = true;
        }
        else if (Input.GetKey(KeyCode.J))
        {
            applyThrust(latThrusters[0], -latSpd);
            raw_thrust[0] = -raw_thrust_spd;
            raw_thrust_used = true;
        }

        // RR
        if (Input.GetKey(KeyCode.I))
        {
            applyThrust(latThrusters[1], latSpd);
            raw_thrust[1] = raw_thrust_spd;
            raw_thrust_used = true;
        }
        else if (Input.GetKey(KeyCode.K))
        {
            applyThrust(latThrusters[1], -latSpd);
            raw_thrust[1] = -raw_thrust_spd;
            raw_thrust_used = true;
        }
    }

    // apply force at position of thruster transform 't'
    // also draws green ray to show thrust in editor
    // TODO: apply ray to camera views and GUI
    public void applyThrust(Transform t, float spd = 1.0f)
    {
        if (enableManualCmds)
            rb.AddForceAtPosition(spd * Time.deltaTime * t.transform.up, t.transform.position);

        if (drawLinesManual)
            ld.DrawLine(t.transform.position, t.transform.position + (-spd * 0.3f * t.transform.up * Time.deltaTime), Color.green, 0.1f);
        // Debug.DrawRay(t.transform.position, -spd * Time.deltaTime * t.transform.up, Color.green);
    }

    public void applyLatThrust(int tNum, float spd = 1.0f)
    {
        if (tNum >= latThrusters.Length)
        {
            Debug.Log("invali thruster num, lat");
            return;
        }
        Transform t = latThrusters[tNum];
        rb.AddForceAtPosition(spd * Time.deltaTime * t.transform.up, t.transform.position);

        // UI thrust
        Transform uiT = UiLatThrusters[tNum];
        uiT.GetChild(0).gameObject.SetActive(spd < 0);
        uiT.GetChild(1).gameObject.SetActive(spd > 0);
    
        if (drawLinesCmd)
            ld.DrawLine(t.transform.position, t.transform.position + (-spd * 0.3f * t.transform.up * Time.deltaTime), Color.green, 0.1f);
        // Debug.DrawRay(t.transform.position, -spd * Time.deltaTime * t.transform.up, Color.green);
    }

    public void applyVertThrust(int tNum, float spd = 1.0f)
    {
        if (tNum >= vertThrusters.Length)
        {
            Debug.Log("invali thruster num, vert");
            return;
        }
        Transform t = vertThrusters[tNum];
        rb.AddForceAtPosition(spd * Time.deltaTime * t.transform.up, t.transform.position);

        // UI thrust
        Transform uiT = UiVertThrusters[tNum];
        uiT.GetChild(0).gameObject.SetActive(spd < 0);
        uiT.GetChild(1).gameObject.SetActive(spd > 0);

        if (drawLinesCmd)
            ld.DrawLine(t.transform.position, t.transform.position + (-spd * 0.3f * t.transform.up * Time.deltaTime), Color.green, 0.1f);
        // // Debug.DrawRay(t.transform.position, -spd * Time.deltaTime * t.transform.up, Color.green);
    }

    // 4 vectored thruster move forward command
    // +/- latSpd to control output direction
    // thrusters ordered clockwise {frontRight, backRight, backLeft, fronLeft}
    public void moveForward(float spd)
    {
        applyThrust(latThrusters[0], spd);
        applyThrust(latThrusters[1], spd);
        applyThrust(latThrusters[2], spd);
        applyThrust(latThrusters[3], spd);
    }

    public void moveReverse(float spd)
    {
        applyThrust(latThrusters[0], -spd);
        applyThrust(latThrusters[1], -spd);
        applyThrust(latThrusters[2], -spd);
        applyThrust(latThrusters[3], -spd);
    }

    public void strafeRight(float spd)
    {
        applyThrust(latThrusters[0], -spd);
        applyThrust(latThrusters[1], spd);
        applyThrust(latThrusters[2], -spd);
        applyThrust(latThrusters[3], spd);
    }

    public void strafeLeft(float spd)
    {
        applyThrust(latThrusters[0], spd);
        applyThrust(latThrusters[1], -spd);
        applyThrust(latThrusters[2], spd);
        applyThrust(latThrusters[3], -spd);
    }

    public void turnRight(float spd)
    {
        applyThrust(latThrusters[0], -spd);
        applyThrust(latThrusters[1], -spd);
        applyThrust(latThrusters[2], spd);
        applyThrust(latThrusters[3], spd);
    }

    public void turnLeft(float spd)
    {
        applyThrust(latThrusters[0], spd);
        applyThrust(latThrusters[1], spd);
        applyThrust(latThrusters[2], -spd);
        applyThrust(latThrusters[3], -spd);
    }

    public void vertUp(float spd)
    {
        foreach(Transform t in vertThrusters){
            applyThrust(t, spd);
        }
    }

    public void vertDown(float spd)
    {
        foreach(Transform t in vertThrusters){
            applyThrust(t, -spd);
        }
    }

    public void resetScene()
    {
        SceneManager.LoadScene (SceneManager.GetActiveScene ().name); // resets lvl
    }

    public void enableManual()
    {
        enableManualCmds = true;
        enableAbsoluteCmds = false;
        rb.isKinematic = false;
    }

    public void enableAbsolute()
    {
        enableAbsoluteCmds = true;
        enableManualCmds = false;
        rb.isKinematic = true;
    }
}
