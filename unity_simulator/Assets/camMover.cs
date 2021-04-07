using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;



public class camMover : MonoBehaviour
{

    public Rigidbody rb;
    public float spd;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.X))
        {
            rb.AddForceAtPosition(spd * Time.deltaTime * transform.forward, transform.position);
        }
        if (Input.GetKey(KeyCode.Z))
        {
            rb.AddForceAtPosition(-spd * Time.deltaTime * transform.forward, transform.position);
        }
        if (Input.GetKey(KeyCode.N))
        {
            rb.AddForceAtPosition(spd * Time.deltaTime * transform.up, transform.position);
        }
        if (Input.GetKey(KeyCode.B))
        {
            rb.AddForceAtPosition(-spd * Time.deltaTime * transform.up, transform.position);
        }
    }
}
