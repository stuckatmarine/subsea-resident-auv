using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSaver : MonoBehaviour
{
    private bool saving = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (saving)
            {
                stopSaving();
            }
            else
                startSaving();

            Debug.Log("Saving: " + saving);
        }
    }

    private void stopSaving()
    {
        saving = false;
    }

    private void startSaving()
    {
        saving  = true;
    }
}
