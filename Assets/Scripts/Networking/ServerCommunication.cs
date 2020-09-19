using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Forefront class for the server communication.
/// </summary>
public class ServerCommunication : MonoBehaviour
{
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

    public int sendIntervalMs = 1000;
    public TelemetryModel tm = new TelemetryModel();

    // Address used in code
    private string host => useLocalhost ? "localhost" : hostIP;
    // Final server address
    private string server;
    
        public Transform[] distSensorVals;
        public Transform srauv;
        public Transform dock;
        public Transform tree1;
        public Transform tree2;
        public Transform tree3;
    // WebSocket Client
    private WsClient client;

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
        srauv = GameObject.Find("Dock").GetComponent<Transform>();
        tree1 = GameObject.Find("Tree1").GetComponent<Transform>();
        tree2 = GameObject.Find("Tree2").GetComponent<Transform>();
        tree3 = GameObject.Find("Tree3").GetComponent<Transform>();
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
                Debug.Log("Apply Forces Here");
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
        tm.msgType = "telem";
        tm.depth = distSensorVals[4].GetComponent<TMPro.TextMeshProUGUI>().text;
        tm.alt = distSensorVals[5].GetComponent<TMPro.TextMeshProUGUI>().text;
        tm.fwdDist = distSensorVals[0].GetComponent<TMPro.TextMeshProUGUI>().text;
        tm.rightDist = distSensorVals[1].GetComponent<TMPro.TextMeshProUGUI>().text;
        tm.rearDist = distSensorVals[2].GetComponent<TMPro.TextMeshProUGUI>().text;
        tm.leftDist = distSensorVals[3].GetComponent<TMPro.TextMeshProUGUI>().text;
        tm.posX = srauv.position.x;
        tm.posY = srauv.position.y;
        tm.posZ = srauv.position.z;
        if (dock)
            tm.dockDist = Vector3.Distance(srauv.position, dock.position);
        if (tree1)
            tm.tree1Dist = Vector3.Distance(srauv.position, tree1.position);
        if (tree2)
            tm.tree2Dist = Vector3.Distance(srauv.position, tree2.position);
        if (tree3)
            tm.tree3Dist = Vector3.Distance(srauv.position, tree3.position);
        
        string msg = JsonUtility.ToJson(tm);

        Debug.Log("Sending: " + msg);
        client.Send(msg);
    }
}
