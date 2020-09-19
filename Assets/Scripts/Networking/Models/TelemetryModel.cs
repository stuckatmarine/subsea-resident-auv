/// <summary>
/// Telemetry message model.
/// </summary>
[System.Serializable]
public class TelemetryModel
{
    public string source;
    public int msgNum;
    public string msgType;
    public string timestamp;
    public string depth;
    public string alt;
    // Keeping to primitives for ease of json parsing troubleshooting
    public string fwdDist, rightDist, rearDist, leftDist;
    public float posX;
    public float posY;
    public float posZ;
    public float dockDist;
    public float tree1Dist;
    public float tree2Dist;
    public float tree3Dist;
}
