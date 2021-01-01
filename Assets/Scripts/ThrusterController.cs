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
    public Rigidbody rb;
    public float vertSpd = 5.0f;
    public float latSpd = 5.0f;
    
    public bool enableManualCmds = false;
    public bool enableAbsoluteCmds = false;

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
        // ehcange what spacebar does
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
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x,
                                                 transform.position.y,
                                                 transform.position.z - 0.1f);
            else
                strafeLeft(latSpd);
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
        }
        else if (Input.GetKey(KeyCode.S))
        {
            if (enableAbsoluteCmds)
                transform.position = new Vector3(transform.position.x + 0.1f,
                                                 transform.position.y,
                                                 transform.position.z);
            else
                moveReverse(latSpd);
        }

        // turn
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow))
        {
            turnRight(latSpd);
        }
        else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow))
        {
            turnLeft(latSpd);
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
    public void applyThrust(Transform t, float spd = 1.0f)
    {
        if (enableManualCmds)
            rb.AddForceAtPosition(spd * Time.deltaTime * t.transform.up, t.transform.position);

        if (drawLines)
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
    
        // if (drawLines)
        //     ld.DrawLine(t.transform.position, t.transform.position + (-spd * 0.3f * t.transform.up * Time.deltaTime), Color.green, 0.1f);
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

        // if (drawLines)
        //     ld.DrawLine(t.transform.position, t.transform.position + (-spd * 0.3f * t.transform.up * Time.deltaTime), Color.green, 0.1f);
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
