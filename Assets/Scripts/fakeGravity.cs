using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fakeGravity : MonoBehaviour
{
    public Rigidbody rb;
    public bool gravDown = true;
    public bool gravUp = false;
    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (gravDown)
            rb.AddForce (Vector3.down * 9.81f, ForceMode.Acceleration);
        
        if (gravUp)
            rb.AddForceAtPosition (Vector3.up * 9.81f, transform.position + Vector3.up * 2, ForceMode.Acceleration);
    }
}
