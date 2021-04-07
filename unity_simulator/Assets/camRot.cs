using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class camRot : MonoBehaviour
{
    
    public float spd;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.V))
        {
            transform.rotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, transform.eulerAngles.y - spd, transform.eulerAngles.z));
        }
        if (Input.GetKey(KeyCode.C))
        {
            transform.rotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + spd, transform.eulerAngles.z));
        }
        if (Input.GetKey(KeyCode.M))
        {
            transform.rotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - spd));
        }
    }
}
