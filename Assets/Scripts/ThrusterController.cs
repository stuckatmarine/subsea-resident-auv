using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterController : MonoBehaviour
{
    public Transform[] vertThrusters; // front, rear
    public Transform[] latThrusters; // clockwise from 0deg heading
    public Rigidbody rb;
    public float vertSpd = 5.0f;
    public float latSpd = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // verts
        if (Input.GetKey(KeyCode.UpArrow))
        {
            foreach(Transform t in vertThrusters){
                rb.AddForceAtPosition(new Vector3(0.0f, vertSpd, 0.0f) * Time.deltaTime, t.transform.position);
                Debug.DrawRay(t.transform.position, new Vector3(0.0f, -vertSpd, 0.0f) * Time.deltaTime, Color.green);
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            foreach(Transform t in vertThrusters){
                rb.AddForceAtPosition(new Vector3(0.0f, -vertSpd, 0.0f) * Time.deltaTime, t.transform.position);
                Debug.DrawRay(t.transform.position, new Vector3(0.0f, vertSpd, 0.0f) * Time.deltaTime, Color.green);
            }
        }

        // lats
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // FR
            rb.AddForceAtPosition(new Vector3(0.0f, 0.0f, -latSpd) * Time.deltaTime, latThrusters[0].transform.position);
            Debug.DrawRay(latThrusters[0].transform.position, new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, Color.green);

            // FL
            rb.AddForceAtPosition(new Vector3(latSpd, 0.0f) * Time.deltaTime, latThrusters[3].transform.position);
            Debug.DrawRay(latThrusters[3].transform.position, new Vector3(-latSpd, 0.0f,  0.0f) * Time.deltaTime, Color.green);
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            // FR
            rb.AddForceAtPosition(new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, latThrusters[0].transform.position);
            Debug.DrawRay(latThrusters[0].transform.position, new Vector3(0.0f, 0.0f, -latSpd) * Time.deltaTime, Color.green);

            // FL
            rb.AddForceAtPosition(new Vector3(-latSpd, 0.0f, 0.0f) * Time.deltaTime, latThrusters[3].transform.position);
            Debug.DrawRay(latThrusters[3].transform.position, new Vector3(latSpd, 0.0f, 0.0f) * Time.deltaTime, Color.green);
        }

        // RL
        if (Input.GetKey(KeyCode.Q))
        {
            rb.AddForceAtPosition(new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, latThrusters[2].transform.position);
            Debug.DrawRay(latThrusters[2].transform.position, new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, Color.green);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            rb.AddForceAtPosition(new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, latThrusters[2].transform.position);
            Debug.DrawRay(latThrusters[2].transform.position, new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, Color.green);
        }

        // FL
        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForceAtPosition(new Vector3(latSpd, 0.0f) * Time.deltaTime, latThrusters[3].transform.position);
            Debug.DrawRay(latThrusters[3].transform.position, new Vector3(-latSpd, 0.0f,  0.0f) * Time.deltaTime, Color.green);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            rb.AddForceAtPosition(new Vector3(-latSpd, 0.0f, 0.0f) * Time.deltaTime, latThrusters[3].transform.position);
            Debug.DrawRay(latThrusters[3].transform.position, new Vector3(latSpd, 0.0f, 0.0f) * Time.deltaTime, Color.green);
        }

        // FR
        if (Input.GetKey(KeyCode.E))
        {
            rb.AddForceAtPosition(new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, latThrusters[0].transform.position);
            Debug.DrawRay(latThrusters[0].transform.position, new Vector3(0.0f, 0.0f, -latSpd) * Time.deltaTime, Color.green);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            rb.AddForceAtPosition(new Vector3(0.0f, 0.0f, -latSpd) * Time.deltaTime, latThrusters[0].transform.position);
            Debug.DrawRay(latThrusters[0].transform.position, new Vector3(0.0f, 0.0f, latSpd) * Time.deltaTime, Color.green);
        }
    }
}
