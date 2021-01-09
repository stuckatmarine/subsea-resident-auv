using UnityEngine;
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
    public bool enableLogging = false;
    public bool enableVehicleCmds = false;

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
    public TelemetryModel tm = new TelemetryModel();
    public CamShotModel cm = new CamShotModel();

    // Address used in code
    private string host => useLocalhost ? "localhost" : hostIP;
    // Final server address
    private string server;
    
    // public Transform[] distSensorVals;
    public float[] distancesFloat;
    public Transform srauv;
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
        server = "ws://" + host + ":" + port;
        client = new WsClient(server);
                // Messaging
        // Lobby = new LobbyMessaging(this);
        ConnectToServer();

        srauv = GameObject.Find("SRAUV").GetComponent<Transform>();
        dock = GameObject.Find("Dock").GetComponent<Transform>();
        tree1 = GameObject.Find("Tree1").GetComponent<Transform>();
        tree2 = GameObject.Find("Tree2").GetComponent<Transform>();
        tree3 = GameObject.Find("Tree3").GetComponent<Transform>();

        frontCam = GameObject.Find("FrontCamera").GetComponent<Camera>();
        distancesFloat = GameObject.Find("SRAUV").GetComponent<DistanceSensors>().distancesFloat;

        // thrusterController = GameObject.Find("SRAUV").GetComponent<ThrusterController>();  
    }


    /// <summary>
    /// Unity method called every frame
    /// </summary>
    private void Update()
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

        if (Time.time * 1000 > lastTxTime + txIntervalMs)
        {
            SendRequest("placeholder");
            lastTxTime = (int)Time.time * 1000;
        }

        // apply thruste vals
        
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
        switch (message.msgType)
        {
            case "command":
                {
                    if (!enableVehicleCmds)
                    {
                        Debug.Log("vehicle cmds not eabled");
                        break;
                    }
                    
                    Debug.Log("Apply Forces Here");
                    forces[0] = message.thrustFwd;
                    forces[1] = message.thrustRight;
                    forces[2] = message.thrustRear;
                    forces[3] = message.thrustLeft;
                    forces[4] = message.vertA;
                    forces[5] = message.vertB;

                    // only impulses
                    // srauv.GetComponent<ThrusterController>().applyLatThrust(0, message.thrustFwd);
                    // srauv.GetComponent<ThrusterController>().applyLatThrust(1, message.thrustRight);
                    // srauv.GetComponent<ThrusterController>().applyLatThrust(2, message.thrustRear);
                    // srauv.GetComponent<ThrusterController>().applyLatThrust(3, message.thrustLeft);
                    // srauv.GetComponent<ThrusterController>().applyVertThrust(0, message.vertA);
                    // srauv.GetComponent<ThrusterController>().applyVertThrust(1, message.vertB);

                    // alternative example, higher level thruster control, not implement for all
                    // srauv.GetComponent<ThrusterController>().moveForward(thrustFwd);
                }
                break;
            case "telemetry":
                Debug.Log("Updating UI with telemetry from server");
                {
                    //  Update UI
                    var tel = JsonUtility.FromJson<TelemetryModel>(msg);

                    telX.GetComponent<TMPro.TextMeshProUGUI>().text = tel.posX.ToString("#.00");
                    telY.GetComponent<TMPro.TextMeshProUGUI>().text = tel.posY.ToString("#.00");
                    telZ.GetComponent<TMPro.TextMeshProUGUI>().text = tel.posZ.ToString("#.00");
                    telHeading.GetComponent<TMPro.TextMeshProUGUI>().text = tel.heading.ToString("#.0");
                    telDistanceFloats[0] = tel.fwdDist;
                    telDistanceFloats[1] = tel.rightDist;
                    telDistanceFloats[2] = tel.rearDist;
                    telDistanceFloats[3] = tel.leftDist;
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
                }
                break;
            case "reset":
                SceneManager.LoadScene (SceneManager.GetActiveScene ().name);
                break;
            default:
                Debug.Log("Unknown type of method: " + message.msgType);
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
    public void SendRequest(string message)
    {
        tm.source = "sim";
        tm.msgNum = txNum++;
        tm.msgType = "telemetry";
        DateTime timestamp = DateTime.Now;
        tm.timestamp = timestamp.ToString("MM/dd/yyy HH:mm:ss.") + DateTime.Now.Millisecond.ToString();
        tm.fwdDist = distancesFloat[0];
        tm.rightDist = distancesFloat[1];
        tm.rearDist = distancesFloat[2];
        tm.leftDist = distancesFloat[3];
        tm.depth = distancesFloat[4];
        tm.alt = distancesFloat[5];
        tm.posX = srauv.position.x;
        tm.posY = srauv.position.y;
        tm.posZ = srauv.position.z;
        tm.heading = srauv.rotation.y * 360.0f;
        tm.roll = srauv.rotation.x * 360.0f;
        tm.pitch = srauv.rotation.z * 360.0f;
        if (dock)
        {
            tm.dockDist = Vector3.Distance(srauv.position, dock.position);
            tm.dockDistX = srauv.position.x - dock.position.x;
            tm.dockDistY = srauv.position.y - dock.position.y;
            tm.dockDistZ = srauv.position.z - dock.position.z;
        }
        if (tree1)
        {
            tm.tree1Dist = Vector3.Distance(srauv.position, tree1.position);
            tm.tree1DistX = srauv.position.x - tree1.position.x;
            tm.tree1DistY = srauv.position.y - tree1.position.y;
            tm.tree1DistZ = srauv.position.z - tree1.position.z;
        }
        if (tree2)
        {
            tm.tree2Dist = Vector3.Distance(srauv.position, tree2.position);
            tm.tree2DistX = srauv.position.x - tree2.position.x;
            tm.tree2DistY = srauv.position.y - tree2.position.y;
            tm.tree2DistZ = srauv.position.z - tree2.position.z;
        }
        if (tree3)
        {
            tm.tree3Dist = Vector3.Distance(srauv.position, tree3.position);
            tm.tree3DistX = srauv.position.x - tree3.position.x;
            tm.tree3DistY = srauv.position.y - tree3.position.y;
            tm.tree3DistZ = srauv.position.z - tree3.position.z;
        }
        
        string msg = JsonUtility.ToJson(tm);

        if (enableLogging)
            Debug.Log("Sending: " + msg);

        client.Send(msg);

        //  Update UI
        simHeading.GetComponent<TMPro.TextMeshProUGUI>().text = tm.heading.ToString("#.0");
        simX.GetComponent<TMPro.TextMeshProUGUI>().text = tm.posX.ToString("#.00");
        simY.GetComponent<TMPro.TextMeshProUGUI>().text = tm.posY.ToString("#.00");
        simZ.GetComponent<TMPro.TextMeshProUGUI>().text = tm.posZ.ToString("#.00");

        // send screenshot too after every x msgs
        if (txNum % 2 == 0)
        {
            cm.source = "sim";
            cm.msgNum = txNum++;
            cm.msgType = "cam";
            cm.timestamp = timestamp.ToString("MM/dd/yyy HH:mm:ss.") + DateTime.Now.Millisecond.ToString();

            frontCamTexture = getScreenshot(frontCam);
            // frontCamTexSture = frontCam;
            byte[] bytes;
            bytes = frontCamTexture.EncodeToJPG();
            cm.imgStr = Convert.ToBase64String(bytes);

            msg = JsonUtility.ToJson(cm);
            if (enableLogging)
                Debug.Log("Sending Img: " + msg);

            client.Send(msg);
        }
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
}
