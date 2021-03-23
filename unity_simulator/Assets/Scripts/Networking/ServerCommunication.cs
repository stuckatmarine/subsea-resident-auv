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
        SimpleAi,
        Auto
    }

    public bool useSrauvPos = false;
    public bool send_cmds = false;
    public bool send_tel = false;
    public bool disableWebsocketServer = false; // disables all socket stuff for training
    public bool useWebsocket = false;           // uses tcp raw socket instead of websocket
    public bool useTcpSocket = false;           // uses tcp raw socket instead of websocket
    public bool enableLogging = false;
    public bool enableVehicleCmds = false;      // pi_fly_sim, vehicle sends commands up (do not use)
    public bool sendScreenshots = false;        // sends fron cam img as string
    public bool isMlTank = false;               // sends fron cam img as string
    
    public string headlightSetting = "low";
    public State controlState = State.Idle;

    public Color32 grn = new Color32(0, 138, 20, 255);
    public Color32 blu = new Color32(0, 138, 20, 255);
    public Color32 red = new Color32(0, 138, 20, 255);
    public Color32 gry = new Color32(0, 138, 20, 255);

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
    private bool clientConnected = false;

    public int txIntervalMs = 1000;
    private int lastTxTime = 0;
    public CommandModel cmd_msg = new CommandModel();
    public CamPicModel cam_msg = new CamPicModel();
    public TelemetryModel tel_msg = new TelemetryModel();

    // Address used in code
    private string host => useLocalhost ? "localhost" : hostIP;


    public GameObject missionLog;
    static private string baseLogMsg = "Not connected";
    public string logText = baseLogMsg;

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
    public GameObject targetWaypoint;
    private bool reset_to_first_waypoint = false;

    //  UI
    public Transform simHeading;
    public Transform simX;
    public Transform simY;
    public Transform simZ;

    public Button server_btn;
    public Button pos;
    public Button headlight;
    public Button waypoint;
    public Button idle;
    public Button manual;
    public Button simple;
    public Button auto;
    public Button reset_gui;
    public Transform telState;
    public Transform telHeading;
    public Transform telX;
    public Transform telY;
    public Transform telZ;
    public Transform vel_x;
    public Transform vel_y;
    public Transform vel_z;
    public Transform vel_rot;
    public Transform target_pos_x;
    public Transform target_pos_y;
    public Transform target_pos_z;
    public Transform target_heading;
    public float forcesDownscaler = 10.0f;
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
        missionLog.GetComponent<TMPro.TextMeshProUGUI>().text = logText;
        sock = GetComponent<TcpSocket>();
        srauv = GameObject.Find("SRAUV").GetComponent<Transform>();
        distancesFloat = srauv.GetComponent<DistanceSensors>().distancesFloat;
        raw_thrust_arr = srauv.GetComponent<ThrusterController>().raw_thrust;
        cmd_msg.raw_thrust = new float[]{0.0f, 0.0f, 0.0f,0.0f,0.0f,0.0f};
        cmd_msg.dir_thrust = new string[]{"", "", "", ""};
        cmd_msg.headlight_setting = headlightSetting;
        rb = srauv.GetComponent<Rigidbody>();
        spotlights =  GameObject.FindGameObjectsWithTag("spotlight");

        if (isMlTank)
            goalPos = srauv.GetComponent<Pilot>().goal;

        if (disableWebsocketServer)
            return;

        if (sendScreenshots)
            frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();

        if (useWebsocket)
        {
            server = "ws://" + host + ":" + port;
            Debug.Log("using websocket setver " + server);
            if (client == null)
            {   
                client = new WsClient(server);
                ConnectToServer();
            }
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
        simHeading.GetComponent<TMPro.TextMeshProUGUI>().text = (srauv.eulerAngles.y).ToString("#.0");
        simX.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.x.ToString("#.00");
        simY.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.y.ToString("#.00");
        simZ.GetComponent<TMPro.TextMeshProUGUI>().text = srauv.position.z.ToString("#.00");

        if (useWebsocket && client != null)
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

        // Debug.Log("Forces " + forces)
        srauv.GetComponent<ThrusterController>().applyLatThrust(0, forces[0], useSrauvPos);
        srauv.GetComponent<ThrusterController>().applyLatThrust(1, forces[1], useSrauvPos);
        srauv.GetComponent<ThrusterController>().applyLatThrust(2, forces[2], useSrauvPos);
        srauv.GetComponent<ThrusterController>().applyLatThrust(3, forces[3], useSrauvPos);
        srauv.GetComponent<ThrusterController>().applyVertThrust(0, forces[4], useSrauvPos);
        srauv.GetComponent<ThrusterController>().applyVertThrust(1, forces[5], useSrauvPos);
    }

    /// <summary>
    /// Method responsible for handling server messages
    /// </summary>
    /// <param name="msg">Message.</param>
    private void HandleMessage(string msg)
    {
        Debug.Log("Rx: " + rxNum++ + ", msg: " + msg);
        if (logText == baseLogMsg)
        {
            updateLog("Websocket Connected\n");
        }

        // Deserializing message from the server
        var message = JsonUtility.FromJson<CommandModel>(msg);

        // Picking correct method for message handling
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
                    vel_x.GetComponent<TMPro.TextMeshProUGUI>().text = tel.vel_x.ToString("#.00");
                    vel_y.GetComponent<TMPro.TextMeshProUGUI>().text = tel.vel_y.ToString("#.00");
                    vel_z.GetComponent<TMPro.TextMeshProUGUI>().text = tel.vel_z.ToString("#.00");
                    vel_rot.GetComponent<TMPro.TextMeshProUGUI>().text = tel.imu_dict.gyro_y.ToString("#.0");
                    target_pos_x.GetComponent<TMPro.TextMeshProUGUI>().text = tel.target_pos_x.ToString("#.00");
                    target_pos_y.GetComponent<TMPro.TextMeshProUGUI>().text = tel.target_pos_y.ToString("#.00");
                    target_pos_z.GetComponent<TMPro.TextMeshProUGUI>().text = tel.target_pos_z.ToString("#.00");
                    target_heading.GetComponent<TMPro.TextMeshProUGUI>().text = tel.target_heading_to.ToString("#.0");
                    telState.GetComponent<TMPro.TextMeshProUGUI>().text = tel.state;
                    
                    telState.parent.GetComponent<Image>().color = blu;
                    telX.parent.GetComponent<Image>().color = blu;
                    telY.parent.GetComponent<Image>().color = blu;
                    telZ.parent.GetComponent<Image>().color = blu;
                    telHeading.parent.GetComponent<Image>().color = blu;
                    vel_x.parent.GetComponent<Image>().color = blu;
                    vel_y.parent.GetComponent<Image>().color = blu;
                    vel_z.parent.GetComponent<Image>().color = blu;
                    vel_rot.parent.GetComponent<Image>().color = blu;
                    target_pos_x.parent.GetComponent<Image>().color = grn;
                    target_pos_y.parent.GetComponent<Image>().color = grn;
                    target_pos_z.parent.GetComponent<Image>().color = grn;
                    target_heading.parent.GetComponent<Image>().color = grn;

                    // gui buttons
                    server_btn.GetComponent<Image>().color = clientConnected && send_cmds ? blu : red;
                    pos.GetComponent<Image>().color = useSrauvPos ? blu : red;
                    headlight.GetComponent<Image>().color = headlightSetting == "low" ? blu : red;
                    waypoint.GetComponent<Image>().color = reset_to_first_waypoint ? blu : red;


                    idle.GetComponent<Image>().color = tel.state == "idle" ? blu : red;
                    manual.GetComponent<Image>().color = tel.state == "manual" ? blu : red;
                    simple.GetComponent<Image>().color = tel.state == "simple_ai" ? blu : red;
                    auto.GetComponent<Image>().color = tel.state == "autonomous" ? blu : red;
                    
                    if (tel.mission_msg != "")
                    {
                        updateLog(tel.mission_msg);
                    }

                    targetWaypoint.transform.position = new Vector3(tel.target_pos_x,
                                                                    tel.target_pos_y,
                                                                    tel.target_pos_z);

                    if (useSrauvPos)
                    {
                        srauv.position = new Vector3(tel.pos_x, tel.pos_y, tel.pos_z);
                        srauv.rotation = Quaternion.Euler(new Vector3((tel.imu_dict.roll), tel.heading, tel.imu_dict.pitch));
                    }

                    forces = tel.thrust_values;
                    for (int i = 0; i < 6; i++)
                    {
                        if (forces[i] != 0)
                            forces[i] = forces[i] / forcesDownscaler;
                    }
                    telDistanceFloats = tel.dist_values;
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
        clientConnected = true;
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
        tel_msg.heading = srauv.rotation.y * 360.0f;
        tel_msg.imu_dict.gyro_y = rb.angularVelocity.y;
        tel_msg.imu_dict.vel_x = rb.velocity.x;
        tel_msg.imu_dict.vel_y = rb.velocity.y;
        tel_msg.imu_dict.vel_z = rb.velocity.z;
        tel_msg.dist_values = distancesFloat;

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
        cmd_msg.pos_x = srauv.position.x;
        cmd_msg.pos_y = srauv.position.y;
        cmd_msg.pos_z = srauv.position.z;
        cmd_msg.depth = distancesFloat[4];
        cmd_msg.alt = distancesFloat[5];
        cmd_msg.imu_dict.pitch = srauv.eulerAngles.z;
        cmd_msg.imu_dict.heading = srauv.eulerAngles.y;
        cmd_msg.imu_dict.roll = srauv.eulerAngles.x;
        cmd_msg.imu_dict.gyro_x = rb.angularVelocity.x;
        cmd_msg.imu_dict.gyro_y = rb.angularVelocity.y;
        cmd_msg.imu_dict.gyro_z = rb.angularVelocity.z;
        cmd_msg.imu_dict.linear_accel_x = (rb.velocity.x - cmd_msg.imu_dict.vel_x) / Time.fixedDeltaTime;
        cmd_msg.imu_dict.linear_accel_y = (rb.velocity.y - cmd_msg.imu_dict.vel_y) / Time.fixedDeltaTime;
        cmd_msg.imu_dict.linear_accel_z = (rb.velocity.z - cmd_msg.imu_dict.vel_z) / Time.fixedDeltaTime;
        cmd_msg.imu_dict.vel_x = rb.velocity.x;
        cmd_msg.imu_dict.vel_y = rb.velocity.y;
        cmd_msg.imu_dict.vel_z = rb.velocity.z;
        cmd_msg.vel_x = rb.velocity.x;
        cmd_msg.vel_y = rb.velocity.y;
        cmd_msg.vel_z = rb.velocity.z;

        cmd_msg.reset_to_first_waypoint = reset_to_first_waypoint;
        if (reset_to_first_waypoint)
            setWaypointReset(false);
        
        for (int i = 0; i < 4; i++)
        {
            string dt = srauv.GetComponent<ThrusterController>().dir_thrust[i];
            cmd_msg.dir_thrust[i] = dt;
        }
        for (int i = 0; i < 6; i++)
        {
            cmd_msg.raw_thrust[i] = srauv.GetComponent<ThrusterController>().raw_thrust[i];
        }

        // Debug.Log("dir_thrust_used: " + srauv.GetComponent<ThrusterController>().dir_thrust_used);
        // Debug.Log("raw_thrust_used: " + srauv.GetComponent<ThrusterController>().raw_thrust_used);

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
        else if (controlState == State.SimpleAi)
        {
            cmd_msg.force_state = "simple_ai";
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

    public void setStateWaypoint()
    {
        controlState = State.SimpleAi;
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
            telDistanceTransforms[i].GetComponent<Image>().color = gry;
    }

    public void toggleSendCmds()
    {
        send_cmds =  !send_cmds;
    }

    public void toggleSrauvPos()
    {
        useSrauvPos =  !useSrauvPos;
        srauv.GetComponent<Rigidbody>().isKinematic = useSrauvPos;
    }

    public void sendTcp()
    {
        sock.SendAndReceive( System.Text.Encoding.UTF8.GetBytes("test"));
    }

    public void updateLog(string s)
    {
        if (logText.IndexOf(s) != 0)
        {
            logText = s + logText;
            missionLog.GetComponent<TMPro.TextMeshProUGUI>().text = logText;
        }
    }

    public void setWaypointReset(bool b)
    {
        reset_to_first_waypoint = b;
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
