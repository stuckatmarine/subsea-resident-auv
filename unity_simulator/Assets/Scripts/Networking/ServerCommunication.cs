﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Text;
using UnityEngine.UI;

/// <summary>
/// Forefront class for the server communication.
/// </summary>
public class ServerCommunication : MonoBehaviour
{
    public bool send_cmds = false;
    public bool send_tel = false;
    public bool disableWebsocketServer = false;
    public bool enableLogging = false;
    public bool enableVehicleCmds = false;
    public bool sendScreenshots = false;

    // Server IP address
    [SerializeField]
    private string hostIP;

    // Server port
    [SerializeField]
    private int port = 3000;

    // Flag to use localhost
    [SerializeField]
    private bool useLocalhost = true;
    [SerializeField]
    private int txNum = 0;
    [SerializeField]
    private int rxNum = 0;

    public int txIntervalMs = 1000;
    private int lastTxTime = 0;
    public TelemetryModel tel_msg = new TelemetryModel();
    public CommandModel cmd_msg = new CommandModel();
    public CamPicModel cam_msg = new CamPicModel();

    // Address used in code
    private string host => useLocalhost ? "localhost" : hostIP;
    // Final server address
    private string server;
    
    // public Transform[] distSensorVals;
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

    //  UI
    public Transform simHeading;
    public Transform simX;
    public Transform simY;
    public Transform simZ;

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
    private float[] forces = new float[]{0.0f, 0.0f, 0.0f,0.0f,0.0f,0.0f};

    // Class with messages for "lobby"
    // public LobbyMessaging Lobby { private set; get; }

    /// <summary>
    /// Unity method called on initialization
    /// </summary>
    private void Awake()
    {
        srauv = GameObject.Find("SRAUV").GetComponent<Transform>();
        distancesFloat = srauv.GetComponent<DistanceSensors>().distancesFloat;
        goalPos = srauv.GetComponent<Pilot>().goal;
        raw_thrust_arr = srauv.GetComponent<ThrusterController>().raw_thrust;
        cmd_msg.raw_thrust = new float[]{0.0f, 0.0f, 0.0f,0.0f,0.0f,0.0f};
        
        cmd_msg.dir_thrust = new string[]{"", "", "", ""};
        rb = srauv.GetComponent<Rigidbody>();


        dock = GameObject.Find("Dock").GetComponent<Transform>();

        
        if (disableWebsocketServer)
            return;

        tree1 = GameObject.Find("Tree1").GetComponent<Transform>();
        tree2 = GameObject.Find("Tree2").GetComponent<Transform>();
        tree3 = GameObject.Find("Tree3").GetComponent<Transform>();

        frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();

        server = "ws://" + host + ":" + port;
        client = new WsClient(server);
        ConnectToServer(); 
    }


    /// <summary>
    /// Unity method called every frame
    /// </summary>
    private void Update()
    {
        if (disableWebsocketServer) // extra blocker
            return;

        //  Update UI
        simHeading.GetComponent<TMPro.TextMeshProUGUI>().text = (srauv.rotation.y * 360.0f).ToString("#.0");
        simX.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.x.ToString("#.00");
        simY.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.y.ToString("#.00");
        simZ.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.z.ToString("#.00");

        if (disableWebsocketServer)
            return;

        // Check if server send new messages
        var cqueue = client.receiveQueue;
        string msg;
        while (cqueue.TryPeek(out msg))
        {
            // Parse newly received messages
            cqueue.TryDequeue(out msg);
            HandleMessage(msg);
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
                    telHeading.GetComponent<TMPro.TextMeshProUGUI>().text = tel.heading.ToString("#.0");
                    telDistanceFloats[0] = tel.fwd_dist;
                    telDistanceFloats[1] = tel.right_dist;
                    telDistanceFloats[2] = tel.rear_dist;
                    telDistanceFloats[3] = tel.left_dist;
                    telDistanceFloats[4] = tel.depth;
                    telDistanceFloats[5] = tel.alt;
                    
                    // colorize/"max" limits
                    for (int i = 0; i < 4; i++)
                    {
                        updateValues(i, latMin, latMax);
                    }
                    for (int i = 4; i < 6; i++)
                    {
                        updateValues(i, vertMin, vertMax);
                    }

                    for (int i = 0; i < 6; i++)
                        forces[i] = message.raw_thrust[i];
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
        tel_msg.fwd_dist = distancesFloat[0];
        tel_msg.right_dist = distancesFloat[1];
        tel_msg.rear_dist = distancesFloat[2];
        tel_msg.left_dist = distancesFloat[3];
        tel_msg.depth = distancesFloat[4];
        tel_msg.alt = distancesFloat[5];
        tel_msg.pos_x = srauv.position.x;
        tel_msg.pos_y = srauv.position.y;
        tel_msg.pos_z = srauv.position.z;
        tel_msg.vel_x = rb.velocity.x;
        tel_msg.vel_y = rb.velocity.y;
        tel_msg.vel_z = rb.velocity.z;
        tel_msg.heading = srauv.rotation.y * 360.0f;

        tel_msg.target_pos_x = goalPos.x;
        tel_msg.target_pos_y = goalPos.y;
        tel_msg.target_pos_z = goalPos.z;

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
        
        string msg = JsonUtility.ToJson(tel_msg);

        if (enableLogging)
            Debug.Log("Sending: " + msg);

        client.Send(msg);

        

        // send screenshot too after every x msgs
        if (sendScreenshots && txNum % 2 == 0)
        {
            cam_msg.source = "sim";
            cam_msg.msgNum = txNum++;
            cam_msg.msgType = "cam";
            cam_msg.timestamp = timestamp.ToString("MM/dd/yyy HH:mm:ss.") + DateTime.Now.Millisecond.ToString();

            frontCamTexture = getScreenshot(frontCam);
            // frontCamTexSture = frontCam;
            byte[] bytes;
            bytes = frontCamTexture.EncodeToJPG();
            cam_msg.imgStr = Convert.ToBase64String(bytes);

            msg = JsonUtility.ToJson(cam_msg);
            if (enableLogging)
                Debug.Log("Sending Img: " + msg);

            client.Send(msg);
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
        DateTime timestamp = DateTime.Now;
        cmd_msg.timestamp = timestamp.ToString("MM/dd/yyy HH:mm:ss.") + DateTime.Now.Millisecond.ToString();
        
        cmd_msg.force_state = "manual";
        cmd_msg.can_thrust = true;
        for (int i = 0; i < 4; i++)
        {
            string dt = srauv.GetComponent<ThrusterController>().dir_thrust[i];
            cmd_msg.dir_thrust[i] = dt;
        }
        for (int i = 0; i < 6; i++)
        {
            cmd_msg.raw_thrust[i] = srauv.GetComponent<ThrusterController>().raw_thrust[i];
        }
        if (srauv.GetComponent<ThrusterController>().dir_thrust_used)
            cmd_msg.thrust_type = "dir_thrust";
        if (srauv.GetComponent<ThrusterController>().raw_thrust_used)
            cmd_msg.thrust_type = "raw_thrust";
        else
            cmd_msg.thrust_type = "";
        

        string msg = JsonUtility.ToJson(cmd_msg);

        if (enableLogging)
            Debug.Log("Sending: " + msg);

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
}
