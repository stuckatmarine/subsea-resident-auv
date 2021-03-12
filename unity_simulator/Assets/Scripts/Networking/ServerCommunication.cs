using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Forefront class for the server communication.
/// </summary>
public class ServerCommunication : MonoBehaviour
{
    public enum State
    {
        Idle,
        Manual,
        Auto
    }

    public bool send_cmds = false;
    public bool send_tel = false;
    public bool disableWebsocketServer = false; // disables all socket stuff for training
    public bool useWebsocket = false; // uses tcp raw socket instead of websocket
    public bool useTcpSocket = false; // uses tcp raw socket instead of websocket
    public bool enableLogging = false;
    public bool enableVehicleCmds = false; // pi_fly_sim, vehicle sends commands up (do not use)
    public bool sendScreenshots = false; // sends fron cam img as string
    
    public string headlightSetting = "low";
    public State controlState = State.Idle;

    public TcpSocket sock; // tcpSocket ip and port set in other script
    public byte[] tcpTxBytes = new byte[65000];
    public byte[] tcpRxBytes = new byte[65000];

    // Server IP address
    [SerializeField]
    private string hostIP;

    // Server port
    [SerializeField]
    private int port = 3000;

    // Flag to use localhost
    [SerializeField]
    private bool useLocalhost = false;
    [SerializeField]
    private int txNum = 0;
    [SerializeField]
    private int rxNum = 0;

    public int txIntervalMs = 1000;
    private int lastTxTime = 0;
    public CommandModel cmd_msg = new CommandModel();
    public CamPicModel cam_msg = new CamPicModel();
    public TelemetryModel tel_msg = new TelemetryModel();

    // Address used in code
    private string host => useLocalhost ? "localhost" : hostIP;
    // Final server address
    private string server;
    
    public float[] distancesFloat;
    public Transform srauv;
    private Rigidbody rb;
    public Vector3 goalPos;
    public Transform dock;
    public Transform tree1;
    public Transform tree2;
    public Transform tree3;
    public Camera frontCam;
    private Texture2D frontCamTexture;
    private GameObject[] spotlights;

    //  UI
    public Transform simHeading;
    public Transform simX;
    public Transform simY;
    public Transform simZ;

    public Transform telState;
    public Transform telHeading;
    public Transform telX;
    public Transform telY;
    public Transform telZ;
    public float latMax;
    public float latMin;
    public float vertMax;
    public float vertMin;
    public float[] telDistanceFloats;
    public Transform[] telDistanceTransforms;

    private float[] raw_thrust_arr;
    private string[] dir_thrust_arr;

    // WebSocket Client
    private WsClient client;

    private GameObject thrusterController;
    private float[] forces = new float[]{0.0f, 0.0f, 0.0f,0.0f,0.0f,0.0f}; // applied to sim


    /// <summary>
    /// Unity method called on initialization
    /// </summary>
    private void Awake()
    {
        sock = GetComponent<TcpSocket>();
        srauv = GameObject.Find("SRAUV").GetComponent<Transform>();
        distancesFloat = srauv.GetComponent<DistanceSensors>().distancesFloat;
        goalPos = srauv.GetComponent<Pilot>().goal;
        raw_thrust_arr = srauv.GetComponent<ThrusterController>().raw_thrust;
        cmd_msg.raw_thrust = new float[]{0.0f, 0.0f, 0.0f,0.0f,0.0f,0.0f};
        
        cmd_msg.dir_thrust = new string[]{"", "", "", ""};
        cmd_msg.headlight_setting = headlightSetting;
        rb = srauv.GetComponent<Rigidbody>();

        spotlights =  GameObject.FindGameObjectsWithTag("spotlight");

        //dock = GameObject.Find("Dock").GetComponent<Transform>();

        
        if (disableWebsocketServer)
            return;

        //tree1 = GameObject.Find("Tree1").GetComponent<Transform>();
        //tree2 = GameObject.Find("Tree2").GetComponent<Transform>();
        //tree3 = GameObject.Find("Tree3").GetComponent<Transform>();

        frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();

        if (!useTcpSocket)
        {
            server = "ws://" + host + ":" + port;
            Debug.Log("using websocket setver " + server);
            client = new WsClient(server);
            ConnectToServer();
        }
        setSpotlights();
    }


    /// <summary>
    /// Unity method called every frame
    /// </summary>
    private void Update()
    {
        if (disableWebsocketServer)
            return;

        //  Update UI
        simHeading.GetComponent<TMPro.TextMeshProUGUI>().text = (srauv.rotation.y * 360.0f).ToString("#.0");
        simX.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.x.ToString("#.00");
        simY.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.y.ToString("#.00");
        simZ.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.z.ToString("#.00");

        if (!useTcpSocket)
        {
            // Check if server send new messages
            var cqueue = client.receiveQueue;
            string msg;
            while (cqueue.TryPeek(out msg))
            {
                // Parse newly received messages
                cqueue.TryDequeue(out msg);
                HandleMessage(msg);
            }
        }

        if (Time.time * 1000 > lastTxTime + txIntervalMs)
        {
            if (send_cmds)
                SendCmdRequest("cmd");
            else if (send_tel)
                SendTelRequest("tel");

            lastTxTime = (int)Time.time * 1000;
        }

        srauv.GetComponent<ThrusterController>().applyLatThrust(0, forces[0]);
        srauv.GetComponent<ThrusterController>().applyLatThrust(1, forces[1]);
        srauv.GetComponent<ThrusterController>().applyLatThrust(2, forces[2]);
        srauv.GetComponent<ThrusterController>().applyLatThrust(3, forces[3]);
        srauv.GetComponent<ThrusterController>().applyVertThrust(0, forces[4]);
        srauv.GetComponent<ThrusterController>().applyVertThrust(1, forces[5]);
    }

    /// <summary>
    /// Method responsible for handling server messages
    /// </summary>
    /// <param name="msg">Message.</param>
    private void HandleMessage(string msg)
    {
        Debug.Log("Rx: " + rxNum++ + ", msg: " + msg);

        // Deserializing message from the server
        var message = JsonUtility.FromJson<CommandModel>(msg);

        // // Picking correct method for message handling
        switch (message.msg_type)
        {
            case "command":
                {
                    if (!enableVehicleCmds)
                    {
                        Debug.Log("vehicle cmds not eabled");
                        break;
                    }
                    
                    Debug.Log("Apply Forces Here");
                    for (int i = 0; i < 6; i++)
                        forces[i] = message.raw_thrust[i];
                }
                break;
            case "telemetry":
                Debug.Log("Updating UI with telemetry from server");
                {
                    //  Update UI
                    var tel = JsonUtility.FromJson<TelemetryModel>(msg);

                    telX.GetComponent<TMPro.TextMeshProUGUI>().text = tel.pos_x.ToString("#.00");
                    telY.GetComponent<TMPro.TextMeshProUGUI>().text = tel.pos_y.ToString("#.00");
                    telZ.GetComponent<TMPro.TextMeshProUGUI>().text = tel.pos_z.ToString("#.00");
                    // Debug.Log("imu dict" + tel.imu_dict.heading);
                    telHeading.GetComponent<TMPro.TextMeshProUGUI>().text = tel.imu_dict.heading.ToString("#.0");
                    telState.GetComponent<TMPro.TextMeshProUGUI>().text = tel.state;
                    
                    forces = tel.thrust_values;
                    telDistanceFloats = tel.dist_values;
                    // for (int i = 0; i < 6; i++)
                    // {
                    //     telDistanceFloats[i] = tel.dist_values[i];
                    // }
                    
                    // colorize/"max" limits
                    for (int i = 0; i < 4; i++)
                    {
                        updateValues(i, latMin, latMax);
                    }
                    for (int i = 4; i < 6; i++)
                    {
                        updateValues(i, vertMin, vertMax);
                    }

                    // for (int i = 0; i < 6; i++)
                    //     forces[i] = message.raw_thrust[i];
                }
                break;
            case "reset":
                SceneManager.LoadScene (SceneManager.GetActiveScene ().name);
                break;
            default:
                Debug.Log("Unknown type of method: " + message.msg_type);
                break;
        }
    }

    /// <summary>
    /// Call this method to connect to the server
    /// </summary>
    public async void ConnectToServer()
    {
        await client.Connect();
    }

    /// <summary>
    /// Method which sends data through websocket
    /// </summary>
    /// <param name="message">Message.</param>
    public void SendTelRequest(string message)
    {
        tel_msg.source = "sim";
        tel_msg.dest = "vehicle";
        tel_msg.msg_num = txNum++;
        tel_msg.msg_type = "telemetry";
        DateTime timestamp = DateTime.Now;
        tel_msg.timestamp = timestamp.ToString("MM/dd/yyy HH:mm:ss.") + DateTime.Now.Millisecond.ToString();
        tel_msg.pos_x = srauv.position.x;
        tel_msg.pos_y = srauv.position.y;
        tel_msg.pos_z = srauv.position.z;
        tel_msg.depth = distancesFloat[4];
        tel_msg.alt = distancesFloat[5];
        tel_msg.imu_dict.heading = srauv.rotation.y * 360.0f;
        tel_msg.imu_dict.gyro_y = rb.angularVelocity.y;
        tel_msg.imu_dict.vel_x = rb.velocity.x;
        tel_msg.imu_dict.vel_y = rb.velocity.y;
        tel_msg.imu_dict.vel_z = rb.velocity.z;
        tel_msg.dist_values = distancesFloat;

        // tel_msg.roll = srauv.rotation.x * 360.0f;
        // tel_msg.pitch = srauv.rotation.z * 360.0f;
        // if (dock)
        // {
        //     tel_msg.dockDist = Vector3.Distance(srauv.position, dock.position);
        //     tel_msg.dockDistX = srauv.position.x - dock.position.x;
        //     tel_msg.dockDistY = srauv.position.y - dock.position.y;
        //     tel_msg.dockDistZ = srauv.position.z - dock.position.z;
        // }
        // if (tree1)
        // {
        //     tel_msg.tree1Dist = Vector3.Distance(srauv.position, tree1.position);
        //     tel_msg.tree1DistX = srauv.position.x - tree1.position.x;
        //     tel_msg.tree1DistY = srauv.position.y - tree1.position.y;
        //     tel_msg.tree1DistZ = srauv.position.z - tree1.position.z;
        // }
        // if (tree2)
        // {
        //     tel_msg.tree2Dist = Vector3.Distance(srauv.position, tree2.position);
        //     tel_msg.tree2DistX = srauv.position.x - tree2.position.x;
        //     tel_msg.tree2DistY = srauv.position.y - tree2.position.y;
        //     tel_msg.tree2DistZ = srauv.position.z - tree2.position.z;
        // }
        // if (tree3)
        // {
        //     tel_msg.tree3Dist = Vector3.Distance(srauv.position, tree3.position);
        //     tel_msg.tree3DistX = srauv.position.x - tree3.position.x;
        //     tel_msg.tree3DistY = srauv.position.y - tree3.position.y;
        //     tel_msg.tree3DistZ = srauv.position.z - tree3.position.z;
        // }
        
        //string msg = JsonUtility.ToJson(tel_msg);

        if (useTcpSocket)
        {
            if (sock.connectTcpSocket())
            {
                if (sendScreenshots)
                {
                    frontCamTexture = getScreenshot(frontCam);
                    tel_msg.state = Convert.ToBase64String(frontCamTexture.EncodeToJPG());
                }
                
                string msg = JsonUtility.ToJson(tel_msg);
                if (enableLogging)
                    Debug.Log("Sending: " + msg);

                tcpTxBytes = System.Text.Encoding.UTF8.GetBytes(msg);
                tcpRxBytes = sock.SendAndReceive(tcpTxBytes);
                
                HandleMessage(System.Text.Encoding.UTF8.GetString(tcpRxBytes));
            }
            else
            {
                useTcpSocket = false;
            }
        }
        
        // if (useWebsocket)
        //     client.Send(msg);

        // send screenshot too after every x msgs
        if (sendScreenshots && txNum % 2 == 0)
        {
            cam_msg.source = "sim";
            cam_msg.msg_num = txNum;
            cam_msg.msg_type = "cam";
            cam_msg.timestamp = timestamp.ToString("MM/dd/yyy HH:mm:ss.") + DateTime.Now.Millisecond.ToString();

            frontCamTexture = getScreenshot(frontCam);
            // frontCamTexSture = frontCam;
            if (enableLogging)
                Debug.Log("Sending: " + cam_msg);
            
            // if (useWebsocket)
            // {
            //     byte[] bytes;
            //     bytes = frontCamTexture.EncodeToJPG();
            //     cam_msg.img_str = Convert.ToBase64String(bytes);

            //     msg = System.Text.Encoding.UTF8.GetBytes(cam_msg);
            //     if (enableLogging)
            //         Debug.Log("Sending Img of size: " + bytes.Length);

            //     client.Send(JsonUtility.ToJson(cam_msg));
            // }
            if (useTcpSocket)
            {
                if (sock.connectTcpSocket())
                {
                    tcpTxBytes = frontCamTexture.EncodeToJPG();
                    tcpRxBytes = sock.SendAndReceive(tcpTxBytes);

                    if (enableLogging)
                        Debug.Log("Sending Img of size: " + tcpTxBytes.Length);

                    HandleMessage(System.Text.Encoding.UTF8.GetString(tcpRxBytes));
                }
                else
                {
                    useTcpSocket = false;
                }
            }

        }
    }

    /// <summary>
    /// Method which sends data through websocket
    /// </summary>
    /// <param name="message">Message.</param>
    public void SendCmdRequest(string message)
    {
        cmd_msg.msg_type = "command";
        cmd_msg.source = "sim";
        cmd_msg.dest = "vehicle";
        cmd_msg.msg_num++;
        cmd_msg.headlight_setting = headlightSetting;
        DateTime timestamp = DateTime.Now;
        cmd_msg.timestamp = timestamp.ToString("MM/dd/yyy HH:mm:ss.") + DateTime.Now.Millisecond.ToString();
        
        
        for (int i = 0; i < 4; i++)
        {
            string dt = srauv.GetComponent<ThrusterController>().dir_thrust[i];
            cmd_msg.dir_thrust[i] = dt;
        }
        for (int i = 0; i < 6; i++)
        {
            cmd_msg.raw_thrust[i] = srauv.GetComponent<ThrusterController>().raw_thrust[i];
        }

        Debug.Log("dir_thrust_used: " + srauv.GetComponent<ThrusterController>().dir_thrust_used);
        Debug.Log("raw_thrust_used: " + srauv.GetComponent<ThrusterController>().raw_thrust_used);

        if (controlState == State.Manual)
        {
            cmd_msg.force_state = "manual";
            cmd_msg.can_thrust = true;

            if (srauv.GetComponent<ThrusterController>().dir_thrust_used)
                cmd_msg.thrust_type = "dir_thrust";
            else if (srauv.GetComponent<ThrusterController>().raw_thrust_used)
                cmd_msg.thrust_type = "raw_thrust";
            else
                cmd_msg.thrust_type = "";
        }
        else if (controlState == State.Auto)
        {
            cmd_msg.force_state = "autonomous";
        }
        else
        {
            cmd_msg.thrust_type = "";
            cmd_msg.can_thrust = false;

            cmd_msg.force_state = "idle";
        }
        

        string msg = JsonUtility.ToJson(cmd_msg);

        if (enableLogging)
            Debug.Log("Sending: " + msg);

        if (useTcpSocket)
        {
            if (sock.connectTcpSocket())
            {
                tcpTxBytes = System.Text.Encoding.UTF8.GetBytes(msg);
                tcpRxBytes = sock.SendAndReceive(tcpTxBytes);
                
                HandleMessage(System.Text.Encoding.UTF8.GetString(tcpRxBytes));
            }
            else
            {
                useTcpSocket = false;
            }
        }
        
        if (useWebsocket)
            client.Send(msg);
    }


    // Take a "screenshot" of a camera's Render Texture.
    private Texture2D getScreenshot(Camera camera)
    {
        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        var currentRT = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        // Render the camera's view.
        camera.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        image.Apply();

        // Replace the original active Render Texture.
        RenderTexture.active = currentRT;
        return image;
    }

    public void enableVehicle()
    {
        enableVehicleCmds = true;
        srauv.GetComponent<Rigidbody>().isKinematic = false;
    }

    public void setStateIdle()
    {
        controlState = State.Idle;
    }

    public void setStateManual()
    {
        controlState = State.Manual;
    }

    public void setStateAuto()
    {
        controlState = State.Auto;
    }

    private void updateValues(int i, float min, float max)
    {
        if (telDistanceFloats[i] < max)
            telDistanceTransforms[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = telDistanceFloats[i].ToString("#.00");
        else
            telDistanceTransforms[i].GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "MAX";

        // min distance for close proximity checks
        if (telDistanceFloats[i] < min)
        {
            telDistanceTransforms[i].GetComponent<Image>().color = Color.red;
        }
        else
            telDistanceTransforms[i].GetComponent<Image>().color = Color.grey;
    }

    public void toggleSendCmds()
    {
        send_cmds =  !send_cmds;
    }

    public void sendTcp()
    {
        sock.SendAndReceive( System.Text.Encoding.UTF8.GetBytes("testtt"));
    }

    public void setSpotlights()
    {
        foreach(GameObject s in spotlights)
        {
            if (headlightSetting == "low")
                s.SetActive(true);
            else
                s.SetActive(false);
        }
    } 

    public void toggleHeadlights()
    {
        if (headlightSetting == "low")
            headlightSetting = "high";
        else
            headlightSetting = "low";

        setSpotlights();
    } 
}
